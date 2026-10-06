using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Core.Rules
{
public static class XiyongChars
{
    public const int MaxPreferredInCombo = 2;

    public static List<string> ValidatePreferred(IEnumerable<string> preferred, ICharacterRepository repo)
    {
        var notes = new List<string>();
        foreach (var ch in (preferred ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrEmpty(x)).Distinct())
        {
            CharacterInfo info;
            if (ch.Length != 1 || !repo.TryGet(ch, out info))
                notes.Add("偏好字「" + ch + "」不在字庫中，已略過");
            else if (!repo.TryStroke(ch).HasValue)
                notes.Add("偏好字「" + ch + "」缺康熙筆畫，已略過");
        }
        return notes;
    }

    public static List<string> ValidPreferred(IEnumerable<string> preferred, ICharacterRepository repo)
    {
        return (preferred ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrEmpty(x) && x.Length == 1)
            .Where(x =>
            {
                CharacterInfo info;
                return repo.TryGet(x, out info) && repo.TryStroke(x).HasValue;
            }).Distinct().ToList();
    }

    public static List<string> FilterGivensByPreferred(IEnumerable<string> givens, IEnumerable<string> preferred)
    {
        var set = new HashSet<char>((preferred ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrEmpty(x)).Select(x => x[0]));
        if (set.Count == 0) return givens.ToList();
        return givens.Where(g => g.Count(set.Contains) <= MaxPreferredInCombo).ToList();
    }

    /// <summary>
    /// True when given contains the generation char (字輩) and at least one preferred char (喜用字).
    /// </summary>
    public static bool ContainsZibeiAndPreferred(string given, string zibei, IEnumerable<string> preferred)
    {
        if (string.IsNullOrEmpty(given) || string.IsNullOrEmpty(zibei)) return false;
        if (!given.Contains(zibei)) return false;
        var prefs = (preferred ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrEmpty(x)).ToList();
        if (prefs.Count == 0) return false;
        return prefs.Any(p => given.Contains(p));
    }

    /// <summary>
    /// Build forced double-name slots: 字輩+喜用字 or 喜用字+字輩 (by position).
    /// Requires allowDouble; skips when preferred equals zibei.
    /// </summary>
    public static List<string> BuildForcedZibeiPreferredGivens(
        string zibei,
        IEnumerable<string> preferred,
        int zibeiPosition,
        bool allowDouble)
    {
        var result = new List<string>();
        if (!allowDouble || string.IsNullOrEmpty(zibei) || zibei.Length != 1) return result;
        var seen = new HashSet<string>();
        foreach (var p in (preferred ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrEmpty(x) && x.Length == 1).Distinct())
        {
            if (p == zibei) continue;
            var g = zibeiPosition == 1 ? p + zibei : zibei + p;
            if (seen.Add(g)) result.Add(g);
        }
        return result;
    }
}
}
