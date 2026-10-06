using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Core.Content
{

public static class CategoryLists
{
    private static readonly Dictionary<string, double> DestinyWeights = new Dictionary<string, double>
    {
        ["bazi"] = 0.40, ["wuxing"] = 0.20, ["wuge"] = 0.30, ["zodiac"] = 0.10
    };
    private static readonly Dictionary<string, double> PhonologyWeights = new Dictionary<string, double>
    {
        ["phonology"] = 0.55, ["meaning"] = 0.20, ["style"] = 0.15, ["bazi"] = 0.10
    };
    private static readonly Dictionary<string, double> MeaningWeights = new Dictionary<string, double>
    {
        ["meaning"] = 0.50, ["phonology"] = 0.15, ["style"] = 0.20, ["rarity"] = 0.15
    };
    private static readonly Dictionary<string, double> ModernWeights = new Dictionary<string, double>
    {
        ["bazi"] = 0.15, ["wuxing"] = 0.08, ["wuge"] = 0.08, ["phonology"] = 0.25,
        ["meaning"] = 0.12, ["zodiac"] = 0.05, ["style"] = 0.20, ["rarity"] = 0.07
    };
    private static readonly Dictionary<string, double> NicheWeights = new Dictionary<string, double>
    {
        ["bazi"] = 0.12, ["wuxing"] = 0.08, ["wuge"] = 0.08, ["phonology"] = 0.15,
        ["meaning"] = 0.15, ["zodiac"] = 0.04, ["style"] = 0.18, ["rarity"] = 0.20
    };

    public static Dictionary<string, List<string>> Build(IEnumerable<NameSuggestion> scored)
    {
        var list = scored == null ? new List<NameSuggestion>() : scored.ToList();
        return new Dictionary<string, List<string>>
        {
            ["綜合最佳"] = TopNames(list, s => s.Total, 10),
            ["命理最佳"] = TopNames(list, s => Weighted(s, DestinyWeights), 5),
            ["音韻最佳"] = TopNames(list, s => Weighted(s, PhonologyWeights), 5),
            ["寓意最佳"] = TopNames(list, s => Weighted(s, MeaningWeights), 5),
            ["現代最佳"] = TopNames(list, s => Weighted(s, ModernWeights), 5),
            ["小眾最佳"] = TopNames(list, s => Weighted(s, NicheWeights), 5),
        };
    }

    private static List<string> TopNames(List<NameSuggestion> scored, Func<NameSuggestion, double> scoreFn, int n)
    {
        return scored.OrderByDescending(scoreFn).ThenByDescending(s => s.Total)
            .Take(n).Select(s => s.FullName + "（" + scoreFn(s).ToString("0.0") + "）").ToList();
    }

    private static double Weighted(NameSuggestion s, Dictionary<string, double> weights)
    {
        var dims = new Dictionary<string, double>
        {
            ["bazi"] = s.BaziScore, ["wuxing"] = s.WuxingScore, ["wuge"] = s.WugeScore,
            ["phonology"] = s.PhonologyScore, ["meaning"] = s.MeaningScore, ["zodiac"] = s.ZodiacScore,
            ["style"] = s.StyleScore, ["rarity"] = s.RarityScore
        };
        var totalW = weights.Values.Sum(w => Math.Max(0, w));
        if (totalW <= 0) totalW = 1;
        return Math.Round(weights.Sum(kv =>
        {
            double v;
            return (dims.TryGetValue(kv.Key, out v) ? v : 50) * Math.Max(0, kv.Value);
        }) / totalW, 1);
    }
}
}
