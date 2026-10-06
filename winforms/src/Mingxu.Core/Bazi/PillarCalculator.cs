using Lunar;
using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Constants;
using Mingxu.Core.Models;

namespace Mingxu.Core.Bazi
{

internal sealed class XijiResult
{
    public List<string> XiYong { get; set; }
    public List<string> XiCi { get; set; }
    public List<string> JiShen { get; set; }
    public List<string> JiCi { get; set; }

    public XijiResult(List<string> xiYong, List<string> xiCi, List<string> jiShen, List<string> jiCi)
    {
        XiYong = xiYong;
        XiCi = xiCi;
        JiShen = jiShen;
        JiCi = jiCi;
    }
}

public static class PillarCalculator
{
    public static Pillars Compute(
        DateTime birth,
        string gender = "M",
        bool useTrueSolar = false,
        double longitude = 121.56,
        string birthPlace = "")
    {
        var dt = birth;
        var notes = new List<string>();
        if (useTrueSolar)
        {
            dt = dt.AddMinutes((longitude - 120.0) * 4.0);
            var placeTxt = string.IsNullOrWhiteSpace(birthPlace) ? "" : birthPlace + "、";
            notes.Add($"已依{placeTxt}東經 {longitude:0.0}° 換算真太陽時（相對東經120°）。");
        }

        var solar = Solar.FromYmdHms(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
        var lunar = solar.Lunar;
        var eight = lunar.EightChar;

        var yg = eight.YearGan; var yz = eight.YearZhi;
        var mg = eight.MonthGan; var mz = eight.MonthZhi;
        var dg = eight.DayGan; var dz = eight.DayZhi;
        var hg = eight.TimeGan; var hz = eight.TimeZhi;
        var dayWx = WuXing.StemWuxing[dg];

        var yearP = MakePillar(yg, yz, dg, false);
        var monthP = MakePillar(mg, mz, dg, false);
        var dayP = MakePillar(dg, dz, dg, true);
        var hourP = MakePillar(hg, hz, dg, false);

        var power = Accumulate(
            new List<string> { yg, mg, dg, hg },
            new List<string> { yz, mz, dz, hz },
            mz);
        var totalP = power.Values.Sum();
        if (totalP <= 0) totalP = 1;
        var share = WuXing.Order.ToDictionary(wx => wx, wx => Math.Round(100.0 * power[wx] / totalP, 1));
        var levels = share.ToDictionary(kv => kv.Key, kv => ElementLevel(kv.Value));
        var scores100 = share.ToDictionary(kv => kv.Key, kv => Math.Max(0, Math.Min(100, (int)Math.Round(kv.Value * 2.2))));

        var strength100 = Strength100(dayWx, power);
        var strength = strength100 >= 58 ? "身強" : strength100 <= 42 ? "身弱" : "中和";
        var tiao = TiaoHou(mz, dayWx);
        var xiji = PickXiji(dayWx, strength100, levels, power, tiao);

        notes.Add("五行分布：" + string.Join("、", WuXing.Order.Select(wx => $"{wx}{levels[wx]}（{share[wx]:0}%）")) + "。");
        if (strength == "身強")
            notes.Add("扶抑：身強喜洩耗克，忌比劫印再扶；並參調候。");
        else if (strength == "身弱")
            notes.Add("扶抑：身弱喜生扶，忌洩耗克；並參調候。");
        else
            notes.Add("扶抑：中和局，以調候為主、生扶為輔。");

        var lunarText = $"農曆{lunar.Year}年{lunar.MonthInChinese}月{lunar.DayInChinese}";
        try { lunarText += $" {lunar.TimeZhi}時"; } catch { /* ignore */ }
        if (!string.IsNullOrWhiteSpace(birthPlace))
            lunarText += $"　出生地{birthPlace}";

        return new Pillars
        {
            Year = yearP,
            Month = monthP,
            Day = dayP,
            Hour = hourP,
            DayMaster = dg,
            DayMasterWuxing = dayWx,
            Strength = strength,
            Strength100 = strength100,
            ElementScores = scores100,
            ElementLevels = levels,
            ElementShare = share,
            XiYong = xiji.XiYong,
            XiCi = xiji.XiCi,
            JiShen = xiji.JiShen,
            JiCi = xiji.JiCi,
            TiaoHou = tiao,
            Notes = notes,
            LunarText = lunarText,
            Zodiac = WuXing.BranchZodiac[yz],
            YearBranch = yz,
            BirthPlace = birthPlace,
            Longitude = longitude,
            Gender = gender,
        };
    }

    public static List<DaYunEntry> DaYunList(DateTime birth, string gender, int startYear, int endYear)
    {
        if (endYear < startYear)
        {
            var t = startYear;
            startYear = endYear;
            endYear = t;
        }
        var solar = Solar.FromYmdHms(birth.Year, birth.Month, birth.Day, birth.Hour, birth.Minute, birth.Second);
        var yun = solar.Lunar.EightChar.GetYun(gender == "M" ? 1 : 0);
        var outList = new List<DaYunEntry>();
        foreach (var da in yun.GetDaYun())
        {
            string gz;
            try { gz = da.GanZhi; } catch { gz = ""; }
            foreach (var ln in da.GetLiuNian())
            {
                var y = ln.Year;
                if (y >= startYear && y <= endYear)
                    outList.Add(new DaYunEntry(y, ln.GanZhi, gz));
            }
        }
        return outList;
    }

    /// <summary>相容舊呼叫：由今年起算若干年。</summary>
    public static List<DaYunEntry> DaYunList(DateTime birth, string gender, int years = 10)
    {
        var start = DateTime.Now.Year;
        var n = years <= 0 ? 10 : years;
        return DaYunList(birth, gender, start, start + n - 1);
    }

    private static string ShiShen(string dayGan, string otherGan)
    {
        if (string.IsNullOrEmpty(dayGan) || string.IsNullOrEmpty(otherGan)) return "";
        var dw = WuXing.StemWuxing[dayGan];
        var ow = WuXing.StemWuxing[otherGan];
        var sameYy = WuXing.StemYang.Contains(dayGan) == WuXing.StemYang.Contains(otherGan);
        if (dw == ow) return sameYy ? "比肩" : "劫財";
        if (WuXing.Generates(dw, ow)) return sameYy ? "食神" : "傷官";
        if (WuXing.Generates(ow, dw)) return sameYy ? "偏印" : "正印";
        if (WuXing.Controls(dw, ow)) return sameYy ? "偏財" : "正財";
        if (WuXing.Controls(ow, dw)) return sameYy ? "七殺" : "正官";
        return "";
    }

    private static string MonthWang(string zhi)
    {
        if (zhi == "寅" || zhi == "卯") return "木";
        if (zhi == "巳" || zhi == "午") return "火";
        if (zhi == "申" || zhi == "酉") return "金";
        if (zhi == "亥" || zhi == "子") return "水";
        return "土";
    }

    private static string YinWx(string dayWx) => WuXing.Sheng.First(kv => kv.Value == dayWx).Key;
    private static string GuanWx(string dayWx) => WuXing.Ke.First(kv => kv.Value == dayWx).Key;

    private static List<string> TiaoHou(string monthZhi, string dayWx)
    {
        if ("亥子丑".Contains(monthZhi))
        {
            var prefer = new List<string> { "火" };
            if (dayWx == "金" || dayWx == "水") prefer.Add("木");
            return prefer;
        }
        if ("巳午未".Contains(monthZhi))
        {
            var prefer = new List<string> { "水" };
            if (dayWx == "火" || dayWx == "土") prefer.Add("金");
            return prefer;
        }
        if ("寅卯辰".Contains(monthZhi))
        {
            if (dayWx == "木") return new List<string> { "火", "金" };
            if (dayWx == "水") return new List<string> { "火", "土" };
            return new List<string> { "火" };
        }
        if (dayWx == "金") return new List<string> { "水", "火" };
        if (dayWx == "土") return new List<string> { "水", "金" };
        return new List<string> { "水" };
    }

    private static string ElementLevel(double share)
    {
        if (share < 8) return "過弱";
        if (share < 16) return "偏弱";
        if (share < 28) return "適中";
        if (share < 40) return "偏旺";
        return "過旺";
    }

    private static Dictionary<string, double> Accumulate(List<string> stems, List<string> branches, string monthZhi)
    {
        var power = WuXing.Order.ToDictionary(wx => wx, unusedElement => 0.0);
        foreach (var gan in stems)
            power[WuXing.StemWuxing[gan]] += 1.05;
        for (var i = 0; i < branches.Count; i++)
        {
            var zhi = branches[i];
            var boost = zhi == monthZhi ? 1.85 : 1.0;
            List<HiddenStem> hides;
            if (!WuXing.ZhiCanggan.TryGetValue(zhi, out hides)) continue;
            foreach (var hidden in hides)
                power[WuXing.StemWuxing[hidden.Gan]] += hidden.Weight * boost;
        }
        power[MonthWang(monthZhi)] += 1.6;
        return power;
    }

    private static int Strength100(string dayWx, Dictionary<string, double> power)
    {
        var yin = YinWx(dayWx);
        var guan = GuanWx(dayWx);
        var support = power[dayWx] + power[yin];
        var drain = power[WuXing.Sheng[dayWx]] + power[WuXing.Ke[dayWx]] + power[guan];
        var total = support + drain;
        if (total <= 0) return 50;
        return Math.Max(0, Math.Min(100, (int)Math.Round(100.0 * support / total)));
    }

    private static XijiResult PickXiji(
        string dayWx, int strength100, Dictionary<string, string> levels, Dictionary<string, double> scores, List<string> tiao)
    {
        var yin = YinWx(dayWx);
        var guan = GuanWx(dayWx);
        var bijie = dayWx;
        var shishang = WuXing.Sheng[dayWx];
        var cai = WuXing.Ke[dayWx];
        var dayLevel = DictionaryValue(levels, dayWx, "適中");
        var weakish = dayLevel == "過弱" || dayLevel == "偏弱";

        List<string> helpers, harmers;
        if (strength100 <= 42)
        {
            helpers = weakish ? new List<string> { bijie, yin } : new List<string> { yin, bijie };
            harmers = new List<string> { guan, cai, shishang };
        }
        else if (strength100 >= 58)
        {
            helpers = new List<string> { shishang, cai, guan };
            harmers = new List<string> { bijie, yin };
        }
        else
        {
            helpers = strength100 < 50 ? new List<string> { yin, bijie, shishang } : new List<string> { shishang, cai, yin };
            harmers = strength100 < 50 ? new List<string> { guan, cai } : new List<string> { bijie, yin };
        }

        var xiCandidates = new List<string>();
        if (strength100 <= 50 && weakish) xiCandidates.Add(bijie);
        foreach (var wx in tiao.Concat(helpers))
        {
            if (!xiCandidates.Contains(wx) && DictionaryValue(levels, wx, "") != "過旺")
                xiCandidates.Add(wx);
        }

        harmers = harmers.Where(h => !xiCandidates.Contains(h)).Distinct().OrderByDescending(w => DictionaryValue(scores, w, 0)).ToList();
        var xiYong = xiCandidates.Take(2).ToList();
        var xiCi = xiCandidates.Skip(xiYong.Count).Where(w => !xiYong.Contains(w)).Take(1).ToList();
        var jiShen = harmers.Take(1).ToList();
        var jiCi = harmers.Skip(1).Where(w => !jiShen.Contains(w)).Take(1).ToList();
        if (jiShen.Count == 0)
        {
            var leftover = WuXing.Order.Where(w => !xiYong.Contains(w) && !xiCi.Contains(w))
                .OrderByDescending(w => DictionaryValue(scores, w, 0)).ToList();
            jiShen = leftover.Take(1).ToList();
            jiCi = leftover.Skip(1).Take(1).ToList();
        }
        return new XijiResult(xiYong, xiCi, jiShen, jiCi);
    }

    private static Pillar MakePillar(string gan, string zhi, string dayGan, bool isDay)
    {
        List<HiddenStem> list;
        var hides = WuXing.ZhiCanggan.TryGetValue(zhi, out list)
            ? list.Select(x => x.Gan).ToList()
            : new List<string>();
        var hideSs = hides.Select(g => isDay && g == dayGan ? "日主" : ShiShen(dayGan, g)).ToList();
        return new Pillar
        {
            Gan = gan,
            Zhi = zhi,
            GanWuxing = WuXing.StemWuxing[gan],
            ZhiWuxing = WuXing.BranchWuxing[zhi],
            ShishenGan = isDay ? "日主" : ShiShen(dayGan, gan),
            HideGans = hides,
            HideShishen = hideSs,
        };
    }

    private static TValue DictionaryValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        TValue value;
        return dictionary.TryGetValue(key, out value) ? value : defaultValue;
    }
}
}
