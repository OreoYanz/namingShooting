using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Core.Pipeline
{

public sealed class RankingResult
{
    public List<NameSuggestion> Selected { get; set; }
    public List<string> Notes { get; set; }

    public RankingResult(List<NameSuggestion> selected, List<string> notes)
    {
        Selected = selected;
        Notes = notes;
    }
}

internal sealed class RankingRegion
{
    public string Label { get; set; }
    public Func<NameSuggestion, double> Score { get; set; }
}

public static class FinalRanking
{
    public const int FullScorePool = 500;
    public const int RegionTop = 10;

    private static readonly Dictionary<string, double> DestinyWeights = new Dictionary<string, double>()
    {
        ["bazi"] = 0.40, ["wuxing"] = 0.20, ["wuge"] = 0.30, ["zodiac"] = 0.10,
    };
    private static readonly Dictionary<string, double> ModernWeights = new Dictionary<string, double>()
    {
        ["bazi"] = 0.15, ["wuxing"] = 0.08, ["wuge"] = 0.08, ["phonology"] = 0.25,
        ["meaning"] = 0.12, ["zodiac"] = 0.05, ["style"] = 0.20, ["rarity"] = 0.07,
    };
    private static readonly Dictionary<string, double> LiteraryWeights = new Dictionary<string, double>()
    {
        ["bazi"] = 0.12, ["wuxing"] = 0.08, ["wuge"] = 0.08, ["phonology"] = 0.18,
        ["meaning"] = 0.28, ["zodiac"] = 0.04, ["style"] = 0.18, ["rarity"] = 0.04,
    };
    private static readonly Dictionary<string, double> NicheWeights = new Dictionary<string, double>()
    {
        ["bazi"] = 0.12, ["wuxing"] = 0.08, ["wuge"] = 0.08, ["phonology"] = 0.15,
        ["meaning"] = 0.15, ["zodiac"] = 0.04, ["style"] = 0.18, ["rarity"] = 0.20,
    };

    public static RankingResult Rank(
        IEnumerable<NameSuggestion> scored,
        int topN = 50,
        bool diversityEnabled = true,
        int firstMax = 3,
        int secondMax = 3,
        int wuxingMax = 5,
        IEnumerable<string> preferredChars = null)
    {
        topN = Math.Max(1, topN);
        var preferred = new HashSet<string>((preferredChars ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrEmpty(x)));
        if (preferred.Count > 0)
            return RankPreferredFirst(scored, topN, diversityEnabled, firstMax, secondMax, wuxingMax, preferred);

        var pool = scored.OrderByDescending(EffectiveTotal).Take(FullScorePool).ToList();
        var notes = new List<string>
        {
            $"完整評分池 Top {pool.Count}／{FullScorePool}",
            $"跨區域取樣：綜合／命理／現代／文學／小眾 各 Top {RegionTop}",
        };
        if (pool.Count == 0) return new RankingResult(new List<NameSuggestion>(), notes);

        var taken = new HashSet<string>();
        var merged = new List<NameSuggestion>();
        var regions = new RankingRegion[]
        {
            new RankingRegion { Label = "綜合", Score = EffectiveTotal },
            new RankingRegion { Label = "命理", Score = s => Weighted(s, DestinyWeights) },
            new RankingRegion { Label = "現代", Score = s => Weighted(s, ModernWeights) },
            new RankingRegion { Label = "文學", Score = s => Weighted(s, LiteraryWeights) },
            new RankingRegion { Label = "小眾", Score = s => Weighted(s, NicheWeights) },
        };
        var per = Math.Max(1, Math.Min(RegionTop, topN / regions.Length));
        foreach (var region in regions)
        {
            var chunk = PickRegion(pool, region.Score, per, taken, Math.Min(3, firstMax), preferred);
            notes.Add($"{region.Label}型 {chunk.Count} 組");
            merged.AddRange(chunk);
        }

        if (merged.Count < topN)
        {
            foreach (var s in pool)
            {
                if (!taken.Add(s.FullName)) continue;
                merged.Add(s);
                if (merged.Count >= Math.Max(topN * 2, topN)) break;
            }
        }

        List<NameSuggestion> selected;
        if (diversityEnabled)
        {
            var diversityResult = Diversity.Select(merged, topN, firstMax, secondMax, wuxingMax, true);
            selected = diversityResult.Selected;
            notes.AddRange(diversityResult.Notes);
        }
        else
        {
            selected = merged.Take(topN).ToList();
            notes.Add("多樣性關閉，直接輸出合併結果");
        }

        if (selected.Count < topN)
        {
            var before = selected.Count;
            var seen = new HashSet<string>(selected.Select(s => s.FullName));
            foreach (var s in pool)
            {
                if (selected.Count >= topN) break;
                if (!seen.Add(s.FullName)) continue;
                selected.Add(s);
            }
            var filled = selected.Count - before;
            if (filled > 0)
                notes.Add($"評分池次優補足 +{filled}，湊滿 {selected.Count}／目標 {topN}");
        }

        notes.Insert(0, $"最終輸出 Top {selected.Count}／目標 {topN}");
        return new RankingResult(selected, notes);
    }

    public static RankingResult RankPreferredFirst(
        IEnumerable<NameSuggestion> scored,
        int topN,
        bool diversityEnabled,
        int firstMax,
        int secondMax,
        int wuxingMax,
        IEnumerable<string> preferredChars)
    {
        topN = Math.Max(1, topN);
        var all = scored.OrderByDescending(EffectiveTotal).ToList();
        var preferred = new HashSet<string>((preferredChars ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrEmpty(x)));
        var withPreferred = all.Where(s => PreferredHits(s, preferred) > 0).ToList();
        var plain = all.Where(s => PreferredHits(s, preferred) == 0).ToList();
        var preferredQuota = Math.Min(withPreferred.Count, Math.Max(1, (int)Math.Ceiling(topN * .60)));
        var plainQuota = Math.Min(plain.Count, Math.Max(0, topN - preferredQuota));

        var preferredResult = Rank(withPreferred, preferredQuota, diversityEnabled,
            firstMax, secondMax, wuxingMax, null);
        var plainResult = plainQuota > 0
            ? Rank(plain, plainQuota, diversityEnabled, firstMax, secondMax, wuxingMax, null)
            : new RankingResult(new List<NameSuggestion>(), new List<string>());
        var selected = preferredResult.Selected.Concat(plainResult.Selected).ToList();
        var seen = new HashSet<string>(selected.Select(x => x.FullName));
        foreach (var item in all)
        {
            if (selected.Count >= topN) break;
            if (seen.Add(item.FullName)) selected.Add(item);
        }
        return new RankingResult(selected, new List<string>
        {
            "偏好字優先：" + preferredResult.Selected.Count + "，一般：" + plainResult.Selected.Count,
            "偏好字目標配額約 60%"
        }.Concat(preferredResult.Notes).Concat(plainResult.Notes).ToList());
    }

    private static List<NameSuggestion> PickRegion(
        List<NameSuggestion> scored,
        Func<NameSuggestion, double> scoreFn,
        int limit,
        HashSet<string> taken,
        int firstMax,
        HashSet<string> preferredChars)
    {
        var ranked = scored
            .Where(s => !taken.Contains(s.FullName))
            .OrderByDescending(s => PreferredHits(s, preferredChars))
            .ThenByDescending(s => scoreFn(s))
            .ThenByDescending(EffectiveTotal)
            .ToList();
        if (ranked.Count == 0) return new List<NameSuggestion>();

        var result = Diversity.Select(
            ranked,
            limit,
            Math.Max(1, firstMax),
            Math.Max(1, firstMax),
            Math.Max(limit, 3),
            false);
        foreach (var s in result.Selected)
            taken.Add(s.FullName);
        return result.Selected;
    }

    private static double Weighted(NameSuggestion s, Dictionary<string, double> weights)
    {
        var dims = new Dictionary<string, double>
        {
            ["bazi"] = s.BaziScore,
            ["wuxing"] = s.WuxingScore,
            ["wuge"] = s.WugeScore,
            ["phonology"] = s.PhonologyScore,
            ["meaning"] = s.MeaningScore,
            ["zodiac"] = s.ZodiacScore,
            ["style"] = s.StyleScore,
            ["rarity"] = s.RarityScore,
        };
        var totalW = weights.Values.Sum(w => Math.Max(0, w));
        if (totalW <= 0) totalW = 1;
        return Math.Round(weights.Sum(kv => DimensionValue(dims, kv.Key) * Math.Max(0, kv.Value)) / totalW, 1);
    }

    private static int PreferredHits(NameSuggestion suggestion, HashSet<string> preferred)
    {
        if (preferred == null || preferred.Count == 0) return 0;
        return preferred.Count(x => suggestion.Given.Contains(x));
    }

    private static double EffectiveTotal(NameSuggestion suggestion)
    {
        return suggestion.Total + suggestion.RankBonus;
    }

    private static double DimensionValue(Dictionary<string, double> dimensions, string key)
    {
        double value;
        return dimensions.TryGetValue(key, out value) ? value : 50;
    }
}
}
