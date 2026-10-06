using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Mingxu.Core.Rules
{
public static class UserBlocked
{
    public static HashSet<char> Load()
    {
        var result = new HashSet<char>();
        foreach (var path in CandidatePaths())
            LoadFile(path, result);
        return result;
    }

    private static IEnumerable<string> CandidatePaths()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var root = AppDomain.CurrentDomain.BaseDirectory;
        foreach (var path in new[]
        {
            Path.Combine(root, "user_blocked_chars.json"),
            Path.Combine(root, "Data", "user_blocked_chars.json"),
            Path.Combine(root, "data", "user_blocked_chars.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TaiwanNamingHelper", "user_blocked_chars.json")
        })
            if (seen.Add(path)) yield return path;

        var dir = new DirectoryInfo(root);
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "data", "user_blocked_chars.json");
            if (seen.Add(path)) yield return path;
        }
    }

    private static void LoadFile(string path, HashSet<char> result)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var match = Regex.Match(json, "\"chars\"\\s*:\\s*\\[(.*?)\\]",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!match.Success) return;
            foreach (Match item in Regex.Matches(match.Groups[1].Value, "\"((?:\\\\.|[^\"])*)\""))
            {
                var value = Regex.Unescape(item.Groups[1].Value);
                foreach (var ch in value)
                    if (HardRules.IsNormalWritable(ch)) result.Add(ch);
            }
        }
        catch
        {
            // A malformed optional user file must not stop naming.
        }
    }
}
}
