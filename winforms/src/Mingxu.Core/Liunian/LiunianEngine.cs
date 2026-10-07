using Lunar;
using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Bazi;
using Mingxu.Core.Constants;
using Mingxu.Core.Models;
using Mingxu.Core.Scoring;

namespace Mingxu.Core.Liunian
{
/// <summary>
/// 流年／流月本地計算引擎。數值與干支／節氣皆在此產生；ChatGPT 僅可潤飾文字欄位。
/// </summary>
public static class LiunianEngine
{
    public static List<YearLuck> Compute(
        Pillars pillars,
        NameSuggestion sug,
        DateTime birth,
        string gender,
        int startYear,
        int endYear,
        bool includeMonths,
        bool detailedMonths,
        bool childDomains = false)
    {
        if (endYear < startYear)
        {
            var t = startYear;
            startYear = endYear;
            endYear = t;
        }
        endYear = Math.Min(endYear, startYear + 49);

        var useTrueSolar = pillars != null && pillars.UseTrueSolar;
        var longitude = pillars != null ? pillars.Longitude : 121.56;
        var map = PillarCalculator.DaYunList(birth, gender, startYear, endYear, useTrueSolar, longitude)
            .GroupBy(x => x.Year)
            .ToDictionary(g => g.Key, g => g.First());
        var list = new List<YearLuck>();
        for (var y = startYear; y <= endYear; y++)
        {
            var year = BuildYear(pillars, sug, birth, y, map, childDomains);
            if (includeMonths)
            {
                year.Months = BuildMonths(pillars, sug, birth, y, year, detailedMonths, childDomains);
                ApplyMonthRankings(year, childDomains);
            }
            list.Add(year);
        }
        return list;
    }

    public static List<YearLuck> Compute(
        Pillars pillars,
        NameSuggestion sug,
        DateTime birth,
        string gender,
        int years,
        bool includeMonths = false,
        bool childDomains = false)
    {
        var start = DateTime.Now.Year;
        var n = years <= 0 ? 10 : Math.Min(50, years);
        return Compute(pillars, sug, birth, gender, start, start + n - 1, includeMonths, true, childDomains);
    }

    private static YearLuck BuildYear(
        Pillars pillars,
        NameSuggestion sug,
        DateTime birth,
        int year,
        Dictionary<int, DaYunEntry> map,
        bool childDomains)
    {
        var age = year - birth.Year + 1;
        DaYunEntry row;
        map.TryGetValue(year, out row);
        var gz = row == null ? "" : row.LiuNian;
        if (string.IsNullOrEmpty(gz))
        {
            try
            {
                var solar = Solar.FromYmdHms(year, 5, 6, 12, 0, 0);
                gz = solar.Lunar.YearInGanZhi;
            }
            catch { gz = ""; }
        }

        string zodiac;
        var animal = gz.Length >= 2 && WuXing.BranchZodiac.TryGetValue(gz[1].ToString(), out zodiac) ? zodiac : "";
        var yearGan = gz.Length >= 1 ? gz[0].ToString() : "";
        string ywx;
        if (!WuXing.StemWuxing.TryGetValue(yearGan, out ywx)) ywx = "";

        string baziNote;
        var bazi = ScoreStemAgainstPillars(pillars, yearGan, ywx, out baziNote);
        var nameYear = NameYearLuck(sug == null ? null : sug.Wuge, age);
        var nameScore = nameYear.Score;
        var zodiacYear = ZodiacScorer.ZodiacYearScore(pillars.Zodiac, animal);
        var zodiacScore = zodiacYear.Score;

        var risks = new List<string>();
        var advice = new List<string>();
        var yearBranch = gz.Length >= 2 ? gz[1].ToString() : "";
        if (ZodiacScorer.IsSixChong(pillars.Day.Zhi, yearBranch))
        {
            bazi = Math.Max(0, bazi - 8);
            risks.Add("日支與流年地支相沖，情緒、人際或作息易變");
            advice.Add("重大決策預留緩衝，避免急進");
        }
        BuildAgeOutlook(age, risks, advice);
        if (zodiacScore < 55) risks.Add("生肖流年有沖刑害破，計畫易有反覆");
        if (nameScore < 55) advice.Add("姓名數理流年偏弱，宜以穩健累積為主");

        var total = Math.Round((bazi + nameScore + zodiacScore) / 3.0, 1);
        var career = Clamp(total + ((bazi - 55) * 0.15) + ((nameScore - 55) * 0.08));
        var wealth = Clamp(total + ((bazi - 55) * 0.12) + ((zodiacScore - 55) * 0.1));
        var relationship = Clamp(total + ((zodiacScore - 55) * 0.18));
        var level = LevelFromScore(total);
        var keyword = KeywordFromScore(total);
        var outlook = AgeProfile(age) + "階段：" +
            (total >= 80 ? "順勢拓展，仍宜保持節奏。" : total >= 65 ? "穩中有進，可把握合作機會。" :
            total >= 50 ? "平穩為主，先整頓再推進。" : "波動較多，宜守成並照顧身心。");

        var suitable = BuildYearSuitable(total, bazi);
        var avoid = BuildYearAvoid(total, bazi);
        var overall = outlook;
        string careerText, wealthText, relationText, lifeText;
        if (childDomains)
        {
            careerText = DomainText(LiunianDomainLabels.Career(true), career,
                "適合探索興趣與累積能力", "宜穩住學習節奏、循序漸進");
            wealthText = DomainText(LiunianDomainLabels.Wealth(true), wealth,
                "適合整理支援與學習環境", "留意過度消耗、保留緩衝");
            relationText = DomainText(LiunianDomainLabels.Relationship(true), relationship,
                "互動較易推進，適合同儕與家人溝通", "宜放慢、多確認彼此期待");
            lifeText = DomainText(LiunianDomainLabels.Life(true), total,
                "適合調整作息與生活安排", "保留緩衝，照顧睡眠與身體節奏");
        }
        else
        {
            careerText = DomainText(LiunianDomainLabels.Career(false), career,
                "推進專案與職涯布局", "穩住節奏、補強專業");
            wealthText = DomainText(LiunianDomainLabels.Wealth(false), wealth,
                "檢視配置與長期累積", "控制支出、避免一次投入過大");
            relationText = DomainText(LiunianDomainLabels.Relationship(false), relationship,
                "拓展合作與互動", "多溝通、少急於下判斷");
            lifeText = DomainText(LiunianDomainLabels.Life(false), total,
                "整理作息與身心節奏", "保留彈性、照顧睡眠與運動");
        }

        return new YearLuck
        {
            Year = year,
            Age = age,
            Ganzhi = gz,
            Animal = animal,
            BaziScore = Math.Round(bazi, 1),
            NameScore = nameScore,
            ZodiacScore = zodiacScore,
            TotalScore = total,
            CareerScore = career,
            WealthScore = wealth,
            RelationshipScore = relationship,
            Level = level,
            Keyword = keyword,
            DaYun = row == null ? "" : row.DaYun ?? "",
            BaziNote = baziNote,
            NameNote = nameYear.Note,
            ZodiacNote = zodiacYear.Note,
            Risks = risks.Distinct().ToList(),
            Advice = advice.Distinct().ToList(),
            Outlook = outlook,
            Overall = overall,
            Career = careerText,
            Wealth = wealthText,
            Relationship = relationText,
            Life = lifeText,
            Suitable = suitable,
            Avoid = avoid,
            Summary = "流年" + gz + "，關鍵字「" + keyword + "」，綜合" + level + "（" + total.ToString("0") + "）。" + outlook,
        };
    }

    private static List<MonthLuck> BuildMonths(
        Pillars pillars,
        NameSuggestion sug,
        DateTime birth,
        int year,
        YearLuck yearLuck,
        bool detailed,
        bool childDomains)
    {
        var months = new List<MonthLuck>();
        for (var m = 1; m <= 12; m++)
            months.Add(BuildMonth(pillars, sug, birth, year, m, yearLuck, detailed, childDomains));
        return months;
    }

    private static MonthLuck BuildMonth(
        Pillars pillars,
        NameSuggestion sug,
        DateTime birth,
        int year,
        int month,
        YearLuck yearLuck,
        bool detailed,
        bool childDomains)
    {
        string monthGz = "";
        string termRange = "";
        try
        {
            var solar = Solar.FromYmdHms(year, month, 15, 12, 0, 0);
            var lunar = solar.Lunar;
            try { monthGz = lunar.MonthInGanZhiExact; }
            catch { monthGz = lunar.MonthInGanZhi; }
            termRange = ResolveSolarTermRange(lunar);
        }
        catch
        {
            monthGz = "";
            termRange = "";
        }

        var stem = monthGz.Length >= 1 ? monthGz[0].ToString() : "";
        var branch = monthGz.Length >= 2 ? monthGz[1].ToString() : "";
        string mwx;
        if (!WuXing.StemWuxing.TryGetValue(stem, out mwx)) mwx = "";
        string note;
        var bazi = ScoreStemAgainstPillars(pillars, stem, mwx, out note);
        if (ZodiacScorer.IsSixChong(pillars.Day.Zhi, branch))
            bazi = Math.Max(0, bazi - 6);

        var age = year - birth.Year + 1;
        var nameBase = yearLuck != null ? yearLuck.NameScore : 55;
        var nameScore = Clamp(nameBase + (((month + age) % 5) - 2) * 1.5);
        var animalMonth = "";
        string zod;
        if (branch.Length > 0 && WuXing.BranchZodiac.TryGetValue(branch, out zod))
            animalMonth = zod;
        var zodiacScore = ZodiacScorer.ZodiacYearScore(pillars.Zodiac, string.IsNullOrEmpty(animalMonth) ? yearLuck.Animal : animalMonth).Score;
        var total = Math.Round((bazi * 0.45) + (nameScore * 0.25) + (zodiacScore * 0.30), 1);
        var level = MonthLevelFromScore(total);

        var monthLuck = new MonthLuck
        {
            Year = year,
            Month = month,
            MonthGanzhi = monthGz,
            SolarTermRange = termRange,
            BaziScore = Math.Round(bazi, 1),
            NameScore = Math.Round(nameScore, 1),
            ZodiacScore = Math.Round(zodiacScore, 1),
            TotalScore = total,
            Level = level,
            Suitable = MonthSuitable(level),
            Avoid = MonthAvoid(level),
        };
        FillMonthTexts(monthLuck, detailed, note, childDomains);
        return monthLuck;
    }

    private static void FillMonthTexts(MonthLuck m, bool detailed, string baziNote, bool childDomains)
    {
        m.Overall = "流月定位：" + m.Level + "。本月整體節奏偏" + ToneOf(m.Level) + "。";
        if (childDomains)
        {
            m.Career = detailed
                ? LiunianDomainLabels.Career(true) + "：" + DomainHint(m.TotalScore, "適合探索興趣與練習", "宜穩住學習節奏")
                : DomainHint(m.TotalScore, "可推進", "宜穩住");
            m.Wealth = detailed
                ? LiunianDomainLabels.Wealth(true) + "：" + DomainHint(m.TotalScore, "適合整理支援與環境", "留意過度消耗")
                : DomainHint(m.TotalScore, "宜整理", "宜緩衝");
            m.Relationship = detailed
                ? LiunianDomainLabels.Relationship(true) + "：" + DomainHint(m.TotalScore, "互動較易推進，適合溝通", "宜放慢、多確認期待")
                : DomainHint(m.TotalScore, "可互動", "宜溝通");
            m.Life = detailed
                ? LiunianDomainLabels.Life(true) + "：" + DomainHint(m.TotalScore, "適合調整作息與生活安排", "保留緩衝，照顧睡眠與身體節奏")
                : DomainHint(m.TotalScore, "宜調整", "宜緩衝");
        }
        else
        {
            m.Career = detailed
                ? "事業：" + DomainHint(m.TotalScore, "適合推進既有事項與盤點優先序", "宜穩住產出、避免一次擴張過多")
                : DomainHint(m.TotalScore, "可推進", "宜穩住");
            m.Wealth = detailed
                ? "財運：" + DomainHint(m.TotalScore, "適合整理配置與檢視收支", "留意支出節奏、避免衝動投入")
                : DomainHint(m.TotalScore, "宜整理", "宜控管");
            m.Relationship = detailed
                ? "感情／人際：" + DomainHint(m.TotalScore, "互動較易推進，適合溝通協調", "宜放慢決策、多確認彼此期待")
                : DomainHint(m.TotalScore, "可互動", "宜溝通");
            m.Life = detailed
                ? "生活：" + DomainHint(m.TotalScore, "適合調整作息與生活安排", "保留緩衝，照顧睡眠與身體節奏")
                : DomainHint(m.TotalScore, "宜調整", "宜緩衝");
        }
        m.Advice = detailed
            ? "本月建議：以「" + m.Level + "」為基調安排節奏。" +
              (string.IsNullOrEmpty(baziNote) ? "" : "（" + baziNote + "）") +
              (string.IsNullOrEmpty(m.SolarTermRange) ? "" : " 節氣參考：" + m.SolarTermRange + "。")
            : "本月定位：" + m.Level + "。";
        m.Summary = m.Month + "月（" + (string.IsNullOrEmpty(m.MonthGanzhi) ? "—" : m.MonthGanzhi) + "）〔" + m.Level + "〕" +
            m.Overall.Replace("流月定位：" + m.Level + "。", "");
    }

    private static void ApplyMonthRankings(YearLuck year, bool childDomains = false)
    {
        if (year.Months == null || year.Months.Count == 0) return;
        year.StrongMonths = year.Months.Where(m => m.TotalScore >= 78).Select(m => m.Month).ToList();
        year.StableMonths = year.Months.Where(m => m.TotalScore >= 65 && m.TotalScore < 78).Select(m => m.Month).ToList();
        year.AdjustMonths = year.Months.Where(m => m.TotalScore >= 52 && m.TotalScore < 65).Select(m => m.Month).ToList();
        year.CautionMonths = year.Months.Where(m => m.TotalScore < 52).Select(m => m.Month).ToList();

        year.MonthGuideCareer = BuildMonthGuide(LiunianDomainLabels.Career(childDomains), year.Months, m => m.TotalScore + (m.BaziScore - 55) * 0.2);
        year.MonthGuideWealth = BuildMonthGuide(childDomains ? LiunianDomainLabels.Wealth(true) : "財務", year.Months, m => m.TotalScore + (m.BaziScore - 55) * 0.15);
        year.MonthGuideRelationship = BuildMonthGuide(LiunianDomainLabels.Relationship(childDomains), year.Months, m => m.TotalScore + (m.ZodiacScore - 55) * 0.2);
    }

    private static string BuildMonthGuide(string title, List<MonthLuck> months, Func<MonthLuck, double> scoreFn)
    {
        var ranked = months.OrderByDescending(scoreFn).ToList();
        var top = ranked.Take(3).Select(m => m.Month + "月").ToList();
        var low = ranked.Skip(Math.Max(0, ranked.Count - 2)).Select(m => m.Month + "月").ToList();
        return title + "：" + string.Join("、", top) + "較適合推進；" +
            string.Join("、", low) + "建議多保留彈性。";
    }

    public static string FormatYearOverviewTable(IEnumerable<YearLuck> years)
    {
        var lines = new List<string>
        {
            "年度\t整體\t事業\t財運\t感情\t定位"
        };
        foreach (var y in years.OrderBy(x => x.Year))
        {
            lines.Add(string.Format("{0}\t{1:0}\t{2:0}\t{3:0}\t{4:0}\t{5}",
                y.Year, y.TotalScore, y.CareerScore, y.WealthScore, y.RelationshipScore, y.Keyword));
        }
        return string.Join("\n", lines);
    }

    public static string FormatYearDetail(YearLuck y, bool includeMonths)
    {
        if (y == null) return "";
        var lines = new List<string>
        {
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            y.Year + "年度總覽",
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            "流年干支：" + y.Ganzhi + "　生肖：" + y.Animal + "　虛歲：" + y.Age,
            "大運：" + (string.IsNullOrEmpty(y.DaYun) ? "—" : y.DaYun),
            "年度關鍵字：「" + y.Keyword + "」　綜合：" + y.TotalScore.ToString("0") + "（" + y.Level + "）",
            "",
            "整體：" + y.Overall,
            "事業：" + y.Career,
            "財運：" + y.Wealth,
            "感情／人際：" + y.Relationship,
            "生活：" + y.Life,
            "",
            "年度宜：" + string.Join("、", y.Suitable ?? new List<string>()),
            "年度忌：" + string.Join("、", y.Avoid ?? new List<string>()),
        };
        if (y.Advice != null && y.Advice.Count > 0)
            lines.Add("建議：" + string.Join("；", y.Advice));
        if (includeMonths && y.Months != null && y.Months.Count > 0)
        {
            lines.Add("");
            lines.Add("【年度月份重點】");
            lines.Add("⭐ 較適合積極推進：" + FormatMonthList(y.StrongMonths));
            lines.Add("○ 適合穩定累積：" + FormatMonthList(y.StableMonths));
            lines.Add("△ 建議保守調整：" + FormatMonthList(y.AdjustMonths));
            lines.Add("⚠️ 較需留意：" + FormatMonthList(y.CautionMonths));
            lines.Add("");
            lines.Add("【年度月份指南】");
            if (!string.IsNullOrEmpty(y.MonthGuideCareer)) lines.Add(y.MonthGuideCareer);
            if (!string.IsNullOrEmpty(y.MonthGuideWealth)) lines.Add(y.MonthGuideWealth);
            if (!string.IsNullOrEmpty(y.MonthGuideRelationship)) lines.Add(y.MonthGuideRelationship);
            lines.Add("");
            lines.Add("【" + y.Year + "流月】");
            lines.Add("月份\t流月\t節氣區間\t定位\t分數\t重點");
            foreach (var m in y.Months.OrderBy(x => x.Month))
            {
                lines.Add(string.Format("{0}月\t{1}\t{2}\t{3}\t{4:0}\t{5}",
                    m.Month,
                    string.IsNullOrEmpty(m.MonthGanzhi) ? "—" : m.MonthGanzhi,
                    string.IsNullOrEmpty(m.SolarTermRange) ? "—" : m.SolarTermRange,
                    m.Level,
                    m.TotalScore,
                    Truncate(m.Summary, 28)));
            }
        }
        return string.Join("\n", lines);
    }

    public static string FormatMonthDetail(MonthLuck m)
    {
        if (m == null) return "";
        var lines = new List<string>
        {
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            m.Year + "年" + m.Month + "月",
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            "流月干支：" + (string.IsNullOrEmpty(m.MonthGanzhi) ? "—" : m.MonthGanzhi),
            "節氣區間：" + (string.IsNullOrEmpty(m.SolarTermRange) ? "—" : m.SolarTermRange),
            "流月定位：" + m.Level + "　綜合：" + m.TotalScore.ToString("0"),
            "",
            "整體：" + m.Overall,
            "事業：" + m.Career,
            "財運：" + m.Wealth,
            "感情／人際：" + m.Relationship,
            "生活：" + m.Life,
            "",
            "適合：" + string.Join("、", m.Suitable ?? new List<string>()),
            "留意：" + string.Join("、", m.Avoid ?? new List<string>()),
            "",
            "本月建議：" + m.Advice
        };
        return string.Join("\n", lines);
    }

    private static string ResolveSolarTermRange(Lunar.Lunar lunar)
    {
        try
        {
            var prev = lunar.GetPrevJie(false);
            var next = lunar.GetNextQi(false);
            var a = prev == null ? "" : prev.Name;
            var b = next == null ? "" : next.Name;
            if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b))
                return a + " ～ " + b + "前";
            if (!string.IsNullOrEmpty(a)) return a + "起";
            if (!string.IsNullOrEmpty(b)) return b + "前";
        }
        catch { /* ignore */ }
        return "";
    }

    private static double ScoreStemAgainstPillars(Pillars pillars, string gan, string wx, out string note)
    {
        var bazi = 55.0;
        note = "流月／流年天干" + gan + "屬" + (string.IsNullOrEmpty(wx) ? "未知" : wx) + "，命局影響中性";
        if (pillars == null) return bazi;
        if (pillars.XiYong.Contains(wx)) { bazi = 88; note = "天干" + gan + wx + "命中喜用，助力明顯"; }
        else if (pillars.XiCi.Contains(wx)) { bazi = 80; note = "天干" + gan + wx + "命中次喜，較有助力"; }
        else if (pillars.JiShen.Contains(wx)) { bazi = 32; note = "天干" + gan + wx + "命中忌神，宜保守"; }
        else if (pillars.JiCi.Contains(wx)) { bazi = 42; note = "天干" + gan + wx + "命中次忌，需留意波動"; }
        if (pillars.TiaoHou.Contains(wx)) { bazi = Math.Min(100, bazi + 6); note += "；兼具調候作用"; }
        return bazi;
    }

    private static NameYearResult NameYearLuck(WugeResult wuge, int age)
    {
        if (wuge == null) return new NameYearResult(55, "無五格資料");
        int num;
        string stage;
        if (age <= 24) { stage = "地格"; num = wuge.Di; }
        else if (age <= 47) { stage = "人格"; num = wuge.Ren; }
        else { stage = "總格"; num = wuge.Zong; }
        var luck = WuXing.Wuge81Class(num);
        double basePts;
        switch (luck)
        {
            case "大吉": basePts = 92; break;
            case "吉": basePts = 86; break;
            case "次吉": basePts = 70; break;
            case "中性":
            case "中": basePts = 55; break;
            case "凶": basePts = 38; break;
            case "大凶": basePts = 28; break;
            default: basePts = 55; break;
        }
        var cycle = (age + wuge.Zong) % 81;
        if (cycle == 0) cycle = 81;
        var cycleLuck = WuXing.Wuge81Class(cycle);
        var adjust = LuckAdjustment(cycleLuck);
        return new NameYearResult(Math.Max(0, Math.Min(100, Math.Round(basePts + adjust, 1))),
            stage + num + "·" + luck + "；歲序數理(age+總格)%81=" + cycle + "·" + cycleLuck);
    }

    private static double LuckAdjustment(string luck)
    {
        switch (luck)
        {
            case "大吉": return 8;
            case "吉": return 5;
            case "次吉": return 2;
            case "凶": return -6;
            case "大凶": return -10;
            default: return 0;
        }
    }

    private static string LevelFromScore(double avg)
    {
        return avg >= 80 ? "佳" : avg >= 65 ? "平偏佳" : avg >= 50 ? "平" : "慎";
    }

    private static string KeywordFromScore(double avg)
    {
        if (avg >= 85) return "發展突破";
        if (avg >= 75) return "穩中求進";
        if (avg >= 65) return "穩健累積";
        if (avg >= 52) return "調整整頓";
        return "守成慎進";
    }

    private static string MonthLevelFromScore(double score)
    {
        if (score >= 82) return "發展期";
        if (score >= 72) return "上升期";
        if (score >= 60) return "穩定期";
        if (score >= 48) return "調整期";
        return "謹慎期";
    }

    private static string ToneOf(string level)
    {
        if (level == "發展期" || level == "上升期") return "積極可為";
        if (level == "穩定期") return "穩健";
        if (level == "調整期") return "宜整理";
        return "保守";
    }

    private static List<string> MonthSuitable(string level)
    {
        if (level == "發展期" || level == "上升期")
            return new List<string> { "推進", "拓展", "對外溝通" };
        if (level == "穩定期")
            return new List<string> { "規劃", "累積", "整理" };
        if (level == "調整期")
            return new List<string> { "檢視", "調整節奏", "補強細節" };
        return new List<string> { "守成", "休息整頓", "小步前進" };
    }

    private static List<string> MonthAvoid(string level)
    {
        if (level == "謹慎期")
            return new List<string> { "重大冒進", "一次投入過大", "忽略溝通" };
        if (level == "調整期")
            return new List<string> { "過度催進度", "倉促簽約" };
        return new List<string> { "分心多線", "忽略身體節奏" };
    }

    private static List<string> BuildYearSuitable(double total, double bazi)
    {
        if (total >= 75) return new List<string> { "規劃", "累積", "推進合作" };
        if (total >= 55) return new List<string> { "整理", "累積", "規劃" };
        return new List<string> { "整頓", "守成", "補強基本盤" };
    }

    private static List<string> BuildYearAvoid(double total, double bazi)
    {
        if (bazi < 45) return new List<string> { "過度衝動", "一次投入過大", "忽略溝通" };
        if (total < 55) return new List<string> { "急於擴張", "忽略節奏", "單押高風險事項" };
        return new List<string> { "分心多線", "忽略身體與作息" };
    }

    private static string DomainText(string title, double score, string good, string soft)
    {
        return title + "（" + score.ToString("0") + "）：" + (score >= 70 ? good : soft) + "。";
    }

    private static string DomainHint(double score, string good, string soft)
    {
        return score >= 70 ? good : soft;
    }

    private static string FormatMonthList(List<int> months)
    {
        if (months == null || months.Count == 0) return "—";
        return string.Join("、", months.Select(m => m + "月"));
    }

    private static string Truncate(string text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("\n", " ").Trim();
        return text.Length <= max ? text : text.Substring(0, max) + "…";
    }

    private static double Clamp(double v)
    {
        return Math.Max(0, Math.Min(100, Math.Round(v, 1)));
    }

    private static string AgeProfile(int age)
    {
        if (age <= 6) return "學齡前";
        if (age <= 12) return "小學";
        if (age <= 15) return "國中";
        return "成人";
    }

    private static void BuildAgeOutlook(int age, List<string> risks, List<string> advice)
    {
        if (age <= 6)
        {
            risks.Add("幼兒期留意作息、碰撞與季節性不適");
            advice.Add("維持規律睡眠與穩定照護環境");
        }
        else if (age <= 12)
        {
            risks.Add("課業適應與同儕互動可能帶來壓力");
            advice.Add("培養專注習慣，鼓勵表達需求");
        }
        else if (age <= 15)
        {
            risks.Add("青春期情緒與人際敏感度提高");
            advice.Add("兼顧學習節奏、運動與親子溝通");
        }
        else
        {
            risks.Add("工作財務、人際與健康需平衡");
            advice.Add("分段設定目標，保留資金與時間彈性");
        }
    }

    private sealed class NameYearResult
    {
        public double Score { get; private set; }
        public string Note { get; private set; }
        public NameYearResult(double score, string note)
        {
            Score = score;
            Note = note;
        }
    }
}
}
