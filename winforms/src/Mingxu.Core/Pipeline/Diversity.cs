using System.Collections.Generic;
using System;
using System.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Core.Pipeline
{

public sealed class SelectionResult
{
    public List<NameSuggestion> Selected { get; set; }
    public List<string> Notes { get; set; }

    public SelectionResult(List<NameSuggestion> selected, List<string> notes)
    {
        Selected = selected;
        Notes = notes;
    }
}

public static class Diversity
{
    public const int FirstMax = 3;
    public const int SecondMax = 3;
    public const int WuxingMax = 5;

    public static string WuxingComboKey(IEnumerable<string> charWuxing)
    {
        var elems = charWuxing.Where(wx => !string.IsNullOrWhiteSpace(wx)).ToList();
        return elems.Count == 0 ? "—" : string.Join("+", elems);
    }

    public static SelectionResult Select(
        IEnumerable<NameSuggestion> candidates,
        int limit,
        int firstMax = FirstMax,
        int secondMax = SecondMax,
        int wuxingMax = WuxingMax,
        bool allowScoreFill = true)
    {
        limit = Math.Max(0, limit);
        var list = candidates.ToList();
        if (limit <= 0 || list.Count == 0)
            return new SelectionResult(new List<NameSuggestion>(), new List<string>());

        firstMax = Math.Max(1, firstMax);
        secondMax = Math.Max(1, secondMax);
        wuxingMax = Math.Max(1, wuxingMax);

        var notes = new List<string>
        {
            $"多樣性：同第一字≤{firstMax}、同第二字≤{secondMax}、同五行組合≤{wuxingMax}",
        };
        var selected = new List<NameSuggestion>();
        var firstCnt = new Dictionary<string, int>();
        var secondCnt = new Dictionary<string, int>();
        var wxCnt = new Dictionary<string, int>();
        var taken = new HashSet<string>();
        var skipped = 0;

        foreach (var sug in list)
        {
            if (selected.Count >= limit) break;
            TryAdd(sug, false, selected, firstCnt, secondCnt, wxCnt, taken, firstMax, secondMax, wuxingMax, ref skipped);
        }

        if (allowScoreFill && selected.Count < limit)
        {
            var before = selected.Count;
            foreach (var sug in list)
            {
                if (selected.Count >= limit) break;
                TryAdd(sug, true, selected, firstCnt, secondCnt, wxCnt, taken, firstMax, secondMax, wuxingMax, ref skipped);
            }
            var filled = selected.Count - before;
            if (filled > 0)
                notes.Add($"多樣性次優補足 +{filled}");
        }

        if (skipped > 0)
            notes.Add($"多樣性略過重複 {skipped} 組");
        return new SelectionResult(selected, notes);
    }

    private static bool TryAdd(
        NameSuggestion sug,
        bool soft,
        List<NameSuggestion> selected,
        Dictionary<string, int> firstCnt,
        Dictionary<string, int> secondCnt,
        Dictionary<string, int> wxCnt,
        HashSet<string> taken,
        int firstMax,
        int secondMax,
        int wuxingMax,
        ref int skipped)
    {
        var given = sug.Given ?? "";
        if (string.IsNullOrEmpty(given)) return false;
        var key = sug.FullName;
        if (!taken.Add(key)) return false;

        var first = given[0].ToString();
        var second = given.Length >= 2 ? given[1].ToString() : "";
        var wx = WuxingComboKey(sug.CharWuxing);

        if (!soft)
        {
            if (CountOf(firstCnt, first) >= firstMax) { skipped++; taken.Remove(key); return false; }
            if (!string.IsNullOrEmpty(second) && CountOf(secondCnt, second) >= secondMax) { skipped++; taken.Remove(key); return false; }
            if (CountOf(wxCnt, wx) >= wuxingMax) { skipped++; taken.Remove(key); return false; }
        }

        selected.Add(sug);
        firstCnt[first] = CountOf(firstCnt, first) + 1;
        if (!string.IsNullOrEmpty(second))
            secondCnt[second] = CountOf(secondCnt, second) + 1;
        wxCnt[wx] = CountOf(wxCnt, wx) + 1;
        return true;
    }

    private static int CountOf(Dictionary<string, int> counts, string key)
    {
        int value;
        return counts.TryGetValue(key, out value) ? value : 0;
    }
}
}
