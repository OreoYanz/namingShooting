using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Constants;
using Mingxu.Core.Models;
using Mingxu.Core.Parents;
using Mingxu.Core.Wuge;

namespace Mingxu.Core.Scoring
{

internal sealed class ScoreNotes
{
    public double Score { get; set; }
    public List<string> Notes { get; set; }

    public ScoreNotes(double score, List<string> notes)
    {
        Score = score;
        Notes = notes;
    }
}

public sealed class ScoreOptions
{
    public string NamingMode { get; set; } = "balanced";
    public string Gender { get; set; } = "M";
    public int RefYear { get; set; }
    public string FatherName { get; set; } = "";
    public string MotherName { get; set; } = "";
    public bool AvoidParentChars { get; set; } = true;
    public bool ApplyParentWeight { get; set; } = true;
    public double ParentWeight { get; set; } = 0.15;
    public bool ExcludeHot5y { get; set; }
    public bool ExcludeClassicHot { get; set; }
    public int HotYears { get; set; } = 5;
    public int HotRankLimit { get; set; } = 50;
}

public static class NameScorer
{
    public static NameSuggestion Analyze(string surname, string given, Pillars pillars,
        ICharacterRepository repo, string namingMode, string gender, int refYear)
    {
        return Analyze(surname, given, pillars, repo, new ScoreOptions
        {
            NamingMode = namingMode,
            Gender = gender,
            RefYear = refYear
        });
    }

    public static NameSuggestion Analyze(string surname, string given, Pillars pillars,
        ICharacterRepository repo, ScoreOptions options)
    {
        options = options ?? new ScoreOptions();
        foreach (var ch in surname + given)
            if (!repo.TryStroke(ch.ToString()).HasValue)
                throw new ArgumentException("字典無「" + ch + "」之康熙筆畫");
        var wuge = WugeCalculator.Compute(surname, given, repo.StrokeOf);
        var chars = given.Select(c => c.ToString()).ToList();
        var charWx = chars.Select(repo.WuxingOf).Where(x => !string.IsNullOrEmpty(x)).ToList();
        var profiles = chars.Select(repo.Get).ToList();
        var meanings = profiles.Where(x => x != null).Select(x => x.Meaning)
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var mode = NamingModes.Resolve(options.NamingMode);

        var surnameWx = surname.Length == 0 ? "" : repo.WuxingOf(surname[0].ToString());
        var wuxing = WuxingScorer.Score(pillars, charWx, surnameWx, chars);
        var wuxingScore = wuxing.Score;
        var wxNotes = wuxing.Notes;
        var baziScore = ScoreBaziFit(pillars, charWx);
        var zodiacScore = ZodiacScorer.Score(pillars.Zodiac, profiles);
        var phonology = PhonologyScorer.Score(surname, given, repo);
        var meaning = MeaningScorer.Score(given, profiles);
        var styleScore = ScoreStyle(profiles, mode, options.Gender);
        var rarityScore = ScoreRarity(profiles, mode);
        var parent = ParentEvaluator.Evaluate(
            surname, given, wuge, charWx, options.FatherName, options.MotherName,
            repo, options.AvoidParentChars);
        if (parent.Blocked) return null;

        var dims = new Dictionary<string, double>
        {
            {"bazi",baziScore},{"wuxing",wuxingScore},{"wuge",wuge.Score},
            {"phonology",phonology.Score},{"meaning",meaning.Score},{"zodiac",zodiacScore},
            {"style",styleScore},{"rarity",rarityScore}
        };
        var total = mode.Weights.Sum(x => dims[x.Key] * x.Value);
        if (parent.Used && options.ApplyParentWeight)
        {
            var parentWeight = Math.Max(0, Math.Min(0.5, options.ParentWeight));
            total = total * (1 - parentWeight) + parent.Score * parentWeight;
        }
        var aesthetic = NameAesthetic.Score(given, profiles, meaning, phonology);
        total = NameAesthetic.BlendAestheticTotal(total, aesthetic.Score);
        var hot = HotNames.Penalty(given, options.Gender, options.RefYear,
            options.HotYears, options.HotRankLimit, options.ExcludeHot5y,
            options.ExcludeClassicHot, repo);
        total -= hot.Penalty;
        total = Math.Max(0, Math.Min(100, Math.Round(total, 1)));

        var reasons = new List<string>
        {
            $"八字喜用：{string.Join("、", pillars.XiYong)}；忌：{string.Join("、", pillars.JiShen)}",
            $"名中五行：{string.Join("、", charWx)}",
            $"三才五格：{wuge.Sancai}（{wuge.SancaiLuck}）",
            "命名模式：" + mode.Label,
        };
        reasons.AddRange(wxNotes.Take(2));
        reasons.AddRange(phonology.Notes.Take(2));
        reasons.AddRange(meaning.Notes.Take(2));
        if (parent.Used)
        {
            reasons.Add("父母合參：" + parent.Score.ToString("0.0"));
            reasons.AddRange(parent.Notes.Take(2));
        }
        reasons.Add("姓名美感：" + aesthetic.Score.ToString("0.0") + "（字形、語意、意象）");
        reasons.AddRange(aesthetic.Notes.Take(2));
        if (hot.Penalty > 0) reasons.Add(hot.Note + "；合計 -" + hot.Penalty.ToString("0.0"));

        return new NameSuggestion
        {
            Surname = surname,
            Given = given,
            Total = total,
            Grade = GradeLabel(total),
            BaziScore = Math.Round((double)baziScore, 1),
            WuxingScore = Math.Round((double)wuxingScore, 1),
            WugeScore = wuge.Score,
            PhonologyScore = Math.Round(phonology.Score, 1),
            MeaningScore = Math.Round(meaning.Score, 1),
            ZodiacScore = Math.Round((double)zodiacScore, 1),
            StyleScore = Math.Round((double)styleScore, 1),
            RarityScore = Math.Round((double)rarityScore, 1),
            AestheticScore = aesthetic.Score,
            ParentScore = parent.Score,
            ParentUsed = parent.Used,
            CharWuxing = charWx,
            Meanings = meanings.Select(m => m.Length > 24 ? m.Substring(0, 24) + "…" : m).ToList(),
            Reasons = reasons,
            ReasonText = string.Join("\n", reasons),
            Wuge = wuge,
        };
    }

    public static string GradeLabel(double score)
    {
        if (score >= 90) return "卓異";
        if (score >= 80) return "上佳";
        if (score >= 70) return "良好";
        if (score >= 60) return "中上";
        if (score >= 50) return "中平";
        return "待琢";
    }

    private static double ScoreBaziFit(Pillars pillars, List<string> charWx)
    {
        if (charWx.Count == 0) return 50;
        var score = 50.0;
        var xiHits = 0;
        var jiHits = 0;
        var xi1 = pillars.XiYong.FirstOrDefault() ?? "";
        var xi2 = pillars.XiYong.Skip(1).FirstOrDefault() ?? "";
        foreach (var wx in charWx)
        {
            if (wx == xi1) { score += 18; xiHits++; }
            else if (wx == xi2) { score += 12; xiHits++; }
            else if (pillars.XiCi.Contains(wx)) { score += 6; xiHits++; }
            else if (pillars.TiaoHou.Contains(wx) && !pillars.JiShen.Contains(wx)) score += 5;
            else if (pillars.JiShen.Contains(wx)) { score -= 12; jiHits++; }
            else if (pillars.JiCi.Contains(wx)) { score -= 8; jiHits++; }
        }
        if (charWx.Count > 0 && xiHits == charWx.Count) score += 8;
        if (jiHits > 0) score -= 6;
        if (pillars.Strength100 < 40 && xiHits > 0) score += 4;
        if (pillars.Strength100 > 70 && jiHits > 0) score -= 3;
        return Math.Max(0, Math.Min(100, score));
    }

    private static double ScoreStyle(IList<CharacterInfo> chars, NamingModeProfile mode, string gender)
    {
        var valid = chars.Where(x => x != null).ToList();
        if (valid.Count == 0) return 50;
        var prefer = mode.StylePrefer;
        if (prefer == null)
            prefer = gender == "M"
                ? new[] {"modern","classical","literary","elegant","strong","neutral"}
                : gender == "F"
                    ? new[] {"modern","literary","elegant","soft","cute","classical"}
                    : new[] {"modern","classical","literary","elegant","cute","neutral","strong","soft"};
        var scores = new List<double>();
        foreach (var info in valid)
        {
            var score = prefer.Average(k => NamingModes.StyleValue(info, k));
            foreach (var soft in mode.StyleSoft ?? new string[0])
            {
                var value = NamingModes.StyleValue(info, soft);
                if (value >= 75) score -= (value - 70) * .25;
            }
            if (!string.IsNullOrEmpty(info.GenderTag) && info.GenderTag != "U")
            {
                if (gender == info.GenderTag) score += 4;
                else if ((gender == "M" || gender == "F") && info.GenderTag != gender) score -= 6;
            }
            scores.Add(score);
        }
        return Math.Max(0, Math.Min(100, Math.Round(scores.Average(), 1)));
    }

    private static double ScoreRarity(IList<CharacterInfo> chars, NamingModeProfile mode)
    {
        var valid = chars.Where(x => x != null).ToList();
        if (valid.Count == 0) return 50;
        return Math.Round(valid.Average(info =>
        {
            var r = info.Rarity;
            double score;
            if (mode.RarityBias == "rare")
                score = r >= 55 && r <= 88 ? 60 + (r - 55) * (32.0 / 33.0)
                    : r < 55 ? 25 + r * (35.0 / 55.0) : 92 - (r - 88) * .5;
            else
                score = r >= 45 && r <= 78 ? 55 + (r - 45) * (35.0 / 33.0)
                    : r < 45 ? 35 + r * (20.0 / 45.0) : 90 - (r - 78) * .8;
            return Math.Max(0, Math.Min(100, score));
        }), 1);
    }
}
}
