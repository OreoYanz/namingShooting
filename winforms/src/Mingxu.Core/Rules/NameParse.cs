using System.Collections.Generic;
using System.Linq;

namespace Mingxu.Core.Rules
{
public sealed class ParsedName
{
    public string Surname { get; set; }
    public string Given { get; set; }
    public bool IsCompound { get; set; }

    public ParsedName(string surname, string given, bool isCompound)
    {
        Surname = surname;
        Given = given;
        IsCompound = isCompound;
    }
}

public static class NameParse
{
    public static List<string> ParseForbidden(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        foreach (var separator in new[] { '，', ',', '、', ';', '；', '/', '|', ' ', '　' })
            text = text.Replace(separator, ' ');
        return text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList();
    }

    /// <summary>
    /// 拆全名為姓／名。優先採用 preferredSurname（支援未列於複姓表的複姓）；
    /// 否則依複姓表最長前綴比對；再否則取首字為姓。
    /// </summary>
    public static ParsedName SplitFullName(string full, IEnumerable<string> compounds, string preferredSurname = null)
    {
        var text = Clean(full);
        // 僅採用長度≥2 的明確姓氏（複姓／手填），避免單字姓搶在複姓表之前把「歐陽…」拆成「歐｜陽…」
        var preferred = Clean(preferredSurname);
        if (preferred.Length >= 2)
        {
            if (text.Length == 0)
                return new ParsedName(preferred, "", true);
            if (text.StartsWith(preferred, System.StringComparison.Ordinal) && text.Length > preferred.Length)
                return new ParsedName(preferred, text.Substring(preferred.Length), true);
            if (text == preferred)
                return new ParsedName(preferred, "", true);
        }

        var ordered = (compounds ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderByDescending(x => x.Length);
        foreach (var compound in ordered)
            if (text.StartsWith(compound, System.StringComparison.Ordinal) && text.Length > compound.Length)
                return new ParsedName(compound, text.Substring(compound.Length), true);
        if (text.Length == 0) return new ParsedName("", "", false);
        return new ParsedName(text.Substring(0, 1), text.Substring(1), false);
    }

    public static string ParentGivenName(string full, IEnumerable<string> compounds)
    {
        var text = Clean(full);
        if (text.Length <= 1) return text;
        return SplitFullName(text, compounds).Given;
    }

    private static string Clean(string text)
    {
        return (text ?? "").Trim().Replace(" ", "").Replace("　", "")
            .Replace("·", "").Replace("・", "").Replace(".", "").Replace("．", "");
    }
}
}
