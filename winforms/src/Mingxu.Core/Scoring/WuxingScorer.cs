using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Constants;
using Mingxu.Core.Models;

namespace Mingxu.Core.Scoring
{
public sealed class WuxingScoreResult
{
    public Dictionary<string, int> Weights { get; set; } = new Dictionary<string, int>();
    public int PairBonus { get; set; }
    public string PairRelation { get; set; } = "";
    public int FlowBonus { get; set; }
    public string FlowPath { get; set; } = "";
    public int Raw { get; set; }
    public double Score { get; set; }
    public List<string> Notes { get; set; } = new List<string>();
}

public static class WuxingScorer
{
    public const int XiPrimary = 20;
    public const int XiSecond = 15;
    public const int XiCi = 5;
    public const int Ji = -10;
    public const int JiCi = -15;
    public const int Tiao = 8;
    public const int PairSheng = 10;
    public const int PairSamePositive = 4;
    public const int PairSameNegative = -10;
    public const int PairReverseSheng = 3;
    public const int PairKe = -12;
    public const int PairReverseKe = -14;
    public const int FlowMingZi = 6;
    public const int FlowSan = 10;
    public const int FlowXiyong = 5;

    public static Dictionary<string, int> ElementWeights(Pillars pillars)
    {
        var weights = WuXing.Order.ToDictionary(x => x, x => 0);
        if (pillars.XiYong.Count > 0) weights[pillars.XiYong[0]] = XiPrimary;
        if (pillars.XiYong.Count > 1) weights[pillars.XiYong[1]] = XiSecond;
        AssignIfZero(weights, pillars.XiCi, XiCi);
        AssignIfZero(weights, pillars.JiShen, Ji);
        AssignIfZero(weights, pillars.JiCi, JiCi);
        AssignIfZero(weights, pillars.TiaoHou, Tiao);
        return weights;
    }

    public static int PairBonus(string a, string b, IDictionary<string, int> weights, out string label)
    {
        label = "";
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        if (a == b)
        {
            label = a + "比和";
            int weight;
            return weights.TryGetValue(a, out weight) && weight < 0 ? PairSameNegative : PairSamePositive;
        }
        if (WuXing.Generates(a, b)) { label = a + "→" + b + "順生"; return PairSheng; }
        if (WuXing.Generates(b, a)) { label = b + "→" + a + "逆生"; return PairReverseSheng; }
        if (WuXing.Controls(a, b)) { label = a + "克" + b; return PairKe; }
        if (WuXing.Controls(b, a)) { label = b + "克" + a; return PairReverseKe; }
        label = a + "+" + b;
        return 0;
    }

    public static int FlowBonus(
        IEnumerable<string> givenWuxing,
        string surnameWx,
        IEnumerable<string> xiChain,
        out string path)
    {
        var given = (givenWuxing ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrEmpty(x)).ToList();
        var xi = (xiChain ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrEmpty(x)).ToList();
        var bonus = 0;
        var tags = new List<string>();
        if (given.Count >= 2 && IsShengChain(given.Take(2)))
        {
            bonus += FlowMingZi;
            tags.Add("名→字 " + string.Join("→", given.Take(2)));
        }
        if (!string.IsNullOrEmpty(surnameWx) && given.Count >= 2 &&
            IsShengChain(new[] { surnameWx, given[0], given[1] }))
        {
            bonus += FlowSan;
            tags.Add("姓名流通 " + surnameWx + "→" + given[0] + "→" + given[1]);
        }
        else if (!string.IsNullOrEmpty(surnameWx) && given.Count == 1 &&
            IsShengChain(new[] { surnameWx, given[0] }))
        {
            bonus += FlowMingZi;
            tags.Add("姓→名 " + surnameWx + "→" + given[0]);
        }
        if (given.Count >= 2 && xi.Count >= 2 && given[0] == xi[0] && given[1] == xi[1])
        {
            bonus += FlowXiyong;
            tags.Add("喜用流通 " + xi[0] + "→" + xi[1]);
        }
        path = string.Join("；", tags);
        return bonus;
    }

    public static WuxingScoreResult Score(
        Pillars pillars,
        IEnumerable<string> charWuxing,
        string surnameWx,
        IEnumerable<string> givenChars)
    {
        var weights = ElementWeights(pillars);
        var elems = (charWuxing ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrEmpty(x)).ToList();
        var chars = (givenChars ?? Enumerable.Empty<string>()).ToList();
        var notes = new List<string>
        {
            "五行權重（依八字）：" + string.Join("　", WuXing.Order.Select(x => x + weights[x].ToString("+0;-0;0")))
        };
        var raw = 0;
        for (var i = 0; i < elems.Count; i++)
        {
            var wx = elems[i];
            var ch = i < chars.Count ? chars[i] : "";
            int weight;
            if (!weights.TryGetValue(wx, out weight)) weight = 0;
            raw += weight;
            notes.Add("單字" + (ch.Length > 0 ? "「" + ch + "」" : "") + wx + " " + weight.ToString("+0;-0;0"));
        }

        var pairBonus = 0;
        var pairRelation = "";
        if (elems.Count >= 2)
        {
            pairBonus = PairBonus(elems[0], elems[1], weights, out pairRelation);
            raw += pairBonus;
            notes.Add("組合 " + elems[0] + "+" + elems[1] + "（" + pairRelation + "）" +
                pairBonus.ToString("+0;-0;0"));
        }

        var xiChain = pillars.XiYong.Take(2).ToList();
        if (xiChain.Count < 2)
            xiChain.AddRange(pillars.XiCi.Where(x => !xiChain.Contains(x)).Take(2 - xiChain.Count));
        string flowPath;
        var flowBonus = FlowBonus(elems, surnameWx, xiChain, out flowPath);
        raw += flowBonus;
        if (flowBonus != 0)
            notes.Add("流通 " + flowPath + " " + flowBonus.ToString("+0;-0;0"));

        var score = Math.Max(0, Math.Min(100, Math.Round(50.0 + raw, 1)));
        notes.Add("五行評分 " + score.ToString("0") + "（原始" + raw.ToString("+0;-0;0") + "）");
        return new WuxingScoreResult
        {
            Weights = weights,
            PairBonus = pairBonus,
            PairRelation = pairRelation,
            FlowBonus = flowBonus,
            FlowPath = flowPath,
            Raw = raw,
            Score = score,
            Notes = notes
        };
    }

    private static void AssignIfZero(Dictionary<string, int> weights, IEnumerable<string> elements, int value)
    {
        foreach (var wx in elements ?? Enumerable.Empty<string>())
            if (weights.ContainsKey(wx) && weights[wx] == 0) weights[wx] = value;
    }

    private static bool IsShengChain(IEnumerable<string> sequence)
    {
        var list = sequence.Where(x => !string.IsNullOrEmpty(x)).ToList();
        if (list.Count < 2) return false;
        for (var i = 0; i < list.Count - 1; i++)
            if (!WuXing.Generates(list[i], list[i + 1])) return false;
        return true;
    }
}
}
