using System.Collections.Generic;
using Mingxu.Core.Rules;

namespace Mingxu.Core.Parents
{
public static class ParentTaboo
{
    public static HashSet<char> Extract(
        string fatherFullName, string motherFullName, IEnumerable<string> compoundSurnames)
    {
        var result = new HashSet<char>();
        AddGiven(result, NameParse.ParentGivenName(fatherFullName, compoundSurnames));
        AddGiven(result, NameParse.ParentGivenName(motherFullName, compoundSurnames));
        return result;
    }

    private static void AddGiven(HashSet<char> result, string given)
    {
        foreach (var ch in given ?? "") result.Add(ch);
    }
}
}
