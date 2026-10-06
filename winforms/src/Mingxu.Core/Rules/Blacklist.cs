using System.Collections.Generic;
using Mingxu.Core.Models;

namespace Mingxu.Core.Rules
{
public static class Blacklist
{
    public static readonly HashSet<string> ProblemCombos = new HashSet<string>
    {
        "死生","生死","無用","無聊","白痴","白癡","廢物","倒霉",
        "小三","小四","二奶","王八","烏龜","狗屎"
    };

    public static readonly HashSet<string> EasyMisread = new HashSet<string>
    {
        "于於","余餘","涂塗","后後","里裡","制製"
    };

    public static string IsGloballyBlocked(
        string surname, string given, ICharacterRepository repo, IEnumerable<string> extra)
    {
        var hard = HardRules.Evaluate(surname, given, extra, repo);
        if (hard.Blocked) return hard.Reason;
        if (ProblemCombos.Contains(given)) return "常見問題組合";
        if (EasyMisread.Contains(given)) return "容易誤讀組合";
        return "";
    }
}
}
