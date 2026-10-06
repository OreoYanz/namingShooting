using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Core.Scoring
{
internal sealed class ZodiacRule
{
    public string[] Prefer { get; set; }
    public string[] Avoid { get; set; }
    public string[] LikeParts { get; set; }
    public string[] AvoidParts { get; set; }
}

public sealed class ZodiacYearResult
{
    public double Score { get; set; }
    public string Note { get; set; }
}

public static class ZodiacScorer
{
    private static readonly Dictionary<string, ZodiacRule> Rules = BuildRules();
    private static readonly Dictionary<string, string> RadicalAliases = new Dictionary<string, string>
    {
        {"艹","艸"},{"艸","艸"},{"氵","水"},{"水","水"},{"冫","水"},{"灬","火"},{"火","火"},
        {"忄","心"},{"心","心"},{"釒","金"},{"钅","金"},{"金","金"},{"刂","刀"},{"刀","刀"},
        {"辶","辵"},{"辵","辵"},{"扌","手"},{"手","手"},{"亻","人"},{"人","人"}
    };

    public static double Score(string animal, IList<CharacterInfo> chars)
    {
        ZodiacRule rule;
        if (!Rules.TryGetValue(animal ?? "", out rule)) return 50;
        var raw = 0;
        foreach (var info in chars)
        {
            if (info == null) continue;
            if (rule.Prefer.Contains(info.Wuxing)) raw += 15;
            else if (rule.Avoid.Contains(info.Wuxing)) raw -= 12;
            if (PartHit(info, rule.LikeParts)) raw += 10;
            if (PartHit(info, rule.AvoidParts)) raw -= 10;
        }
        return Math.Max(0, Math.Min(100, Math.Round(50.0 + raw, 1)));
    }

    public static double ZodiacCharBias(string animal, CharacterInfo info)
    {
        ZodiacRule rule;
        if (info == null || !Rules.TryGetValue(animal ?? "", out rule)) return 0;
        var delta = rule.Prefer.Contains(info.Wuxing) ? 8 : rule.Avoid.Contains(info.Wuxing) ? -6 : 0;
        if (PartHit(info, rule.LikeParts)) delta += 5;
        if (PartHit(info, rule.AvoidParts)) delta -= 5;
        return delta;
    }

    public static ZodiacYearResult ZodiacYearScore(string selfAnimal, string yearAnimal)
    {
        const string animals = "鼠牛虎兔龍蛇馬羊猴雞狗豬";
        const string branches = "子丑寅卯辰巳午未申酉戌亥";
        var si = animals.IndexOf(selfAnimal ?? "", StringComparison.Ordinal);
        var yi = animals.IndexOf(yearAnimal ?? "", StringComparison.Ordinal);
        if (si < 0 || yi < 0) return new ZodiacYearResult { Score = 70, Note = "生肖資料不足" };
        var a = branches[si].ToString();
        var b = branches[yi].ToString();
        var labels = RelationLabels(a, b);
        if (labels.Count == 0)
            return new ZodiacYearResult { Score = 70, Note = "本命" + selfAnimal + "與流年" + yearAnimal + "無特殊合沖刑害。" };
        var score = 70.0;
        var notes = new List<string>();
        foreach (var label in labels)
        {
            if (label == "值太歲") { score -= 12; notes.Add("值太歲，宜守成、慎重大決策"); }
            else if (label == "六沖") { score -= 18; notes.Add("六沖（犯太歲），變動大、宜謹慎"); }
            else if (label == "三刑" || label == "子卯刑" || label == "自刑") { score -= 14; notes.Add(label + "，人事易生是非"); }
            else if (label == "相害") { score -= 10; notes.Add("相害，暗耗、小人"); }
            else if (label == "相破") { score -= 8; notes.Add("相破，計畫易中挫"); }
            else if (label == "六合" || label == "三合") { score += 12; notes.Add(label + "，貴人、合作較順"); }
        }
        return new ZodiacYearResult { Score = Math.Max(0, Math.Min(100, Math.Round(score, 1))), Note = string.Join("；", notes) };
    }

    public static bool IsSixChong(string a, string b)
    {
        return PairIn(a, b, new[] {"子午","丑未","寅申","卯酉","辰戌","巳亥"});
    }

    private static List<string> RelationLabels(string a, string b)
    {
        var labels = new List<string>();
        if (a == b) labels.Add("值太歲");
        if (IsSixChong(a, b)) labels.Add("六沖");
        if (PairIn(a, b, new[] {"子丑","寅亥","卯戌","辰酉","巳申","午未"})) labels.Add("六合");
        if (PairIn(a, b, new[] {"子未","丑午","寅巳","卯辰","申亥","酉戌"})) labels.Add("相害");
        if (PairIn(a, b, new[] {"子酉","丑辰","寅亥","卯午","巳申","未戌"})) labels.Add("相破");
        if (GroupIn(a, b, new[] {"申子辰","亥卯未","寅午戌","巳酉丑"})) labels.Add("三合");
        if (GroupIn(a, b, new[] {"寅巳申","丑戌未"})) labels.Add("三刑");
        if (PairIn(a, b, new[] {"子卯"})) labels.Add("子卯刑");
        if (a == b && "辰午酉亥".Contains(a)) labels.Add("自刑");
        return labels.Distinct().ToList();
    }

    private static bool PairIn(string a, string b, IEnumerable<string> pairs)
    {
        return pairs.Any(x => x.Contains(a) && x.Contains(b) && (a != b || x[0].ToString() == a && x[1].ToString() == b));
    }

    private static bool GroupIn(string a, string b, IEnumerable<string> groups)
    {
        return a != b && groups.Any(x => x.Contains(a) && x.Contains(b));
    }

    private static bool PartHit(CharacterInfo info, IEnumerable<string> parts)
    {
        var radical = Normalize(info.Radical);
        foreach (var part in parts)
            if (info.Char == part || info.Radical == part || radical == Normalize(part)) return true;
        return false;
    }

    private static string Normalize(string value)
    {
        string normalized;
        return RadicalAliases.TryGetValue(value ?? "", out normalized) ? normalized : value ?? "";
    }

    private static Dictionary<string, ZodiacRule> BuildRules()
    {
        var map = new Dictionary<string, ZodiacRule>();
        map["鼠"] = Rule("水木","土","氵水雨田米宀穴","土山石馬午");
        map["牛"] = Rule("火土","木","火土山石玉","木竹艹艸禾");
        map["虎"] = Rule("水木","金","木竹艹艸禾氵水","金刂刀戈酉");
        map["兔"] = Rule("水木","金","木竹艹艸禾氵水","金刂刀戈酉雞");
        map["龍"] = Rule("火土水","木","火土山水氵","木竹艹艸虎犬");
        map["蛇"] = Rule("木火","水","木火日灬","氵水雨豬亥");
        map["馬"] = Rule("木火","水","木火日灬","氵水雨鼠子");
        map["羊"] = Rule("火土","木","火土山石玉","木竹艹艸");
        map["猴"] = Rule("土金","火","土金石玉","火灬日虎寅");
        map["雞"] = Rule("土金","火","土金石玉","火灬日兔卯");
        map["狗"] = Rule("火土","木","火土山石","木竹艹艸龍辰");
        map["豬"] = Rule("水木","土","氵水雨木","土山石蛇巳");
        return map;
    }

    private static ZodiacRule Rule(string prefer, string avoid, string like, string avoidParts)
    {
        return new ZodiacRule
        {
            Prefer = prefer.Select(x => x.ToString()).ToArray(),
            Avoid = avoid.Select(x => x.ToString()).ToArray(),
            LikeParts = like.Select(x => x.ToString()).ToArray(),
            AvoidParts = avoidParts.Select(x => x.ToString()).ToArray()
        };
    }
}
}
