using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Constants;
using Mingxu.Core.Models;
using Mingxu.Core.Rules;
using Mingxu.Core.Wuge;

namespace Mingxu.Core.Parents
{
public sealed class ParentEvalResult
{
    public double Score { get; set; }
    public List<string> Notes { get; set; } = new List<string>();
    public List<string> TabooChars { get; set; } = new List<string>();
    public bool Blocked { get; set; }
    public string BlockReason { get; set; } = "";
    public bool Used { get; set; }
}

public static class ParentEvaluator
{
    public static ParentEvalResult Evaluate(
        string childSurname,
        string childGiven,
        WugeResult childWuge,
        IEnumerable<string> childWx,
        string fatherFull,
        string motherFull,
        ICharacterRepository repo,
        bool avoidParentChars = true)
    {
        fatherFull = Clean(fatherFull);
        motherFull = Clean(motherFull);
        if (fatherFull.Length == 0 && motherFull.Length == 0)
            return new ParentEvalResult
            {
                Score = 100,
                Notes = new List<string> { "未填父母姓名，略過家族合參。" },
                Used = false
            };

        var compounds = repo.CompoundSurnames();
        var notes = new List<string>();
        var taboo = ParentTaboo.Extract(fatherFull, motherFull, compounds);
        var parentWx = new List<ParentChar>();
        var parentSounds = new List<ParentSound>();

        AddParent("父", fatherFull, compounds, repo, notes, parentWx, parentSounds);
        AddParent("母", motherFull, compounds, repo, notes, parentWx, parentSounds);

        var overlap = (childGiven ?? "").Where(taboo.Contains)
            .Select(c => c.ToString()).Distinct().ToList();
        if (overlap.Count > 0)
        {
            var text = "避諱：用字「" + string.Join("", overlap) + "」與父母名字相同";
            if (avoidParentChars)
                return Blocked(text, notes, overlap);
            notes.Add(text + "（未硬排除，已重扣）");
        }

        foreach (var pair in new[] { new ParentName("父", fatherFull), new ParentName("母", motherFull) })
        {
            if (pair.Full.Length == 0) continue;
            var given = NameParse.ParentGivenName(pair.Full, compounds);
            if (given.Length > 0 && given == childGiven)
            {
                var text = "與" + pair.Role + "親名字完全相同";
                if (avoidParentChars)
                    return Blocked(text, notes, (childGiven ?? "").Select(c => c.ToString()).ToList());
                notes.Add(text);
            }
        }

        var points = new List<double>();
        if (overlap.Count > 0 && !avoidParentChars) points.Add(18);

        foreach (var ch in childGiven ?? "")
        {
            var sound = ZhuyinCore(repo.SoundZhuyin(ch.ToString()));
            if (sound.Length == 0) continue;
            var roles = parentSounds.Where(x => x.Sound == sound).Select(x => x.Role).Distinct().ToList();
            if (roles.Count == 0) continue;
            points.Add(28);
            notes.Add("「" + ch + "」與" + string.Join("/", roles) + "親用字讀音相近，宜避");
        }

        var wxScores = new List<int>();
        foreach (var cwx in (childWx ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrEmpty(x)))
        {
            foreach (var pwx in parentWx)
            {
                string relation;
                var delta = PairWuxing(cwx, pwx.Wuxing, out relation);
                if (relation.Length == 0) continue;
                wxScores.Add(delta);
                if (delta >= 12)
                    notes.Add("與" + pwx.Role + "親「" + pwx.Char + "」" + relation + "，家族五行較順");
                else if (delta < 0)
                    notes.Add("與" + pwx.Role + "親「" + pwx.Char + "」" + relation + "，家族五行較拗");
            }
        }
        if (wxScores.Count > 0)
            points.Add(Clamp(62 + wxScores.Average() * 2.2, 20, 95));
        else
            points.Add(60);

        foreach (var pair in new[] { new ParentName("父", fatherFull), new ParentName("母", motherFull) })
        {
            if (pair.Full.Length == 0 || childWuge == null) continue;
            var parsed = NameParse.SplitFullName(pair.Full, compounds);
            if (parsed.Surname.Length == 0 || parsed.Given.Length == 0) continue;
            if ((parsed.Surname + parsed.Given).Any(c => !repo.TryStroke(c.ToString()).HasValue))
            {
                notes.Add(pair.Role + "親姓名有字典未收字，略過其五格合參");
                continue;
            }
            try
            {
                var pw = WugeCalculator.Compute(parsed.Surname, parsed.Given, repo.StrokeOf);
                string relation;
                var delta = PairWuxing(childWuge.RenWx, pw.RenWx, out relation);
                if (relation.Length == 0) continue;
                points.Add(Clamp(60 + delta * 2, 20, 95));
                notes.Add("人格五行合參：子" + childWuge.RenWx + " vs " + pair.Role + pw.RenWx +
                    "（" + relation + "；" + pair.Role + "親人格" + pw.Ren + "）");
            }
            catch (Exception)
            {
            }
        }

        var score = points.Count == 0 ? (overlap.Count == 0 ? 70 : 30) : points.Average();
        if (overlap.Count > 0) score = Math.Min(score, 40);
        score = Math.Round(Clamp(score, 0, 100), 1);
        if (!notes.Any(n => n.Contains("五行") || n.Contains("避諱") || n.Contains("讀音") || n.Contains("人格")))
            notes.Add("父母姓名已納入合參，未見明顯衝犯。");
        return new ParentEvalResult
        {
            Score = score,
            Notes = notes,
            TabooChars = overlap,
            Used = true
        };
    }

    private static void AddParent(
        string role,
        string full,
        IEnumerable<string> compounds,
        ICharacterRepository repo,
        List<string> notes,
        List<ParentChar> parentWx,
        List<ParentSound> sounds)
    {
        if (full.Length == 0) return;
        var parsed = NameParse.SplitFullName(full, compounds);
        var given = NameParse.ParentGivenName(full, compounds);
        notes.Add(role + "親姓名：" + full + "（姓" + (parsed.Surname.Length == 0 ? "—" : parsed.Surname) +
            "　名" + (given.Length == 0 ? "—" : given) + "）");
        foreach (var ch in given)
        {
            var text = ch.ToString();
            var wx = repo.WuxingOf(text);
            if (!string.IsNullOrEmpty(wx)) parentWx.Add(new ParentChar(role, text, wx));
            var sound = ZhuyinCore(repo.SoundZhuyin(text));
            if (sound.Length > 0) sounds.Add(new ParentSound(role, sound));
        }
    }

    private static ParentEvalResult Blocked(string reason, List<string> notes, List<string> taboo)
    {
        var allNotes = new List<string>(notes) { reason };
        return new ParentEvalResult
        {
            Score = 0,
            Notes = allNotes,
            TabooChars = taboo,
            Blocked = true,
            BlockReason = reason,
            Used = true
        };
    }

    private static int PairWuxing(string a, string b, out string relation)
    {
        relation = "";
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        if (a == b) { relation = a + "比和"; return 6; }
        if (WuXing.Generates(a, b)) { relation = a + "生" + b; return 12; }
        if (WuXing.Generates(b, a)) { relation = b + "生" + a; return 12; }
        if (WuXing.Controls(a, b)) { relation = a + "克" + b; return -14; }
        if (WuXing.Controls(b, a)) { relation = b + "克" + a; return -14; }
        return 0;
    }

    private static string ZhuyinCore(string value)
    {
        var stripped = (value ?? "").Replace("ˊ", "").Replace("ˇ", "").Replace("ˋ", "").Replace("˙", "");
        return new string(stripped.Where(c => !char.IsWhiteSpace(c) && !char.IsDigit(c) &&
            !(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z')).ToArray());
    }

    private static string Clean(string text)
    {
        return (text ?? "").Trim().Replace(" ", "").Replace("　", "")
            .Replace("·", "").Replace("・", "").Replace(".", "").Replace("．", "");
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private sealed class ParentName
    {
        public string Role { get; private set; }
        public string Full { get; private set; }
        public ParentName(string role, string full) { Role = role; Full = full; }
    }

    private sealed class ParentChar
    {
        public string Role { get; private set; }
        public string Char { get; private set; }
        public string Wuxing { get; private set; }
        public ParentChar(string role, string character, string wuxing)
        {
            Role = role; Char = character; Wuxing = wuxing;
        }
    }

    private sealed class ParentSound
    {
        public string Role { get; private set; }
        public string Sound { get; private set; }
        public ParentSound(string role, string sound) { Role = role; Sound = sound; }
    }
}
}
