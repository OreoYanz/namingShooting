using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Core.Scoring
{
public static class ComboSynergy
{
    public static double SynergyScore(NameSuggestion suggestion, IDictionary<string, int> ranks)
    {
        if (suggestion == null || string.IsNullOrEmpty(suggestion.Given)) return 0;
        var values = suggestion.Given.Select(ch =>
        {
            int rank;
            return ranks.TryGetValue(ch.ToString(), out rank) ? rank : 9999;
        }).ToList();
        var worst = values.Max();
        var avg = values.Average();
        if ((worst < 120 && avg < 80) || suggestion.Total < 70) return 0;
        var rankBonus = Math.Min(40, (worst - 80) * .08 + (avg - 60) * .05);
        return Math.Round(suggestion.Total * .55 + suggestion.AestheticScore * .35 + rankBonus, 2);
    }

    public static List<NameSuggestion> PickComboSynergy(IEnumerable<NameSuggestion> scored,
        IEnumerable<CharacterInfo> rankedChars, int limit)
    {
        var ranks = rankedChars.Select((c, i) => new { c.Char, Index = i })
            .GroupBy(x => x.Char).ToDictionary(g => g.Key, g => g.First().Index);
        return scored.Where(s => s.Given.Length >= 2)
            .Select(s => new { Suggestion = s, Score = SynergyScore(s, ranks) })
            .Where(x => x.Score >= 72)
            .OrderByDescending(x => x.Score).Take(Math.Max(0, limit))
            .Select(x => x.Suggestion).ToList();
    }

    public static List<NameSuggestion> MergeComboExploreIntoTop(List<NameSuggestion> top,
        IEnumerable<NameSuggestion> picks, int topN, List<string> notes)
    {
        var reserve = Math.Max(1, (int)Math.Round(topN * .12));
        var seen = new HashSet<string>(top.Select(x => x.FullName));
        var injected = picks.Where(x => seen.Add(x.FullName)).Take(reserve).ToList();
        if (injected.Count == 0) return top;
        var keep = Math.Max(0, topN - injected.Count);
        var merged = top.Take(keep).Concat(injected).Take(topN).ToList();
        notes.Add("組合級探索保留 " + injected.Count + " 組（非最高單字分、整體協調佳）");
        return merged;
    }
}
}
