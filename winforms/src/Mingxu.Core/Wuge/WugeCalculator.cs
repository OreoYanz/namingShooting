using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Constants;
using Mingxu.Core.Models;

namespace Mingxu.Core.Wuge
{

internal sealed class SancaiResult
{
    public string Luck { get; set; }
    public string Note { get; set; }

    public SancaiResult(string luck, string note)
    {
        Luck = luck;
        Note = note;
    }
}

public static class WugeCalculator
{
    private static readonly Dictionary<string, double> GridWeight = new Dictionary<string, double>()
    {
        ["天"] = 0.8, ["人"] = 1.6, ["地"] = 1.1, ["外"] = 0.7, ["總"] = 1.0,
    };
    private const double SancaiWeight = 1.2;

    public static WugeResult Compute(string surname, string given, Func<string, int> strokeOf)
    {
        surname = (surname ?? "").Trim();
        given = (given ?? "").Trim();
        if (surname.Length == 0) throw new ArgumentException("姓氏不可空白");
        if (given.Length == 0) throw new ArgumentException("名字不可空白");

        var isCompound = surname.Length >= 2;
        var surChars = surname.ToCharArray().Select(c => c.ToString()).ToList();
        var givChars = given.ToCharArray().Select(c => c.ToString()).ToList();
        var surStrokes = surChars.Select(strokeOf).ToList();
        var givStrokes = givChars.Select(strokeOf).ToList();
        var all = surStrokes.Concat(givStrokes).ToList();
        var charStrokes = surChars.Concat(givChars).Zip(all, (c, n) => new CharStroke(c, n)).ToList();

        int tian, ren, di, zong, wai;
        if (isCompound)
        {
            tian = surStrokes.Sum();
            ren = surStrokes[surStrokes.Count - 1] + givStrokes[0];
            di = givStrokes.Count >= 2 ? givStrokes.Sum() : givStrokes[0] + 1;
            zong = all.Sum();
            wai = zong - ren;
        }
        else
        {
            tian = surStrokes[0] + 1;
            ren = surStrokes[0] + givStrokes[0];
            di = givStrokes.Count >= 2 ? givStrokes.Sum() : givStrokes[0] + 1;
            zong = all.Sum();
            wai = givStrokes.Count == 1 ? 2 : zong - ren + 1;
        }

        var grids = new Dictionary<string, int> { ["天"] = tian, ["人"] = ren, ["地"] = di, ["外"] = wai, ["總"] = zong };
        var wx = grids.ToDictionary(kv => kv.Key, kv => WuXing.StrokeElement(kv.Value));
        var luck = grids.ToDictionary(kv => kv.Key, kv => WuXing.Wuge81Class(kv.Value));
        var gridPts = luck.ToDictionary(kv => kv.Key, kv => WuXing.WugeLuckPoints(kv.Value));
        var sc = SancaiDetail(wx["天"], wx["人"], wx["地"]);
        var sancaiPts = WuXing.WugeLuckPoints(sc.Luck);
        var raw = grids.Keys.Sum(k => gridPts[k] * GridWeight[k]) + sancaiPts * SancaiWeight;
        var score = Math.Max(0, Math.Min(100, Math.Round(50 + raw, 1)));

        return new WugeResult
        {
            Surname = surname,
            Given = given,
            Tian = tian,
            Ren = ren,
            Di = di,
            Wai = wai,
            Zong = zong,
            TianWx = wx["天"],
            RenWx = wx["人"],
            DiWx = wx["地"],
            WaiWx = wx["外"],
            ZongWx = wx["總"],
            TianLuck = luck["天"],
            RenLuck = luck["人"],
            DiLuck = luck["地"],
            WaiLuck = luck["外"],
            ZongLuck = luck["總"],
            Sancai = $"{wx["天"]}{wx["人"]}{wx["地"]}",
            SancaiLuck = sc.Luck,
            SancaiNote = sc.Note,
            Score = score,
            CharStrokes = charStrokes,
            Notes = new List<string>
            {
                $"康熙筆畫：{string.Join("　", charStrokes.Select(x => $"{x.Char} {x.Stroke}畫"))}",
                $"三才 {wx["天"]}/{wx["人"]}/{wx["地"]} → {sc.Luck}",
                $"人格數理：{WuXing.Wuge81Text(ren)}",
                $"總格數理：{WuXing.Wuge81Text(zong)}",
            },
        };
    }

    private static SancaiResult SancaiDetail(string tian, string ren, string di)
    {
        var r1 = Rel(tian, ren);
        var r2 = Rel(ren, di);
        var map = new Dictionary<string, int>
        {
            ["相生"] = 2, ["比和"] = 1, ["被生"] = 1, ["相克"] = -2, ["被克"] = -2, ["不明"] = 0,
        };
        var score = MapValue(map, r1) + MapValue(map, r2);
        if (r1 == "相克") score -= 1;
        if (r2 == "相克") score -= 1;
        var luck = (r1 == "相生" && r2 == "相生") || score >= 3 ? "大吉"
            : score >= 2 ? "吉"
            : score >= 0 ? "中性"
            : score >= -2 ? "凶" : "大凶";
        return new SancaiResult(luck, $"天人{r1}；人地{r2}");
    }

    private static string Rel(string a, string b)
    {
        if (a == b) return "比和";
        string value;
        if (WuXing.Sheng.TryGetValue(a, out value) && value == b) return "相生";
        if (WuXing.Sheng.TryGetValue(b, out value) && value == a) return "被生";
        if (WuXing.Ke.TryGetValue(a, out value) && value == b) return "相克";
        if (WuXing.Ke.TryGetValue(b, out value) && value == a) return "被克";
        return "不明";
    }

    private static int MapValue(Dictionary<string, int> map, string key)
    {
        int value;
        return map.TryGetValue(key, out value) ? value : 0;
    }
}
}
