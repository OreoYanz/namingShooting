using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mingxu.Core.Models;
using Mingxu.Core.Scoring;
using Mingxu.Core.Llm;

namespace Mingxu.Export.Flow
{
    /// <summary>
    /// 自製字串 HTML 組裝（無第二套 Template Engine）。內嵌 CSS + SVG。
    /// </summary>
    public static class FlowReportHtmlBuilder
    {
        public static string Build(FlowVisualizationData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            var p = data.Profile ?? new FlowProfile();
            var mode = (data.ReportMode ?? "liunian").Trim().ToLowerInvariant();
            var isNaming = mode == "newborn" || mode == "rename";
            var reportTitle = !string.IsNullOrWhiteSpace(data.ReportTitle)
                ? data.ReportTitle
                : (isNaming ? "新生兒命名剖象" : "流年大運分析");
            var sb = new StringBuilder(64 * 1024);
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"zh-Hant\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\" />");
            sb.Append("<title>名序 · ").Append(H(reportTitle)).AppendLine("</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(Css());
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine(BuildWatermarkHtml(data.LogoPath));

            // Cover
            sb.AppendLine("<section class=\"page cover\">");
            if (!string.IsNullOrEmpty(data.LogoPath) && File.Exists(data.LogoPath))
            {
                sb.Append("<div class=\"logo\"><img src=\"")
                    .Append(ToFileUri(data.LogoPath))
                    .AppendLine("\" alt=\"名序\" /></div>");
            }
            sb.AppendLine("<div class=\"brand\">名序</div>");
            sb.AppendLine("<div class=\"slogan-main\">以命為本，以字成名。</div>");
            sb.AppendLine("<div class=\"slogan-sub\">從命理出發，從文字尋名，為每一個名字留下值得珍藏的故事。</div>");
            sb.Append("<div class=\"cover-title\">").Append(H(reportTitle)).AppendLine("</div>");
            if (data.ReportStartYear > 0 && data.ReportEndYear > 0)
            {
                sb.Append("<div class=\"cover-range\">")
                    .Append(data.ReportStartYear)
                    .Append(" ～ ")
                    .Append(data.ReportEndYear)
                    .AppendLine("</div>");
            }
            sb.AppendLine("<div class=\"cover-meta\">");
            sb.Append("<div>姓名：").Append(H(p.FullName)).AppendLine("</div>");
            sb.Append("<div>出生日期：").Append(H(p.BirthDateText)).AppendLine("</div>");
            sb.Append("<div>出生時間：").Append(H(p.BirthTimeText)).AppendLine("</div>");
            sb.Append("<div>性別：").Append(H(p.GenderLabel)).AppendLine("</div>");
            if (!string.IsNullOrWhiteSpace(p.BirthPlace))
                sb.Append("<div>出生地：").Append(H(p.BirthPlace)).AppendLine("</div>");
            if (data.Naming != null && !string.IsNullOrWhiteSpace(data.Naming.ParentsText))
                sb.Append("<div>父母：").Append(H(data.Naming.ParentsText)).AppendLine("</div>");
            if (mode == "rename" && data.Naming != null && !string.IsNullOrWhiteSpace(data.Naming.OriginalName))
                sb.Append("<div>原姓名：").Append(H(data.Naming.OriginalName)).AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.Append("<div class=\"cover-foot\">").Append(H(data.GeneratedAt)).AppendLine("</div>");
            sb.AppendLine("</section>");

            // Profile
            sb.AppendLine("<section class=\"page\">");
            sb.AppendLine("<h1>命盤摘要</h1>");
            sb.AppendLine("<div class=\"grid-2\">");
            Meta(sb, "姓名", p.FullName);
            Meta(sb, "性別", p.GenderLabel);
            Meta(sb, "出生日期", p.BirthDateText);
            Meta(sb, "出生時間", p.BirthTimeText);
            Meta(sb, "出生地", p.BirthPlace);
            Meta(sb, "農曆", p.LunarText);
            sb.AppendLine("</div>");
            sb.AppendLine("<h2>四柱</h2>");
            sb.AppendLine("<div class=\"pillars\">");
            PillarCard(sb, "年柱", p.YearPillar);
            PillarCard(sb, "月柱", p.MonthPillar);
            PillarCard(sb, "日柱", p.DayPillar);
            PillarCard(sb, "時柱", p.HourPillar);
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"grid-2 mt\">");
            Meta(sb, "日主", (p.DayMaster ?? "") + (p.DayMasterWuxing ?? ""));
            Meta(sb, "喜用", p.XiYongText);
            Meta(sb, "忌神", p.JiShenText);
            Meta(sb, "姓名五行", p.CharWuxingText);
            Meta(sb, "綜合評估", string.IsNullOrWhiteSpace(p.Grade) ? NameScorer.GradeLabel(p.TotalScore) : p.Grade);
            sb.AppendLine("</div>");
            sb.AppendLine("<h2>三才五格</h2>");
            sb.AppendLine(BuildWugeBoard(p.Wuge));
            sb.AppendLine("</section>");

            // 命名剖象（新生兒／改名）
            if (isNaming && data.Naming != null)
            {
                sb.AppendLine("<section class=\"page\">");
                sb.AppendLine(mode == "rename" ? "<h1>改名剖象</h1>" : "<h1>命名剖象</h1>");
                if (mode == "rename")
                {
                    sb.AppendLine(BuildRenameCompareCards(data.Naming, p));
                    sb.AppendLine(BuildRenameMessageGrid(data.Naming));
                }
                else if (mode == "newborn")
                {
                    sb.AppendLine("<h2>用字解析</h2>");
                    sb.AppendLine(BuildNewbornCharCards(data.Naming, p));
                    sb.AppendLine(BuildNewbornMessageGrid(data.Naming));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(data.Naming.CharAnalysis))
                    {
                        sb.AppendLine("<h2>用字解析</h2>");
                        sb.Append("<div class=\"prose\">").Append(Nl(data.Naming.CharAnalysis)).AppendLine("</div>");
                    }
                    if (!string.IsNullOrWhiteSpace(data.Naming.Combo))
                        Block(sb, "組合評述", StripNamingScores(data.Naming.Combo));
                    if (!string.IsNullOrWhiteSpace(data.Naming.DestinyNote))
                        Block(sb, "命理對應", StripNamingScores(data.Naming.DestinyNote));
                    if (!string.IsNullOrWhiteSpace(data.Naming.Image))
                        Block(sb, "意象", StripNamingScores(data.Naming.Image));
                    if (!string.IsNullOrWhiteSpace(data.Naming.Story))
                    {
                        sb.AppendLine("<h2>命名故事</h2>");
                        sb.Append("<div class=\"prose\">").Append(Nl(StripNamingScores(data.Naming.Story))).AppendLine("</div>");
                    }
                    if (!string.IsNullOrWhiteSpace(data.Naming.Idea))
                    {
                        sb.AppendLine("<h2>改名理念</h2>");
                        sb.Append("<div class=\"prose\">").Append(Nl(StripNamingScores(data.Naming.Idea))).AppendLine("</div>");
                    }
                    if (!string.IsNullOrWhiteSpace(data.Naming.Blessing))
                    {
                        sb.AppendLine("<h2>祝福</h2>");
                        sb.Append("<div class=\"prose\">").Append(Nl(StripNamingScores(data.Naming.Blessing))).AppendLine("</div>");
                    }
                    if (!string.IsNullOrWhiteSpace(data.Naming.Hope))
                    {
                        sb.AppendLine("<h2>人生期許</h2>");
                        sb.Append("<div class=\"prose\">").Append(Nl(StripNamingScores(data.Naming.Hope))).AppendLine("</div>");
                    }
                }
                sb.AppendLine("</section>");
            }

            var trends = data.YearlyTrend ?? new List<YearTrendPoint>();
            var singleYear = trends.Count < 2;

            // Dayun timeline：多年才顯示整頁；單年改寫進流年總覽標題旁
            if (!singleYear)
            {
                sb.AppendLine("<section class=\"page dayun-page\">");
                sb.AppendLine("<h1>人生大運時間軸</h1>");
                var dayunSegs = FilterDayunForReport(data.DayunTimeline, data.ReportStartYear, data.ReportEndYear);
                sb.AppendLine(BuildDayunSvg(dayunSegs, data.DayunPeriods));
                sb.AppendLine("<div class=\"dayun-list\">");
                foreach (var seg in dayunSegs)
                {
                    sb.Append("<div class=\"dayun-item")
                        .Append(seg.IsCurrent ? " current" : "")
                        .Append("\">");
                    sb.Append("<div class=\"dayun-gz\">")
                        .Append(seg.IsCurrent ? "★ " : "")
                        .Append(seg.StartYear).Append("　").Append(H(seg.Ganzhi));
                    if (!string.IsNullOrWhiteSpace(seg.DaYun))
                        sb.Append("　大運 ").Append(H(seg.DaYun));
                    sb.Append(seg.IsCurrent ? "　目前" : "")
                        .AppendLine("</div>");
                    sb.Append("<div class=\"muted\">虛歲 ").Append(seg.StartAge).AppendLine("</div>");
                    if (!string.IsNullOrWhiteSpace(seg.Summary))
                        sb.Append("<div class=\"dayun-sum\">").Append(H(seg.Summary)).AppendLine("</div>");
                    sb.AppendLine("</div>");
                }
                sb.AppendLine("</div>");
                sb.AppendLine("</section>");
            }

            // Yearly trends：僅多年有參考價值；單年整頁隱藏
            if (!singleYear && trends.Count > 0)
            {
            sb.AppendLine("<section class=\"page\">");
            sb.AppendLine(isNaming ? "<h1>成長流年趨勢</h1>" : "<h1>未來年度趨勢</h1>");
            sb.AppendLine("<p class=\"lead\">綜合評等走勢</p>");
            sb.AppendLine(BuildLineChart(
                trends.Select(t => (double)t.Year).ToList(),
                new List<ChartSeries>
                {
                    new ChartSeries("綜合", "#1f4e5f", trends.Select(t => t.TotalScore).ToList()),
                },
                "年", "評等", gradeYAxis: true));
            sb.AppendLine("<h2>三大面向趨勢</h2>");
            sb.AppendLine("<p class=\"lead\">事業／財運／感情評等走勢</p>");
            sb.AppendLine(BuildLineChart(
                trends.Select(t => (double)t.Year).ToList(),
                new List<ChartSeries>
                {
                    new ChartSeries("事業", "#2a6f7a", trends.Select(t => t.CareerScore).ToList()),
                    new ChartSeries("財運", "#8b6914", trends.Select(t => t.WealthScore).ToList()),
                    new ChartSeries("感情", "#6b4c7a", trends.Select(t => t.RelationshipScore).ToList()),
                },
                "年", "評等", gradeYAxis: true));
            sb.AppendLine("<table class=\"compact\"><thead><tr><th>年</th><th>干支</th><th>綜合</th><th>事業</th><th>財運</th><th>感情</th><th>關鍵字</th></tr></thead><tbody>");
            foreach (var t in trends)
            {
                sb.Append("<tr><td>").Append(t.Year).Append("</td><td>")
                    .Append(H(t.Ganzhi)).Append("</td><td>")
                    .Append(H(NameScorer.GradeLabel(t.TotalScore))).Append("</td><td>")
                    .Append(H(NameScorer.GradeLabel(t.CareerScore))).Append("</td><td>")
                    .Append(H(NameScorer.GradeLabel(t.WealthScore))).Append("</td><td>")
                    .Append(H(NameScorer.GradeLabel(t.RelationshipScore))).Append("</td><td>")
                    .Append(H(t.Keyword)).AppendLine("</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</section>");
            }

            // Narrative（流年策略頁）
            if (!isNaming && data.Narrative != null && HasNarrative(data.Narrative))
            {
                sb.AppendLine("<section class=\"page\">");
                sb.AppendLine("<h1>流年總覽與策略</h1>");
                if (!string.IsNullOrWhiteSpace(data.Narrative.Overview))
                {
                    sb.AppendLine("<h2>總覽</h2>");
                    sb.Append("<div class=\"prose\">").Append(Nl(data.Narrative.Overview)).AppendLine("</div>");
                }
                if (!string.IsNullOrWhiteSpace(data.Narrative.Phases))
                {
                    sb.AppendLine("<h2>宏觀階段</h2>");
                    sb.Append("<div class=\"prose\">").Append(Nl(data.Narrative.Phases)).AppendLine("</div>");
                }
                if (!string.IsNullOrWhiteSpace(data.Narrative.Trend))
                {
                    sb.AppendLine("<h2>趨勢與風險預警</h2>");
                    sb.Append("<div class=\"prose\">").Append(Nl(data.Narrative.Trend)).AppendLine("</div>");
                }
                if (!string.IsNullOrWhiteSpace(data.Narrative.Actions)
                    || (data.Narrative.ActionBuckets != null
                        && data.Narrative.ActionBuckets.Any(b => b.Items != null && b.Items.Count > 0)))
                {
                    sb.AppendLine("<h2>行動建議</h2>");
                    sb.AppendLine(BuildActionCards(data.Narrative));
                }
                sb.AppendLine("</section>");
            }

            // Year detail — 每年兩頁：流年總覽 + 流月明細
            foreach (var y in data.YearlyAnalysis ?? new List<YearAnalysisBlock>())
            {
                var series = (data.MonthlyTrend ?? new List<MonthTrendSeries>()).FirstOrDefault(s => s.Year == y.Year);
                var months = (data.MonthlyAnalysis ?? new List<MonthAnalysisBlock>())
                    .Where(m => m.Year == y.Year).OrderBy(m => m.Month).ToList();

                // Page 1: 流年（單年時標題旁標註所屬大運）
                sb.AppendLine("<section class=\"page year-page\">");
                sb.Append("<div class=\"year-head\"><h1>")
                    .Append(y.Year).Append("　流年　").Append(H(y.Ganzhi));
                if (singleYear && !string.IsNullOrWhiteSpace(y.DaYun))
                    sb.Append("　｜　大運 ").Append(H(y.DaYun));
                sb.Append("</h1><div class=\"year-meta\">")
                    .Append(y.Age).Append("歲 · ").Append(H(y.Animal));
                if (!singleYear)
                    sb.Append(" · 大運 ").Append(H(string.IsNullOrEmpty(y.DaYun) ? "—" : y.DaYun));
                if (!string.IsNullOrWhiteSpace(y.Keyword))
                    sb.Append(" · 「").Append(H(y.Keyword)).Append("」");
                sb.AppendLine("</div></div>");

                sb.AppendLine("<div class=\"score-row\">");
                GradeChip(sb, "綜合", y.TotalScore);
                GradeChip(sb, "事業", y.CareerScore);
                GradeChip(sb, "財運", y.WealthScore);
                GradeChip(sb, "感情", y.RelationshipScore);
                sb.AppendLine("</div>");

                var showNamingSummary = isNaming && !string.IsNullOrWhiteSpace(y.Summary);
                if (showNamingSummary)
                {
                    sb.AppendLine("<div class=\"naming-year-lead\">");
                    sb.Append("<div class=\"t\">年度解說</div><div class=\"c\">")
                        .Append(Nl(y.Summary)).AppendLine("</div></div>");
                }
                if (!showNamingSummary)
                    Block(sb, "整體", y.Overall);
                else if (!string.IsNullOrWhiteSpace(y.Overall)
                    && !string.Equals(y.Overall.Trim(), y.Summary.Trim(), StringComparison.Ordinal))
                    Block(sb, "整體", y.Overall);
                Block(sb, "事業", y.Career);
                Block(sb, "財運", y.Wealth);
                Block(sb, "感情／人際", y.Relationship);
                Block(sb, "生活", y.Life);

                if ((y.Suitable != null && y.Suitable.Count > 0) || (y.Avoid != null && y.Avoid.Count > 0))
                {
                    sb.AppendLine("<div class=\"yi-ji\">");
                    if (y.Suitable != null && y.Suitable.Count > 0)
                        sb.Append("<div class=\"yi-box\"><b>宜</b> ").Append(H(string.Join("、", y.Suitable))).Append("</div>");
                    if (y.Avoid != null && y.Avoid.Count > 0)
                        sb.Append("<div class=\"ji-box\"><b>忌</b> ").Append(H(string.Join("、", y.Avoid))).Append("</div>");
                    sb.AppendLine("</div>");
                }
                if (y.Advice != null && y.Advice.Count > 0)
                    Block(sb, "建議", string.Join("；", y.Advice));
                if (!string.IsNullOrWhiteSpace(y.Outlook))
                    Block(sb, "展望", y.Outlook);

                if (mode == "liunian" && HasMonthHints(y))
                {
                    sb.AppendLine("<h2>月份節奏</h2>");
                    sb.AppendLine("<div class=\"rhythm-list\">");
                    RhythmRow(sb, "推進", "tag-go", "較適合積極推進", y.StrongMonths);
                    RhythmRow(sb, "累積", "tag-steady", "適合穩定累積", y.StableMonths);
                    RhythmRow(sb, "調整", "tag-adjust", "建議保守調整", y.AdjustMonths);
                    RhythmRow(sb, "留意", "tag-caution", "較需留意", y.CautionMonths);
                    sb.AppendLine("</div>");
                    if (!string.IsNullOrWhiteSpace(y.MonthGuideCareer)) Block(sb, "事業月份指南", y.MonthGuideCareer);
                    if (!string.IsNullOrWhiteSpace(y.MonthGuideWealth)) Block(sb, "財運月份指南", y.MonthGuideWealth);
                    if (!string.IsNullOrWhiteSpace(y.MonthGuideRelationship)) Block(sb, "感情月份指南", y.MonthGuideRelationship);
                }
                sb.AppendLine("</section>");

                // Page 2: 流月（僅流年分析模式；新生兒／改名不顯示）
                var showMonthPage = mode == "liunian"
                    && ((series != null && series.Points != null && series.Points.Count > 0) || months.Count > 0);
                if (showMonthPage)
                {
                sb.AppendLine("<section class=\"page year-page month-page\">");
                sb.Append("<div class=\"year-head\"><h1>")
                    .Append(y.Year).Append("　流月")
                    .Append("</h1><div class=\"year-meta\">")
                    .Append(H(y.Ganzhi)).Append(" · ").Append(y.Age).Append("歲")
                    .AppendLine("</div></div>");

                if (series != null && series.Points != null && series.Points.Count > 0)
                {
                    sb.AppendLine("<p class=\"lead\">流月綜合評等走勢</p>");
                    sb.AppendLine(BuildLineChart(
                        series.Points.Select(m => (double)m.Month).ToList(),
                        new List<ChartSeries>
                        {
                            new ChartSeries("綜合", "#1f4e5f", series.Points.Select(m => m.TotalScore).ToList()),
                        },
                        "月", "評等", 600, 150, gradeYAxis: true));
                }

                if (months.Count > 0)
                {
                    sb.AppendLine("<div class=\"month-cards\">");
                    foreach (var m in months)
                    {
                        sb.AppendLine("<div class=\"month-card-detail\">");
                        sb.Append("<div class=\"month-card-head\">")
                            .Append("<span class=\"mh-title\">").Append(m.Month).Append("月</span>");
                        if (!string.IsNullOrWhiteSpace(m.MonthGanzhi))
                            sb.Append("<span class=\"mh-gz\">").Append(H(m.MonthGanzhi)).Append("</span>");
                        sb.Append("<span class=\"mh-score\">").Append(H(NameScorer.GradeLabel(m.TotalScore))).Append("</span>");
                        if (!string.IsNullOrWhiteSpace(m.Level)
                            && !string.Equals(m.Level, NameScorer.GradeLabel(m.TotalScore), StringComparison.Ordinal))
                            sb.Append("<span class=\"mh-level\">").Append(H(m.Level)).Append("</span>");
                        sb.AppendLine("</div>");
                        if (!string.IsNullOrWhiteSpace(m.SolarTermRange))
                            sb.Append("<div class=\"month-card-term\">節氣區間：").Append(H(m.SolarTermRange)).AppendLine("</div>");

                        MonthCardBlock(sb, "整體", StripMonthMetaPrefix(m.Overall, m));
                        MonthCardBlock(sb, "事業", m.Career);
                        MonthCardBlock(sb, "財運", m.Wealth);
                        MonthCardBlock(sb, "感情／人際", m.Relationship);
                        MonthCardBlock(sb, "生活", m.Life);
                        if (m.Suitable != null && m.Suitable.Count > 0)
                            MonthCardBlock(sb, "宜", string.Join("、", m.Suitable));
                        if (m.Avoid != null && m.Avoid.Count > 0)
                            MonthCardBlock(sb, "忌", string.Join("、", m.Avoid));
                        MonthCardBlock(sb, "建議", StripMonthMetaPrefix(m.Advice, m));
                        sb.AppendLine("</div>");
                    }
                    sb.AppendLine("</div>");
                }
                sb.AppendLine("</section>");
                }
            }

            if (data.Narrative != null && !string.IsNullOrWhiteSpace(data.Narrative.Poem))
            {
                sb.AppendLine("<section class=\"page\">");
                sb.AppendLine("<h1>專屬寄語</h1>");
                sb.Append("<div class=\"prose\">").Append(Nl(ChatGptCiPoemGenerator.SanitizePoemDisplay(data.Narrative.Poem))).AppendLine("</div>");
                sb.AppendLine("</section>");
            }

            sb.AppendLine("<section class=\"page footer-page\">");
            sb.AppendLine("<p class=\"disclaimer\">內容僅供文化參考，命理分析不構成人生保證。</p>");
            sb.AppendLine("<p class=\"tagline\">替每一個人生，寫下值得珍藏的第一行文字。</p>");
            sb.AppendLine("<p class=\"brand-foot\">名序 · Mingxu</p>");
            sb.AppendLine("</section>");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private sealed class ChartSeries
        {
            public string Name;
            public string Color;
            public List<double> Values;
            public ChartSeries(string name, string color, List<double> values)
            {
                Name = name;
                Color = color;
                Values = values ?? new List<double>();
            }
        }

        private static string BuildLineChart(
            List<double> xs,
            List<ChartSeries> series,
            string xLabel,
            string yLabel,
            double width = 640,
            double height = 240,
            bool gradeYAxis = false)
        {
            if (xs == null || xs.Count == 0 || series == null || series.Count == 0)
                return "<p class=\"muted\">（無圖表資料）</p>";

            var padL = gradeYAxis ? 58.0 : 44.0;
            const double padR = 16, padT = 20, padB = 36;
            var plotW = width - padL - padR;
            var plotH = height - padT - padB;
            double minY = 0, maxY = 100;
            if (!gradeYAxis)
            {
                foreach (var s in series)
                {
                    foreach (var v in s.Values)
                    {
                        if (v < minY) minY = v;
                        if (v > maxY) maxY = v;
                    }
                }
                if (Math.Abs(maxY - minY) < 1) { minY = 0; maxY = 100; }
                maxY = Math.Ceiling(maxY / 10.0) * 10;
                minY = Math.Floor(minY / 10.0) * 10;
            }

            double minX = xs.Min(), maxX = xs.Max();
            if (Math.Abs(maxX - minX) < 0.0001) maxX = minX + 1;

            Func<double, double> mapX = x => padL + (x - minX) / (maxX - minX) * plotW;
            Func<double, double> mapY = y => padT + (1.0 - (y - minY) / (maxY - minY)) * plotH;

            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<svg class=\"chart\" viewBox=\"0 0 {0} {1}\" width=\"100%\" xmlns=\"http://www.w3.org/2000/svg\">",
                width, height);
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"#f7f5f1\" stroke=\"#e2ddd4\"/>",
                padL, padT, plotW, plotH);

            if (gradeYAxis)
            {
                // 五個文字等級刻度（與 NameScorer.GradeLabel 一致）
                var ticks = new[] { 50.0, 60.0, 70.0, 80.0, 90.0 };
                foreach (var v in ticks)
                {
                    var yy = mapY(v);
                    sb.AppendFormat(CultureInfo.InvariantCulture,
                        "<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{1}\" stroke=\"#e8e2d8\" stroke-width=\"1\"/>",
                        padL, yy, padL + plotW);
                    sb.AppendFormat(CultureInfo.InvariantCulture,
                        "<text x=\"{0}\" y=\"{1}\" class=\"axis axis-grade\">{2}</text>",
                        padL - 6, yy + 4, H(NameScorer.GradeLabel(v)));
                }
            }
            else
            {
                for (var i = 0; i <= 4; i++)
                {
                    var v = minY + (maxY - minY) * i / 4.0;
                    var yy = mapY(v);
                    sb.AppendFormat(CultureInfo.InvariantCulture,
                        "<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{1}\" stroke=\"#e8e2d8\" stroke-width=\"1\"/>",
                        padL, yy, padL + plotW);
                    sb.AppendFormat(CultureInfo.InvariantCulture,
                        "<text x=\"{0}\" y=\"{1}\" class=\"axis\">{2:0}</text>",
                        padL - 8, yy + 4, v);
                }
            }

            var step = Math.Max(1, (int)Math.Ceiling(xs.Count / 8.0));
            for (var i = 0; i < xs.Count; i += step)
            {
                var xx = mapX(xs[i]);
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<text x=\"{0}\" y=\"{1}\" class=\"axis-x\">{2:0}</text>",
                    xx, height - 12, xs[i]);
            }

            foreach (var s in series)
            {
                if (s.Values.Count == 0) continue;
                var pts = new StringBuilder();
                var n = Math.Min(xs.Count, s.Values.Count);
                for (var i = 0; i < n; i++)
                {
                    if (i > 0) pts.Append(" ");
                    pts.AppendFormat(CultureInfo.InvariantCulture, "{0:0.##},{1:0.##}", mapX(xs[i]), mapY(s.Values[i]));
                }
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<polyline fill=\"none\" stroke=\"{0}\" stroke-width=\"2.2\" points=\"{1}\"/>",
                    s.Color, pts);
                for (var i = 0; i < n; i++)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture,
                        "<circle cx=\"{0:0.##}\" cy=\"{1:0.##}\" r=\"2.6\" fill=\"{2}\"/>",
                        mapX(xs[i]), mapY(s.Values[i]), s.Color);
                }
            }

            var legendX = padL;
            var legendY = 14.0;
            foreach (var s in series)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<rect x=\"{0}\" y=\"{1}\" width=\"10\" height=\"10\" fill=\"{2}\"/>",
                    legendX, legendY - 9, s.Color);
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<text x=\"{0}\" y=\"{1}\" class=\"legend\">{2}</text>",
                    legendX + 14, legendY, H(s.Name));
                legendX += 70;
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static List<DayunSegment> FilterDayunForReport(
            List<DayunSegment> segments, int reportStart, int reportEnd)
        {
            var list = segments ?? new List<DayunSegment>();
            if (reportStart <= 0 || reportEnd <= 0 || reportEnd < reportStart)
                return list;
            return list
                .Where(s => DayunSegmentCoveredByReport(s, reportStart, reportEnd))
                .OrderBy(s => s.StartYear)
                .ToList();
        }

        private static bool DayunSegmentCoveredByReport(DayunSegment seg, int reportStart, int reportEnd)
        {
            if (seg == null) return false;
            if (reportStart <= 0 || reportEnd <= 0 || reportEnd < reportStart) return true;
            return seg.EndYear >= reportStart && seg.StartYear <= reportEnd;
        }

        private static string BuildDayunSvg(List<DayunSegment> segments, List<DayunPeriodNote> periods)
        {
            if (segments == null || segments.Count == 0)
                return "<p class=\"muted\">（無大運時間軸資料）</p>";

            var ordered = segments.OrderBy(s => s.StartYear).ToList();
            const double width = 680, height = 148;
            const double padL = 16, padR = 16, padT = 8;
            var plotW = width - padL - padR;
            var n = ordered.Count;
            var slotW = n > 0 ? plotW / n : plotW;
            var yearIndex = new Dictionary<int, int>();
            for (var i = 0; i < n; i++)
                yearIndex[ordered[i].StartYear] = i;

            // 大運色盤（與流年格不同色系）
            var dayunColors = new[]
            {
                "#3d7a8c", "#8b6914", "#6b4c7a", "#2e7d4f",
                "#b45309", "#5c6b8a", "#9a5b4a", "#1f4e5f"
            };
            var colorByDaYun = new Dictionary<string, string>(StringComparer.Ordinal);
            var visStart = ordered[0].StartYear;
            var visEnd = ordered[ordered.Count - 1].EndYear;
            var periodList = (periods ?? new List<DayunPeriodNote>())
                .Where(p => p != null && p.EndYear >= visStart && p.StartYear <= visEnd)
                .OrderBy(p => p.StartYear)
                .Select(p => new DayunPeriodNote
                {
                    Ganzhi = p.Ganzhi,
                    StartYear = Math.Max(p.StartYear, visStart),
                    EndYear = Math.Min(p.EndYear, visEnd),
                    StartAge = p.StartAge,
                    EndAge = p.EndAge,
                    IsCurrent = p.IsCurrent,
                })
                .ToList();
            // 若無 periods，依各年 DaYun 推一段
            if (periodList.Count == 0)
            {
                DayunPeriodNote open = null;
                foreach (var seg in ordered)
                {
                    var gz = seg.DaYun ?? "";
                    if (open == null || !string.Equals(open.Ganzhi, gz, StringComparison.Ordinal))
                    {
                        if (open != null) periodList.Add(open);
                        open = new DayunPeriodNote
                        {
                            Ganzhi = gz,
                            StartYear = seg.StartYear,
                            EndYear = seg.EndYear,
                            IsCurrent = seg.IsCurrent,
                        };
                    }
                    else
                    {
                        open.EndYear = seg.EndYear;
                        if (seg.IsCurrent) open.IsCurrent = true;
                    }
                }
                if (open != null) periodList.Add(open);
            }
            for (var pi = 0; pi < periodList.Count; pi++)
            {
                var key = periodList[pi].Ganzhi ?? "";
                if (!colorByDaYun.ContainsKey(key))
                    colorByDaYun[key] = dayunColors[pi % dayunColors.Length];
            }

            var bandY = padT + 4;
            var bandH = 22.0;
            var yearBarY = bandY + bandH + 18;
            var yearBarH = 16.0;

            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<svg class=\"chart dayun-svg\" viewBox=\"0 0 {0} {1}\" width=\"100%\" xmlns=\"http://www.w3.org/2000/svg\">",
                width, height);

            // 大運色帶（與時間軸同一圖）
            foreach (var p in periodList)
            {
                int i0, i1;
                if (!yearIndex.TryGetValue(p.StartYear, out i0))
                {
                    i0 = ordered.FindIndex(s => s.StartYear >= p.StartYear);
                    if (i0 < 0) continue;
                }
                if (!yearIndex.TryGetValue(p.EndYear, out i1))
                {
                    i1 = ordered.FindLastIndex(s => s.StartYear <= p.EndYear);
                    if (i1 < 0) continue;
                }
                if (i1 < i0) { var t = i0; i0 = i1; i1 = t; }
                var x = padL + i0 * slotW;
                var w = Math.Max(4, (i1 - i0 + 1) * slotW - 1);
                var fill = colorByDaYun.ContainsKey(p.Ganzhi ?? "")
                    ? colorByDaYun[p.Ganzhi ?? ""]
                    : dayunColors[0];
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<rect x=\"{0:0.##}\" y=\"{1:0.##}\" width=\"{2:0.##}\" height=\"{3:0.##}\" rx=\"3\" fill=\"{4}\" opacity=\"0.88\"/>",
                    x, bandY, w, bandH, fill);
                var mid = x + w / 2.0;
                var label = "大運 " + (string.IsNullOrWhiteSpace(p.Ganzhi) ? "—" : p.Ganzhi);
                if (p.IsCurrent) label += " ★";
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<text x=\"{0:0.##}\" y=\"{1:0.##}\" text-anchor=\"middle\" fill=\"#fff\" font-size=\"11\" font-weight=\"600\" font-family=\"Microsoft JhengHei, sans-serif\">{2}</text>",
                    mid, bandY + bandH / 2.0 + 4, H(label));
            }

            // 基準線
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<line x1=\"{0}\" y1=\"{1:0.##}\" x2=\"{2}\" y2=\"{1:0.##}\" stroke=\"#d9d2c5\" stroke-width=\"1\"/>",
                padL, yearBarY - 6, padL + plotW);

            // 流年格（一年一格，顏色跟所屬大運淡化）
            for (var i = 0; i < n; i++)
            {
                var seg = ordered[i];
                var x1 = padL + i * slotW + 1;
                var barW = Math.Max(3, slotW - 2);
                var mid = x1 + barW / 2.0;
                string dayunFill;
                if (!colorByDaYun.TryGetValue(seg.DaYun ?? "", out dayunFill))
                    dayunFill = "#c4b8a5";
                var fill = seg.IsCurrent ? "#1f4e5f" : dayunFill;
                var opacity = seg.IsCurrent ? "0.95" : "0.55";
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<rect x=\"{0:0.##}\" y=\"{1:0.##}\" width=\"{2:0.##}\" height=\"{3:0.##}\" rx=\"2\" fill=\"{4}\" opacity=\"{5}\"/>",
                    x1, yearBarY, barW, yearBarH, fill, opacity);
                var fontSize = n > 12 ? 8 : (n > 8 ? 9 : 10);
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<text x=\"{0:0.##}\" y=\"{1:0.##}\" class=\"dayun-label\" font-size=\"{2}px\">{3}</text>",
                    mid, yearBarY - 4, fontSize, H(seg.Ganzhi ?? ""));
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<text x=\"{0:0.##}\" y=\"{1}\" class=\"axis-x\">{2}</text>",
                    mid, height - 8, seg.StartYear);
            }
            sb.Append("</svg>");

            // 色例（貼在圖下方，非獨立字卡）
            if (periodList.Count > 0)
            {
                sb.Append("<div class=\"dayun-legend\">");
                foreach (var p in periodList)
                {
                    var fill = colorByDaYun.ContainsKey(p.Ganzhi ?? "")
                        ? colorByDaYun[p.Ganzhi ?? ""]
                        : "#c4b8a5";
                    sb.Append("<span class=\"dayun-legend-item\"><i style=\"background:")
                        .Append(fill).Append("\"></i>")
                        .Append(H(string.IsNullOrWhiteSpace(p.Ganzhi) ? "—" : p.Ganzhi))
                        .Append("　").Append(p.StartYear).Append("～").Append(p.EndYear);
                    if (p.IsCurrent) sb.Append("（目前）");
                    sb.Append("</span>");
                }
                sb.Append("</div>");
            }
            return sb.ToString();
        }

        private static string BuildWatermarkHtml(string logoPath)
        {
            if (string.IsNullOrEmpty(logoPath) || !File.Exists(logoPath))
                return "";
            var sb = new StringBuilder();
            sb.Append("<div class=\"page-watermark\" aria-hidden=\"true\">");
            sb.Append("<div class=\"wm-inner\">");
            sb.Append("<img class=\"wm-logo\" src=\"")
                .Append(ToFileUri(logoPath))
                .Append("\" alt=\"\" />");
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private static string Css()
        {
            return @"
@page { size: A4 portrait; margin: 14mm 14mm 16mm 14mm; }
* { box-sizing: border-box; }
body {
  margin: 0;
  color: #1c1a17;
  background: #fff;
  font-family: ""Microsoft JhengHei"", ""PingFang TC"", ""Noto Sans TC"", sans-serif;
  font-size: 10.5pt;
  line-height: 1.55;
  position: relative;
}
.page-watermark {
  position: fixed;
  left: 0; top: 0; right: 0; bottom: 0;
  z-index: 0;
  pointer-events: none;
  display: flex;
  align-items: center;
  justify-content: center;
  -webkit-print-color-adjust: exact;
  print-color-adjust: exact;
}
.wm-inner {
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0.1;
  transform: rotate(-28deg);
}
.wm-logo {
  width: 48mm;
  height: 48mm;
  object-fit: contain;
  background: transparent;
}
.page {
  page-break-after: always;
  padding: 4mm 0 8mm;
  position: relative;
  z-index: 1;
}
.page:last-child { page-break-after: auto; }
.dayun-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 2mm 5mm;
  margin: 1.5mm 0 3mm;
  font-size: 8.5pt;
  color: #5a534a;
}
.dayun-legend-item {
  display: inline-flex;
  align-items: center;
  gap: 1.5mm;
  white-space: nowrap;
}
.dayun-legend-item i {
  display: inline-block;
  width: 3.2mm;
  height: 3.2mm;
  border-radius: 1px;
}
.cover {
  min-height: 240mm;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 20mm 8mm;
  background: linear-gradient(165deg, #f4f1ea 0%, #ffffff 45%, #eef3f4 100%);
}
.logo img { width: 128px; height: 128px; background: transparent; }
.brand {
  margin-top: 10mm;
  font-size: 42pt;
  font-weight: 600;
  letter-spacing: 0.35em;
  color: #1f4e5f;
}
.slogan-main {
  margin-top: 5mm;
  font-size: 14pt;
  color: #2f3e46;
  letter-spacing: 0.18em;
  font-weight: 600;
}
.slogan-sub {
  margin-top: 3mm;
  max-width: 140mm;
  font-size: 10pt;
  color: #6a635a;
  line-height: 1.6;
  letter-spacing: 0.04em;
}
.cover-title { margin-top: 10mm; font-size: 18pt; color: #3a3530; letter-spacing: 0.2em; }
.cover-range { margin-top: 6mm; font-size: 22pt; color: #1f4e5f; font-weight: 600; }
.cover-meta { margin-top: 16mm; font-size: 11pt; color: #4a453f; }
.cover-meta div { margin: 2mm 0; }
.cover-foot { margin-top: 20mm; color: #8a8378; font-size: 9pt; }
h1 {
  font-size: 16pt;
  font-weight: 600;
  color: #1f4e5f;
  border-bottom: 1.5px solid #d9d2c5;
  padding-bottom: 3mm;
  margin: 0 0 5mm;
  letter-spacing: 0.08em;
}
h2 { font-size: 12pt; color: #2f3e46; margin: 6mm 0 3mm; }
h3 { font-size: 10.5pt; color: #3d4f56; margin: 0 0 1mm; }
.lead { color: #5a534a; margin: 0 0 3mm; }
.note { color: #7a7368; font-size: 9pt; }
.sub { color: #6a635a; margin-top: -2mm; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 2.5mm 6mm; }
.mt { margin-top: 4mm; }
.meta-label { font-size: 8.5pt; color: #8a8378; }
.meta-value { font-size: 10.5pt; color: #1c1a17; margin-bottom: 2mm; }
.pillars { display: grid; grid-template-columns: repeat(4, 1fr); gap: 3mm; margin: 3mm 0 5mm; }
.pillar {
  border: 1px solid #e2ddd4;
  border-radius: 4px;
  padding: 3mm;
  text-align: center;
  background: #faf8f4;
}
.pillar .k { font-size: 8pt; color: #8a8378; }
.pillar .v { font-size: 14pt; color: #1f4e5f; font-weight: 600; margin-top: 1mm; }
.chart { margin: 2mm 0 4mm; }
.axis { font-size: 9px; fill: #8a8378; text-anchor: end; }
.axis-grade { font-size: 10px; fill: #5a534a; }
.axis-x { font-size: 9px; fill: #8a8378; text-anchor: middle; }
.legend { font-size: 10px; fill: #3a3530; }
.dayun-label { font-size: 10px; fill: #1f4e5f; text-anchor: middle; font-weight: 600; }
.dayun-list { margin-top: 4mm; }
.dayun-page { overflow: visible; }
.dayun-item {
  border-left: 3px solid #d9d2c5;
  padding: 2mm 0 2mm 4mm;
  margin-bottom: 3mm;
  page-break-inside: avoid;
}
.dayun-item.current { border-left-color: #1f4e5f; background: #f3f7f8; }
.dayun-gz { font-weight: 600; color: #1f4e5f; }
.dayun-sum { margin-top: 1mm; color: #4a453f; }
.muted { color: #8a8378; font-size: 9pt; }
table.compact {
  width: 100%;
  border-collapse: collapse;
  font-size: 9pt;
  margin-top: 3mm;
}
table.compact th, table.compact td {
  border-bottom: 1px solid #e8e2d8;
  padding: 1.6mm 2mm;
  text-align: left;
}
table.compact th { color: #6a635a; font-weight: 600; background: #f7f5f1; }
.score-row { display: flex; gap: 3mm; margin: 3mm 0 4mm; }
.chip {
  flex: 1;
  background: #f7f5f1;
  border: 1px solid #e2ddd4;
  border-radius: 4px;
  padding: 2.5mm;
  text-align: center;
}
.chip .k { font-size: 8pt; color: #8a8378; }
.chip .v { font-size: 14pt; color: #1f4e5f; font-weight: 600; }
.chip .l { font-size: 8pt; color: #6a635a; }
.keyword {
  font-size: 13pt;
  color: #1f4e5f;
  margin-bottom: 3mm;
  letter-spacing: 0.12em;
}
.block { margin: 2.5mm 0; }
.block .t { font-size: 9pt; color: #8a8378; margin-bottom: 0.5mm; }
.block .c { color: #2a2622; white-space: pre-wrap; font-size: 10pt; line-height: 1.45; }
.year-page {
  page-break-after: always;
  padding: 0 0 2mm;
}
.year-page .year-head h1 {
  font-size: 14pt;
  margin: 0;
  padding-bottom: 2mm;
  border-bottom: 1.5px solid #d9d2c5;
}
.year-meta { color: #6a635a; font-size: 9.5pt; margin: 2mm 0 0; }
.yi-ji {
  display: flex;
  gap: 3mm;
  font-size: 9pt;
  color: #4a453f;
  margin: 3mm 0;
  -webkit-print-color-adjust: exact;
  print-color-adjust: exact;
}
.yi-box, .ji-box {
  flex: 1;
  padding: 2.5mm 3mm;
  border-radius: 3px;
  line-height: 1.5;
}
.yi-box {
  background: #e8f5ec;
  border: 1px solid #c5e0ce;
  color: #2a5a3a;
}
.yi-box b { color: #1e6b3a; }
.ji-box {
  background: #fceeed;
  border: 1px solid #e8c9c6;
  color: #6b3530;
}
.ji-box b { color: #a04038; }
.action-cards {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 3mm;
  margin: 2mm 0 4mm;
  -webkit-print-color-adjust: exact;
  print-color-adjust: exact;
}
.action-card {
  border: 1px solid #e2ddd4;
  border-radius: 4px;
  padding: 2.5mm 3mm;
  page-break-inside: avoid;
  background: #fff;
  min-height: 28mm;
}
.action-card.do {
  border: 1px solid #c5e0ce;
  border-top: 3px solid #3d7a5a;
  background: #e8f5ec;
}
.action-card.dont {
  border: 1px solid #e8c9c6;
  border-top: 3px solid #a85a3a;
  background: #fceeed;
}
.action-card .ac-title {
  font-size: 10pt;
  font-weight: 600;
  margin-bottom: 1.5mm;
  letter-spacing: 0.08em;
}
.action-card.do .ac-title { color: #2f5e48; }
.action-card.dont .ac-title { color: #8a3f2a; }
.action-card .ac-empty {
  font-size: 9pt;
  color: #8a8378;
}
.action-card ul {
  margin: 0;
  padding-left: 4.5mm;
}
.action-card li {
  margin: 1mm 0;
  font-size: 9pt;
  line-height: 1.4;
  color: #2a2622;
}
.rhythm-list {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2.5mm 3mm;
  margin: 2mm 0 4mm;
}
.rhythm-row {
  display: flex;
  align-items: flex-start;
  gap: 2.5mm;
  padding: 2.5mm;
  background: #f7f5f1;
  border-radius: 3px;
  border-left: 3px solid #d9d2c5;
  min-height: 16mm;
  box-sizing: border-box;
}
.rhythm-row.go { border-left-color: #3d7a5a; }
.rhythm-row.steady { border-left-color: #2a6f7a; }
.rhythm-row.adjust { border-left-color: #8b6914; }
.rhythm-row.caution { border-left-color: #a85a3a; }
.rhythm-tag {
  flex: 0 0 auto;
  font-size: 9pt;
  font-weight: 600;
  color: #fff;
  padding: 0.8mm 2.5mm;
  border-radius: 3px;
  letter-spacing: 0.08em;
  white-space: nowrap;
}
.tag-go { background: #3d7a5a; }
.tag-steady { background: #2a6f7a; }
.tag-adjust { background: #8b6914; }
.tag-caution { background: #a85a3a; }
.rhythm-body { flex: 1; min-width: 0; }
.rhythm-desc {
  font-size: 8pt;
  color: #8a8378;
  margin-bottom: 0.6mm;
}
.rhythm-months {
  font-size: 10pt;
  color: #2a2622;
  line-height: 1.4;
}
.rhythm-empty {
  font-size: 9pt;
  color: #a39c92;
}
.month-cards {
  margin-top: 3mm;
}
.month-card-detail {
  border: 1px solid #e2ddd4;
  border-radius: 4px;
  padding: 3mm 3.5mm;
  margin-bottom: 3.5mm;
  background: #faf8f4;
  page-break-inside: avoid;
}
.month-card-head {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 2.5mm;
  margin-bottom: 2mm;
  padding-bottom: 1.5mm;
  border-bottom: 1px solid #e2ddd4;
}
.mh-title { font-size: 12pt; font-weight: 600; color: #1f4e5f; }
.mh-gz { font-size: 10.5pt; color: #2f3e46; }
.mh-score { font-size: 10pt; color: #1f4e5f; font-weight: 600; }
.mh-level {
  font-size: 9pt;
  color: #fff;
  background: #1f4e5f;
  padding: 0.4mm 2mm;
  border-radius: 3px;
}
.month-card-term { font-size: 8.5pt; color: #8a8378; margin-bottom: 2mm; }
.mc-block { margin: 1.8mm 0; }
.mc-block .t { font-size: 8.5pt; color: #8a8378; margin-bottom: 0.4mm; }
.mc-block .c { font-size: 9.5pt; color: #2a2622; line-height: 1.45; white-space: pre-wrap; }
.mc-block.yi-tone, .mc-block.ji-tone {
  padding: 1.8mm 2.2mm;
  border-radius: 3px;
  -webkit-print-color-adjust: exact;
  print-color-adjust: exact;
}
.mc-block.yi-tone {
  background: #e8f5ec;
  border: 1px solid #c5e0ce;
}
.mc-block.yi-tone .t { color: #1e6b3a; font-weight: 700; }
.mc-block.yi-tone .c { color: #2a5a3a; }
.mc-block.ji-tone {
  background: #fceeed;
  border: 1px solid #e8c9c6;
}
.mc-block.ji-tone .t { color: #a04038; font-weight: 700; }
.mc-block.ji-tone .c { color: #6b3530; }
.wuge-schematic {
  margin-top: 1mm;
  padding: 1.5mm 3mm 3mm;
  border: 1px solid #e2ddd4;
  border-radius: 4px;
  background: #fffdf8;
  page-break-inside: avoid;
}
.wuge-schematic-title {
  text-align: center;
  font-size: 11pt;
  font-weight: 600;
  color: #8b1e1e;
  letter-spacing: 0.2em;
  margin: 0 0 1mm;
  line-height: 1.2;
}
.wuge-body {
  display: flex;
  align-items: stretch;
  gap: 3mm;
}
.wuge-svg {
  display: block;
  flex: 1 1 auto;
  min-width: 0;
  max-width: 118mm;
.min-height: 52mm;
  height: auto;
  margin: 0;
}
.char-card-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 3mm;
  margin-bottom: 5mm;
}
.char-card {
  border-radius: 5px;
  padding: 3mm 3.5mm;
  border: 1px solid rgba(0,0,0,0.08);
  page-break-inside: avoid;
  min-height: 22mm;
}
.char-card .cc-head {
  display: flex;
  align-items: baseline;
  gap: 2mm;
  margin-bottom: 1.5mm;
}
.char-card .cc-char {
  font-size: 22pt;
  font-weight: 700;
  line-height: 1;
  color: #fff;
  width: 11mm;
  height: 11mm;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
}
.char-card .cc-wx {
  font-size: 9pt;
  font-weight: 600;
  color: #5a534a;
}
.char-card .cc-body {
  font-size: 9.5pt;
  line-height: 1.5;
  color: #2a2622;
}
.char-card--meta .cc-label {
  font-size: 8.5pt;
  font-weight: 600;
  letter-spacing: 0.06em;
  color: #6a635a;
  margin-bottom: 1mm;
}
.char-card--combo { background: #f4f8fb; border-left: 3px solid #1f4e5f; }
.char-card--destiny { background: #faf6ef; border-left: 3px solid #8b6914; }
.char-card--image { background: #f7f4fa; border-left: 3px solid #6b4c7a; }
.naming-msg-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 3.5mm;
  margin-top: 2mm;
}
.naming-msg-grid--stack {
  grid-template-columns: 1fr;
}
.naming-msg-card {
  border-radius: 5px;
  padding: 3.5mm 4mm;
  page-break-inside: avoid;
  border: 1px solid #e2ddd4;
}
.naming-msg-card .nm-title {
  font-size: 10pt;
  font-weight: 700;
  letter-spacing: 0.12em;
  margin-bottom: 2mm;
}
.naming-msg-card .nm-body {
  font-size: 9.5pt;
  line-height: 1.55;
  color: #2a2622;
  white-space: pre-wrap;
}
.naming-msg-card--story { background: linear-gradient(135deg, #f8f3ea 0%, #fffdf8 100%); }
.naming-msg-card--story .nm-title { color: #8b6914; }
.naming-msg-card--idea { background: linear-gradient(135deg, #eef5f7 0%, #fafcfd 100%); }
.naming-msg-card--idea .nm-title { color: #1f4e5f; }
.naming-msg-card--blessing { background: linear-gradient(135deg, #f3eef7 0%, #fdfbff 100%); }
.naming-msg-card--blessing .nm-title { color: #6b4c7a; }
.naming-msg-card--hope { background: linear-gradient(135deg, #eef7f0 0%, #fafffb 100%); }
.naming-msg-card--hope .nm-title { color: #2e7d4f; }
.rename-dir-card {
  margin: 2mm 0 4mm;
  padding: 3.5mm 4mm;
  border-radius: 5px;
  border: 1px solid #e2ddd4;
  background: linear-gradient(135deg, #f7f1e6 0%, #fffdf8 100%);
  border-left: 3px solid #8b6914;
  page-break-inside: avoid;
}
.rename-dir-block {
  margin: 2mm 0 4mm;
  page-break-inside: avoid;
}
.rename-dir-heading {
  font-size: 10pt;
  font-weight: 700;
  letter-spacing: 0.12em;
  color: #8b6914;
  margin-bottom: 2mm;
}
.rename-reason-line {
  font-size: 9pt;
  color: #5a534a;
  margin-bottom: 2.5mm;
}
.rename-dir-row {
  display: flex;
  flex-wrap: wrap;
  gap: 2.5mm;
  margin-bottom: 3mm;
}
.rename-dir-chip {
  flex: 1 1 0;
  min-width: 28mm;
  text-align: center;
  padding: 3mm 2.5mm;
  border-radius: 5px;
  border: 1px solid #e2ddd4;
  background: linear-gradient(160deg, #f7f1e6 0%, #fffdf8 100%);
  border-top: 3px solid #8b6914;
  font-size: 10pt;
  font-weight: 600;
  color: #2a2622;
  letter-spacing: 0.06em;
}
.rename-dir-chip--muted {
  font-weight: 500;
  color: #8a8378;
  border-top-color: #c4b8a5;
}
.rename-focus-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 2mm 3mm;
  padding: 2.5mm 3mm;
  border-radius: 5px;
  background: #f3f7f8;
  border: 1px solid #d9e3e6;
}
.rename-focus-label {
  font-size: 9pt;
  font-weight: 700;
  color: #1f4e5f;
  letter-spacing: 0.08em;
  white-space: nowrap;
}
.rename-focus-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 2mm;
}
.rename-focus-chip {
  display: inline-block;
  padding: 1.2mm 3mm;
  border-radius: 3px;
  background: #fff;
  border: 1px solid #c5d4d8;
  font-size: 9pt;
  color: #2f3e46;
  font-weight: 600;
}
.rename-vs-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 3mm;
  margin-bottom: 4mm;
  align-items: stretch;
}
.rename-vs-card {
  border-radius: 5px;
  padding: 2.5mm 2mm 1.5mm;
  border: 1px solid #e2ddd4;
  page-break-inside: avoid;
}
.rename-vs-card--old {
  background: linear-gradient(160deg, #f6f4f0 0%, #fbfaf7 100%);
  border-top: 3px solid #8a8378;
}
.rename-vs-card--new {
  background: linear-gradient(160deg, #eef5f7 0%, #f7fbfc 100%);
  border-top: 3px solid #1f4e5f;
}
.rename-vs-card--new .rv-label { color: #1f4e5f; }
.rename-vs-card .rv-label {
  font-size: 8pt;
  font-weight: 700;
  letter-spacing: 0.14em;
  color: #6a635a;
  margin-bottom: 0.8mm;
}
.rename-vs-card .rv-name {
  font-size: 14pt;
  font-weight: 700;
  color: #1c1a17;
  letter-spacing: 0.08em;
  margin-bottom: 1.5mm;
  line-height: 1.15;
}
.rename-vs-card .rv-wuge-board {
  margin-top: 0;
}
.wuge-schematic--compact {
  margin: 0;
  padding: 0;
  background: #fff;
  border: none;
}
.wuge-body--compact {
  flex-direction: column;
  gap: 0.8mm;
  align-items: stretch;
}
.wuge-svg--compact {
  max-width: none;
  min-height: 0;
  width: 100% !important;
  height: auto;
  display: block;
}
.wuge-side--compact {
  flex: 0 0 auto !important;
  width: 100%;
  height: auto !important;
  min-height: 0 !important;
  padding: 1mm 1.5mm;
  gap: 0.3mm;
  box-sizing: border-box;
}
.wuge-side--compact .sancai-note {
  font-size: 7.5pt;
  line-height: 1.25;
  margin: 0;
  padding: 0;
}
.wuge-side--compact .wuge-luck-line {
  font-size: 7pt;
  line-height: 1.25;
  margin: 0;
  padding: 0;
}
.rename-note-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 3.5mm;
  margin: 3mm 0 4mm;
}
.rename-note-card {
  border-radius: 5px;
  padding: 3.5mm 4mm;
  border: 1px solid #e2ddd4;
  page-break-inside: avoid;
}
.rename-note-card .rn-title {
  font-size: 9.5pt;
  font-weight: 700;
  letter-spacing: 0.1em;
  margin-bottom: 2mm;
}
.rename-note-card .rn-body {
  font-size: 9pt;
  line-height: 1.5;
  color: #2a2622;
  white-space: pre-wrap;
}
.rename-note-card ul {
  margin: 0;
  padding-left: 4.5mm;
}
.rename-note-card li { margin: 0.8mm 0; }
.rename-note-card--eval {
  margin: 2mm 0 3.5mm;
  background: #faf8f4;
}
.rename-note-card--eval .rn-title { color: #6a635a; }
.rename-note-card--up { background: #f3f8f4; }
.rename-note-card--up .rn-title { color: #2e7d4f; }
.rename-note-card--down { background: #faf4f2; }
.rename-note-card--down .rn-title { color: #9b2c2c; }
.rename-note-card--reason { background: #eef5f7; }
.rename-note-card--reason .rn-title { color: #1f4e5f; }
.rename-note-card--conclusion { background: #f3eef7; }
.rename-note-card--conclusion .rn-title { color: #6b4c7a; }
.rename-note-card--span2 { grid-column: 1 / -1; }
.naming-year-lead {
  margin: 3mm 0 4mm;
  padding: 3mm 3.5mm;
  background: #f3f7f8;
  border-left: 3px solid #1f4e5f;
  border-radius: 0 4px 4px 0;
  page-break-inside: avoid;
}
.naming-year-lead .t { font-size: 9pt; font-weight: 600; color: #1f4e5f; margin-bottom: 1mm; }
.naming-year-lead .c { font-size: 9.5pt; line-height: 1.5; color: #2a2622; white-space: pre-wrap; }
.wuge-side {
  flex: 0 0 58mm;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 2.5mm;
  padding: 2mm 2.5mm;
  background: #f3f7f8;
  border: 1px solid #d9e3e6;
  border-radius: 4px;
}
.wuge-side .sancai-note {
  margin: 0;
  font-size: 9.5pt;
  color: #2f3e46;
  line-height: 1.45;
  font-weight: 600;
}
.wuge-side .wuge-luck-line {
  margin: 0;
  font-size: 8.5pt;
  color: #5a534a;
  line-height: 1.55;
}
.wuge-luck-line {
  margin-top: 2mm;
  font-size: 9pt;
  color: #5a534a;
}
.sancai-row {
  display: flex;
  align-items: center;
  gap: 2.5mm;
  padding: 3mm;
  background: #f3f7f8;
  border: 1px solid #d9e3e6;
  border-radius: 4px;
}
.sancai-node { text-align: center; min-width: 14mm; }
.sancai-node .k { font-size: 8pt; color: #8a8378; margin-bottom: 1mm; }
.sancai-node .wx {
  color: #fff;
  font-size: 11pt;
  font-weight: 600;
  padding: 2mm 3mm;
  border-radius: 4px;
}
.sancai-arrow { color: #8a8378; font-size: 14pt; }
.sancai-result {
  margin-left: auto;
  text-align: right;
  min-width: 28mm;
}
.sancai-result .k { font-size: 8pt; color: #8a8378; }
.sancai-result .v { font-size: 13pt; font-weight: 600; color: #1f4e5f; }
.sancai-result .luck { font-size: 9pt; color: #5a534a; }
.sancai-note { margin-top: 2mm; font-size: 9pt; color: #6a635a; }
.prose { white-space: pre-wrap; color: #2a2622; margin-bottom: 4mm; }
.hints { padding-left: 5mm; color: #4a453f; }
.footer-page { text-align: center; padding-top: 40mm; color: #6a635a; }
.disclaimer { font-size: 9pt; }
.tagline { margin-top: 8mm; color: #1f4e5f; letter-spacing: 0.08em; }
.brand-foot { margin-top: 16mm; letter-spacing: 0.35em; color: #8a8378; }
";
        }

        private static void Meta(StringBuilder sb, string label, string value)
        {
            sb.Append("<div><div class=\"meta-label\">").Append(H(label))
                .Append("</div><div class=\"meta-value\">").Append(H(value ?? ""))
                .AppendLine("</div></div>");
        }

        private static void PillarCard(StringBuilder sb, string label, string value)
        {
            sb.Append("<div class=\"pillar\"><div class=\"k\">").Append(H(label))
                .Append("</div><div class=\"v\">").Append(H(string.IsNullOrEmpty(value) ? "—" : value))
                .AppendLine("</div></div>");
        }
        private static string BuildWugeBoard(FlowWugeVisual w, bool compact = false)
        {
            if (w == null)
                return "<p class=\"muted\">（無三才五格資料）</p>";

            var chars = w.Chars ?? new List<FlowWugeChar>();
            if (chars.Count == 0)
                return "<p class=\"muted\">（無三才五格字畫資料）</p>";

            var showVirtualOne = !w.IsCompoundSurname;
            var n = chars.Count;
            var surLen = Math.Max(1, chars.Count(c => c.IsSurname));
            if (surLen > n) surLen = n;
            var givCount = n - surLen;
            var isSingleGiven = givCount <= 1;

            const double width = 560;
            var charSize = compact ? 36.0 : 44.0;
            var strokeW = compact ? 28.0 : 34.0;
            var strokeH = compact ? 24.0 : 28.0;
            var rowGap = compact ? 48.0 : 56.0;
            var boxW = compact ? 72.0 : 88.0;
            var boxH = compact ? 40.0 : 48.0;
            const double framePad = 10;
            // 單名地格／外格虛擬「1」需與上一字拉開，避免與人格框重疊（舊值 28 會跑版）
            var virtualGap = isSingleGiven ? rowGap : 28.0;

            var frameLeft = compact ? 96.0 : 150.0;
            var charX = frameLeft + framePad;
            var strokeX = charX + charSize + 8;
            var frameRight = strokeX + strokeW + framePad;
            var frameTop = showVirtualOne ? 22.0 : 8.0;
            var firstCy = frameTop + framePad + charSize / 2.0 + (showVirtualOne ? 6 : 0);
            var lastCy = firstCy + (n - 1) * rowGap;
            var frameBottom = lastCy + charSize / 2.0 + framePad;
            if (isSingleGiven && showVirtualOne)
                frameBottom = Math.Max(frameBottom, lastCy + virtualGap + strokeH / 2.0 + framePad);
            var rightRail = frameRight + (compact ? 12.0 : 16.0);
            var rightBoxX = rightRail + (compact ? 12.0 : 20.0);
            var leftBoxX = compact ? 10.0 : 28.0;
            var leftRail = frameLeft - (compact ? 12.0 : 16.0);

            // compact：viewBox 緊貼內容寬度，讓 width:100% 真正拉滿字卡
            var svgWidth = compact
                ? Math.Max(rightBoxX + boxW + 6, frameRight + 8)
                : width;

            var tianY1 = firstCy;
            var tianMid = showVirtualOne ? ((frameTop - 4) + tianY1) / 2.0 : firstCy;

            var renIdxA = surLen - 1;
            var renIdxB = Math.Min(surLen, n - 1);
            var renY0 = firstCy + renIdxA * rowGap;
            var renY1 = firstCy + renIdxB * rowGap;
            var renMid = (renY0 + renY1) / 2.0;

            var diIdxA = Math.Min(surLen, n - 1);
            var diY0 = firstCy + diIdxA * rowGap;
            var diY1 = lastCy;
            var diExtra = isSingleGiven && showVirtualOne;
            if (diExtra) diY1 = lastCy + virtualGap;
            var diMid = (diY0 + diY1) / 2.0;

            var waiMid = showVirtualOne
                ? ((frameTop - 4) + (isSingleGiven ? lastCy + virtualGap : lastCy)) / 2.0
                : (firstCy + lastCy) / 2.0;

            // 單名：人格／地格在右側易重疊，強制地格框下移
            if (diExtra)
            {
                var minDiMid = renMid + boxH + 10;
                if (diMid < minDiMid)
                {
                    diMid = minDiMid;
                    diY1 = Math.Max(diY1, diMid + (diMid - diY0));
                    frameBottom = Math.Max(frameBottom, diY1 + strokeH / 2.0 + framePad);
                }
            }

            var zongLineY = frameBottom + 10;
            var height = zongLineY + 6 + boxH + 6;
            // 右側地格框底部不得超出 viewBox
            height = Math.Max(height, diMid + boxH / 2.0 + 8);
            height = Math.Max(height, waiMid + boxH / 2.0 + 8);

            var sb = new StringBuilder();
            sb.AppendLine(compact
                ? "<div class=\"wuge-schematic wuge-schematic--compact\">"
                : "<div class=\"wuge-schematic\">");
            if (!compact)
                sb.AppendLine("<div class=\"wuge-schematic-title\">姓名五格示意圖</div>");
            sb.AppendLine(compact ? "<div class=\"wuge-body wuge-body--compact\">" : "<div class=\"wuge-body\">");
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<svg class=\"wuge-svg{0}\" viewBox=\"0 0 {1:0.##} {2:0.##}\" width=\"100%\" preserveAspectRatio=\"xMidYMin meet\" xmlns=\"http://www.w3.org/2000/svg\">",
                compact ? " wuge-svg--compact" : "", compact ? svgWidth : width, height);

            Action<double, double, double, double> line = (x1, y1, x2, y2) =>
            {
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<line x1=\"{0:0.##}\" y1=\"{1:0.##}\" x2=\"{2:0.##}\" y2=\"{3:0.##}\" stroke=\"#222\" stroke-width=\"1.15\" stroke-linecap=\"square\"/>",
                    x1, y1, x2, y2);
            };

            var v1X = charX + (charSize - strokeW) / 2.0;
            var v1Cy = frameTop - 4;
            if (showVirtualOne)
            {
                line(frameLeft, frameTop, v1X - 2, frameTop);
                line(v1X + strokeW + 2, frameTop, frameRight, frameTop);
                SbStrokeBox(sb, v1X, v1Cy - strokeH / 2.0, strokeW, strokeH, "1");
            }
            else
            {
                line(frameLeft, frameTop, frameRight, frameTop);
            }
            line(frameLeft, frameTop, frameLeft, frameBottom);
            line(frameRight, frameTop, frameRight, frameBottom);
            line(frameLeft, frameBottom, frameRight, frameBottom);

            for (var i = 0; i < n; i++)
            {
                var cy = firstCy + i * rowGap;
                var ch = chars[i];
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<rect x=\"{0:0.##}\" y=\"{1:0.##}\" width=\"{2:0.##}\" height=\"{2:0.##}\" fill=\"#fff\" stroke=\"#222\" stroke-width=\"1.3\"/>",
                    charX, cy - charSize / 2.0, charSize);
                SbSvgText(sb, charX + charSize / 2.0, cy, H(ch.Char), 20, true);
                SbStrokeBox(sb, strokeX, cy - strokeH / 2.0, strokeW, strokeH,
                    ch.Stroke.ToString(CultureInfo.InvariantCulture));
            }

            // 天格
            if (showVirtualOne)
            {
                line(v1X + strokeW, v1Cy, rightRail, v1Cy);
                line(frameRight, tianY1, rightRail, tianY1);
                line(rightRail, v1Cy, rightRail, tianY1);
                line(rightRail, tianMid, rightBoxX, tianMid);
            }
            else
            {
                var surBot = firstCy + (surLen - 1) * rowGap;
                line(frameRight, firstCy, rightRail, firstCy);
                line(frameRight, surBot, rightRail, surBot);
                if (Math.Abs(surBot - firstCy) > 1) line(rightRail, firstCy, rightRail, surBot);
                tianMid = (firstCy + surBot) / 2.0;
                line(rightRail, tianMid, rightBoxX, tianMid);
            }
            SbGridBox(sb, rightBoxX, tianMid - boxH / 2.0, boxW, boxH, "天格", w.Tian, w.TianWx);

            // 人格
            line(frameRight, renY0, rightRail, renY0);
            line(frameRight, renY1, rightRail, renY1);
            if (Math.Abs(renY1 - renY0) > 1) line(rightRail, renY0, rightRail, renY1);
            line(rightRail, renMid, rightBoxX, renMid);
            SbGridBox(sb, rightBoxX, renMid - boxH / 2.0, boxW, boxH, "人格", w.Ren, w.RenWx);

            // 地格
            line(frameRight, diY0, rightRail, diY0);
            if (diExtra)
            {
                var plusY = lastCy + virtualGap;
                SbStrokeBox(sb, strokeX, plusY - strokeH / 2.0, strokeW, strokeH, "1");
                line(strokeX + strokeW, plusY, rightRail, plusY);
                line(rightRail, diY0, rightRail, plusY);
                line(rightRail, diMid, rightBoxX, diMid);
            }
            else
            {
                line(frameRight, diY1, rightRail, diY1);
                if (Math.Abs(diY1 - diY0) > 1) line(rightRail, diY0, rightRail, diY1);
                line(rightRail, diMid, rightBoxX, diMid);
            }
            SbGridBox(sb, rightBoxX, diMid - boxH / 2.0, boxW, boxH, "地格", w.Di, w.DiWx);

            // 外格：貼齊外框左緣再連出
            if (showVirtualOne)
            {
                line(v1X, v1Cy, leftRail, v1Cy);
                if (isSingleGiven)
                {
                    var botY = lastCy + virtualGap;
                    SbStrokeBox(sb, v1X, botY - strokeH / 2.0, strokeW, strokeH, "1");
                    line(v1X, botY, leftRail, botY);
                    line(leftRail, v1Cy, leftRail, botY);
                    waiMid = (v1Cy + botY) / 2.0;
                }
                else
                {
                    line(frameLeft, lastCy, leftRail, lastCy);
                    line(leftRail, v1Cy, leftRail, lastCy);
                    waiMid = (v1Cy + lastCy) / 2.0;
                }
                line(leftRail, waiMid, leftBoxX + boxW, waiMid);
            }
            else
            {
                line(frameLeft, firstCy, leftRail, firstCy);
                line(frameLeft, lastCy, leftRail, lastCy);
                line(leftRail, firstCy, leftRail, lastCy);
                waiMid = (firstCy + lastCy) / 2.0;
                line(leftRail, waiMid, leftBoxX + boxW, waiMid);
            }
            SbGridBox(sb, leftBoxX, waiMid - boxH / 2.0, boxW, boxH, "外格", w.Wai, w.WaiWx);

            // 總格
            line(frameLeft, zongLineY, frameRight, zongLineY);
            var zongBoxX = (frameLeft + frameRight) / 2.0 - (boxW + 10) / 2.0;
            SbGridBox(sb, zongBoxX, zongLineY + 6, boxW + 10, boxH, "總格", w.Zong, w.ZongWx);

            sb.AppendLine("</svg>");
            sb.AppendLine(compact ? "<aside class=\"wuge-side wuge-side--compact\">" : "<aside class=\"wuge-side\">");
            if (!string.IsNullOrWhiteSpace(w.SancaiNote))
                sb.Append("<div class=\"sancai-note\">").Append(H(w.SancaiNote)).AppendLine("</div>");
            else
            {
                sb.Append("<div class=\"sancai-note\">三才 ")
                    .Append(H(string.IsNullOrEmpty(w.Sancai) ? (w.TianWx + w.RenWx + w.DiWx) : w.Sancai))
                    .Append("（").Append(H(w.SancaiLuck)).Append("）")
                    .AppendLine("</div>");
            }
            if (compact)
            {
                sb.Append("<div class=\"wuge-luck-line\">吉凶：天 ").Append(H(w.TianLuck))
                    .Append("　人 ").Append(H(w.RenLuck))
                    .Append("　地 ").Append(H(w.DiLuck))
                    .Append("　外 ").Append(H(w.WaiLuck))
                    .Append("　總 ").Append(H(w.ZongLuck)).AppendLine("</div>");
            }
            else
            {
                sb.Append("<div class=\"wuge-luck-line\">吉凶：天 ")
                    .Append(H(w.TianLuck)).Append("<br/>人 ").Append(H(w.RenLuck))
                    .Append("<br/>地 ").Append(H(w.DiLuck)).Append("<br/>外 ").Append(H(w.WaiLuck))
                    .Append("<br/>總 ").Append(H(w.ZongLuck)).AppendLine("</div>");
            }
            sb.AppendLine("</aside>");
            sb.AppendLine("</div>"); // wuge-body
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static void SbSvgText(StringBuilder sb, double x, double y, string text, double fontSize, bool bold)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<text x=\"{0:0.##}\" y=\"{1:0.##}\" text-anchor=\"middle\" font-size=\"{2:0.##}\" fill=\"#1c1a17\"{3} font-family=\"Microsoft JhengHei, DFKai-SB, sans-serif\">{4}</text>",
                x, y + fontSize * 0.35, fontSize, bold ? " font-weight=\"600\"" : "", text);
        }

        private static void SbStrokeBox(StringBuilder sb, double x, double y, double w, double h, string text)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<rect x=\"{0:0.##}\" y=\"{1:0.##}\" width=\"{2:0.##}\" height=\"{3:0.##}\" fill=\"#fff\" stroke=\"#222\" stroke-width=\"1.1\"/>",
                x, y, w, h);
            SbSvgText(sb, x + w / 2.0, y + h / 2.0, H(text), 13, false);
        }

        private static void SbGridBox(StringBuilder sb, double x, double y, double w, double h, string title, int num, string wx)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<rect x=\"{0:0.##}\" y=\"{1:0.##}\" width=\"{2:0.##}\" height=\"{3:0.##}\" fill=\"#fff\" stroke=\"#222\" stroke-width=\"1.2\"/>",
                x, y, w, h);
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<line x1=\"{0:0.##}\" y1=\"{1:0.##}\" x2=\"{2:0.##}\" y2=\"{1:0.##}\" stroke=\"#222\" stroke-width=\"1\"/>",
                x, y + 17, x + w);
            SbSvgText(sb, x + w / 2.0, y + 9, H(title), 11, false);
            var body = num.ToString(CultureInfo.InvariantCulture) + (string.IsNullOrEmpty(wx) ? "" : " " + wx);
            SbSvgText(sb, x + w / 2.0, y + 17 + (h - 17) / 2.0, H(body), 14, true);
        }


        private static void MonthCardBlock(StringBuilder sb, string title, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            var tone = "";
            if (string.Equals(title, "宜", StringComparison.Ordinal)) tone = " yi-tone";
            else if (string.Equals(title, "忌", StringComparison.Ordinal)) tone = " ji-tone";
            sb.Append("<div class=\"mc-block").Append(tone).Append("\"><div class=\"t\">").Append(H(title))
                .Append("</div><div class=\"c\">").Append(H(content.Trim())).AppendLine("</div></div>");
        }

        private static void WugeCell(StringBuilder sb, string label, int num, string wx, string luck)
        {
            var color = WxColor(wx);
            sb.Append("<div class=\"wuge-cell\" style=\"border-top-color:")
                .Append(color).Append("\">");
            sb.Append("<div class=\"k\">").Append(H(label)).Append("</div>");
            sb.Append("<div class=\"num\">").Append(num).Append("</div>");
            sb.Append("<div class=\"wx-pill\" style=\"background:")
                .Append(color).Append("\">").Append(H(string.IsNullOrEmpty(wx) ? "—" : wx)).Append("</div>");
            sb.Append("<div class=\"luck\">").Append(H(string.IsNullOrEmpty(luck) ? "—" : luck)).Append("</div>");
            sb.AppendLine("</div>");
        }

        private static void SancaiNode(StringBuilder sb, string label, string wx)
        {
            var color = WxColor(wx);
            sb.Append("<div class=\"sancai-node\"><div class=\"k\">").Append(H(label))
                .Append("</div><div class=\"wx\" style=\"background:").Append(color).Append("\">")
                .Append(H(string.IsNullOrEmpty(wx) ? "—" : wx))
                .AppendLine("</div></div>");
        }

        private static string WxColor(string wx)
        {
            if (string.IsNullOrEmpty(wx)) return "#9a9288";
            if (wx.IndexOf("木", StringComparison.Ordinal) >= 0) return "#3d7a5a";
            if (wx.IndexOf("火", StringComparison.Ordinal) >= 0) return "#a85a3a";
            if (wx.IndexOf("土", StringComparison.Ordinal) >= 0) return "#8b6914";
            if (wx.IndexOf("金", StringComparison.Ordinal) >= 0) return "#6a7a8a";
            if (wx.IndexOf("水", StringComparison.Ordinal) >= 0) return "#2a6f7a";
            return "#9a9288";
        }

        private static string StripMonthMetaPrefix(string text, MonthAnalysisBlock m)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var t = text.Trim();

            // 常見 Summary 前綴：3月（庚寅）〔穩〕…
            var bracket = t.IndexOf('〕');
            if (bracket >= 0 && bracket + 1 < t.Length)
                t = t.Substring(bracket + 1).TrimStart('：', ':', ' ', '　');

            if (m != null)
            {
                if (!string.IsNullOrEmpty(m.Level))
                {
                    t = t.Replace("流月定位：" + m.Level + "。", "");
                    t = t.Replace("本月定位：" + m.Level + "。", "");
                    t = t.Replace("流月定位：" + m.Level, "");
                    t = t.Replace("本月定位：" + m.Level, "");
                }

                var prefixes = new List<string>();
                prefixes.Add(m.Month + "月");
                if (!string.IsNullOrEmpty(m.MonthGanzhi))
                {
                    prefixes.Add("（" + m.MonthGanzhi + "）");
                    prefixes.Add("(" + m.MonthGanzhi + ")");
                    prefixes.Add(m.MonthGanzhi);
                }
                if (!string.IsNullOrEmpty(m.Level))
                {
                    prefixes.Add("〔" + m.Level + "〕");
                    prefixes.Add("[" + m.Level + "]");
                }
                foreach (var pfx in prefixes)
                {
                    if (string.IsNullOrEmpty(pfx)) continue;
                    if (t.StartsWith(pfx, StringComparison.Ordinal))
                        t = t.Substring(pfx.Length).TrimStart('：', ':', ' ', '　', '，', ',', '。', '.');
                }
            }

            return t.Trim();
        }

        private static void GradeChip(StringBuilder sb, string label, double score)
        {
            sb.Append("<div class=\"chip\"><div class=\"k\">").Append(H(label))
                .Append("</div><div class=\"v\">").Append(H(NameScorer.GradeLabel(score))).Append("</div>");
            sb.AppendLine("</div>");
        }

        /// <summary>將命名文案中的分數改為評等文字，或直接剔除數字分數。</summary>
        private static string StripNamingScores(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var t = text;
            // 合參分 85 → 合參評等上佳
            t = Regex.Replace(t, @"合參分\s*(\d+(?:\.\d+)?)", m =>
            {
                double v;
                return double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v)
                    ? "合參評等" + NameScorer.GradeLabel(v)
                    : "合參評等";
            });
            // 組合美感 85 / 組合美感85 → 組合美感評等上佳
            t = Regex.Replace(t, @"組合美感\s*(\d+(?:\.\d+)?)", m =>
            {
                double v;
                return double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v)
                    ? "組合美感評等" + NameScorer.GradeLabel(v)
                    : "組合美感評等";
            });
            // 總分 85、評等上佳 → 評等上佳
            t = Regex.Replace(t, @"總分\s*\d+(?:\.\d+)?\s*[、，,]?\s*", "");
            // 綜合評等上佳（85分）→ 綜合評等上佳
            t = Regex.Replace(t, @"（\s*\d+(?:\.\d+)?\s*分?\s*）", "");
            t = Regex.Replace(t, @"\(\s*\d+(?:\.\d+)?\s*分?\s*\)", "");
            // 孤立「85分」
            t = Regex.Replace(t, @"\d+(?:\.\d+)?\s*分", "");
            return t.Trim();
        }

        private static string BuildRenameCompareCards(FlowNamingCopy naming, FlowProfile profile)
        {
            if (naming == null) return "";
            var sb = new StringBuilder();
            sb.AppendLine("<h2>專業改名 － 前後比較</h2>");

            // 1) 原名診斷（去分數、三才五格段、改名焦點、數字標號）
            if (!string.IsNullOrWhiteSpace(naming.OriginalEval))
            {
                sb.AppendLine("<div class=\"rename-note-card rename-note-card--eval\">");
                sb.AppendLine("<div class=\"rn-title\">原名診斷</div>");
                sb.Append("<div class=\"rn-body\">")
                    .Append(Nl(StripNumericLabels(StripDiagnosisScores(naming.OriginalEval))))
                    .AppendLine("</div></div>");
            }

            // 2) 改名方向：個別字卡同一 row；其後生活關注
            sb.AppendLine(BuildRenameDirectionRow(naming));

            // 3) 原名／新名三才五格字卡
            var cmp = naming.Compare;
            var oldName = !string.IsNullOrWhiteSpace(naming.OriginalName)
                ? naming.OriginalName
                : (cmp != null ? cmp.OriginalName : "");
            var newName = profile != null ? profile.FullName : (cmp != null ? cmp.NewName : "");
            var oldWuge = naming.OriginalWuge
                ?? (cmp != null && cmp.Wuge != null
                    ? FlowVisualizationMapper.MapWugeVisual(cmp.Wuge.Original, null)
                    : null);
            var newWuge = naming.NewWuge
                ?? (profile != null ? profile.Wuge : null)
                ?? (cmp != null && cmp.Wuge != null
                    ? FlowVisualizationMapper.MapWugeVisual(cmp.Wuge.NewName, null)
                    : null);

            sb.AppendLine("<div class=\"rename-vs-grid\">");
            AppendRenameWugeCard(sb, "old", "原名", oldName, oldWuge);
            AppendRenameWugeCard(sb, "new", "新名", newName, newWuge);
            sb.AppendLine("</div>");

            // 建議理由（結論不顯示；去數字標號）
            if (cmp != null && !string.IsNullOrWhiteSpace(cmp.RecommendReason))
            {
                sb.AppendLine("<div class=\"rename-note-grid\">");
                sb.AppendLine("<div class=\"rename-note-card rename-note-card--reason rename-note-card--span2\">");
                sb.AppendLine("<div class=\"rn-title\">建議理由</div>");
                sb.Append("<div class=\"rn-body\">")
                    .Append(Nl(StripNumericLabels(StripRecommendHeader(cmp.RecommendReason))))
                    .AppendLine("</div></div>");
                sb.AppendLine("</div>");
            }
            else if (!string.IsNullOrWhiteSpace(naming.ComparisonText))
            {
                sb.AppendLine("<div class=\"rename-note-grid\">");
                sb.AppendLine("<div class=\"rename-note-card rename-note-card--reason rename-note-card--span2\">");
                sb.AppendLine("<div class=\"rn-title\">原名／新名比較</div>");
                sb.Append("<div class=\"rn-body\">")
                    .Append(Nl(StripNumericLabels(naming.ComparisonText)))
                    .AppendLine("</div></div>");
                sb.AppendLine("</div>");
            }

            var hasMeta = !string.IsNullOrWhiteSpace(naming.CharAnalysis)
                || !string.IsNullOrWhiteSpace(naming.Combo)
                || !string.IsNullOrWhiteSpace(naming.DestinyNote)
                || !string.IsNullOrWhiteSpace(naming.Image);
            if (hasMeta)
            {
                sb.AppendLine("<h2>用字與命理</h2>");
                sb.AppendLine(BuildNewbornCharCards(naming, profile));
            }
            return sb.ToString();
        }

        private static string BuildRenameDirectionRow(FlowNamingCopy naming)
        {
            var dirs = naming.DirectionItems ?? new List<string>();
            var focus = naming.FocusItems ?? new List<string>();
            // 備援：從 DirectionsText 解析
            if (dirs.Count == 0 && !string.IsNullOrWhiteSpace(naming.DirectionsText))
                dirs = ParseDirectionsFromText(naming.DirectionsText);
            if (focus.Count == 0 && !string.IsNullOrWhiteSpace(naming.DirectionsText))
                focus = ParseFocusFromText(naming.DirectionsText);

            if (dirs.Count == 0 && focus.Count == 0 && string.IsNullOrWhiteSpace(naming.RenameReason))
                return "";

            var sb = new StringBuilder();
            sb.AppendLine("<div class=\"rename-dir-block\">");
            sb.AppendLine("<div class=\"rename-dir-heading\">改名方向</div>");
            if (!string.IsNullOrWhiteSpace(naming.RenameReason))
            {
                sb.Append("<div class=\"rename-reason-line\">改名原因：")
                    .Append(H(naming.RenameReason.Trim())).AppendLine("</div>");
            }
            if (dirs.Count > 0)
            {
                sb.AppendLine("<div class=\"rename-dir-row\">");
                foreach (var d in dirs)
                {
                    sb.Append("<div class=\"rename-dir-chip\">").Append(H(d)).AppendLine("</div>");
                }
                sb.AppendLine("</div>");
            }
            else
            {
                sb.AppendLine("<div class=\"rename-dir-row\">");
                sb.AppendLine("<div class=\"rename-dir-chip rename-dir-chip--muted\">未勾選改善方向</div>");
                sb.AppendLine("</div>");
            }
            if (focus.Count > 0)
            {
                sb.AppendLine("<div class=\"rename-focus-row\">");
                sb.AppendLine("<div class=\"rename-focus-label\">生活關注</div>");
                sb.AppendLine("<div class=\"rename-focus-chips\">");
                foreach (var f in focus)
                    sb.Append("<span class=\"rename-focus-chip\">").Append(H(f)).AppendLine("</span>");
                sb.AppendLine("</div></div>");
            }
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static List<string> ParseDirectionsFromText(string text)
        {
            var list = new List<string>();
            foreach (var raw in (text ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                var line = StripNumericLabels(raw).Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("本次改名", StringComparison.Ordinal)) continue;
                if (line.StartsWith("改名原因", StringComparison.Ordinal)) continue;
                if (line.StartsWith("生活關注", StringComparison.Ordinal)) continue;
                if (line.StartsWith("（未勾選", StringComparison.Ordinal)) continue;
                list.Add(line);
            }
            return list;
        }

        private static List<string> ParseFocusFromText(string text)
        {
            foreach (var raw in (text ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith("生活關注：", StringComparison.Ordinal)
                    || line.StartsWith("生活關注:", StringComparison.Ordinal))
                {
                    var rest = line.Substring(line.IndexOf('：') >= 0
                        ? line.IndexOf('：') + 1
                        : line.IndexOf(':') + 1).Trim();
                    return rest.Replace("；", ",").Replace("、", ",").Replace("，", ",")
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .ToList();
                }
            }
            return new List<string>();
        }

        private static void AppendRenameWugeCard(
            StringBuilder sb, string side, string label, string name, FlowWugeVisual wuge)
        {
            sb.Append("<div class=\"rename-vs-card rename-vs-card--").Append(side).Append("\">");
            sb.Append("<div class=\"rv-label\">").Append(H(label)).AppendLine("</div>");
            sb.Append("<div class=\"rv-name\">").Append(H(string.IsNullOrWhiteSpace(name) ? "—" : name)).AppendLine("</div>");
            sb.AppendLine("<div class=\"rv-wuge-board\">");
            sb.AppendLine(BuildWugeBoard(wuge, compact: true));
            sb.AppendLine("</div></div>");
        }

        private static string BuildRenameMessageGrid(FlowNamingCopy naming)
        {
            if (naming == null) return "";
            var cards = new[]
            {
                new { Title = "改名理念", Text = StripNamingScores(naming.Idea), Css = "naming-msg-card--idea" },
                new { Title = "命名故事", Text = StripNamingScores(naming.Story), Css = "naming-msg-card--story" },
                new { Title = "人生期許", Text = StripNamingScores(naming.Hope), Css = "naming-msg-card--hope" },
                new { Title = "祝福", Text = StripNamingScores(naming.Blessing), Css = "naming-msg-card--blessing" },
            };
            if (cards.All(c => string.IsNullOrWhiteSpace(c.Text))) return "";
            var sb = new StringBuilder();
            sb.AppendLine("<div class=\"naming-msg-grid naming-msg-grid--stack\">");
            foreach (var c in cards)
            {
                if (string.IsNullOrWhiteSpace(c.Text)) continue;
                sb.Append("<div class=\"naming-msg-card ").Append(c.Css).Append("\">");
                sb.Append("<div class=\"nm-title\">").Append(H(c.Title)).Append("</div>");
                sb.Append("<div class=\"nm-body\">").Append(Nl(StripNumericLabels(c.Text))).AppendLine("</div></div>");
            }
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string StripRenameDirectionHeader(string text)
        {
            var t = (text ?? "").Trim();
            if (t.StartsWith("本次改名主要方向：", StringComparison.Ordinal)
                || t.StartsWith("本次改名主要方向:", StringComparison.Ordinal))
            {
                var idx = t.IndexOf('\n');
                if (idx >= 0) t = t.Substring(idx + 1).Trim();
                else t = "";
            }
            return t;
        }

        private static string StripRecommendHeader(string text)
        {
            var t = (text ?? "").Trim();
            if (t.StartsWith("【推薦理由】", StringComparison.Ordinal))
            {
                var idx = t.IndexOf('\n');
                t = idx >= 0 ? t.Substring(idx + 1).Trim() : "";
            }
            return t;
        }

        /// <summary>去掉①②③／1.／（1）等數字標號，僅影響 PDF 顯示文案。</summary>
        private static string StripNumericLabels(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder();
            foreach (var raw in lines)
            {
                var line = raw ?? "";
                // 行首圓圈數字、括號數字、阿拉伯數字標號
                line = System.Text.RegularExpressions.Regex.Replace(
                    line,
                    @"^\s*(?:[①②③④⑤⑥⑦⑧⑨⑩⑪⑫⑬⑭⑮⑯⑰⑱⑲⑳]|[⑴⑵⑶⑷⑸⑹⑺⑻⑼⑽]|[（(]?\d+[）)]|[0-9]+[\.．、])\s*",
                    "");
                // 行內殘留的單獨圓圈標號（如「① 命理：」已去行首後可能剩標題冒號）
                line = System.Text.RegularExpressions.Regex.Replace(
                    line,
                    @"[①②③④⑤⑥⑦⑧⑨⑩⑪⑫⑬⑭⑮⑯⑰⑱⑲⑳]\s*",
                    "");
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line);
            }
            return sb.ToString().Trim();
        }

        /// <summary>原名診斷：隱藏分數、三才五格段、改名焦點，保留質性描述。</summary>
        private static string StripDiagnosisScores(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var kept = new List<string>();
            var skipWugeBlock = false;
            foreach (var raw in lines)
            {
                var line = (raw ?? "").TrimEnd();
                var trimmed = line.TrimStart();

                // 整段略過「原名三才五格」及其後格位列
                if (trimmed.StartsWith("原名三才五格", StringComparison.Ordinal))
                {
                    skipWugeBlock = true;
                    continue;
                }
                if (skipWugeBlock)
                {
                    if (trimmed.Length == 0) { skipWugeBlock = false; continue; }
                    if (trimmed.StartsWith("天格", StringComparison.Ordinal)
                        || trimmed.StartsWith("外格", StringComparison.Ordinal)
                        || trimmed.StartsWith("三才", StringComparison.Ordinal)
                        || trimmed.StartsWith("人格", StringComparison.Ordinal)
                        || trimmed.StartsWith("地格", StringComparison.Ordinal)
                        || trimmed.StartsWith("總格", StringComparison.Ordinal))
                        continue;
                    skipWugeBlock = false;
                }

                // 對應改名焦點不顯示
                if (trimmed.StartsWith("對應改名焦點", StringComparison.Ordinal))
                    continue;

                // 分數彙總列
                if (trimmed.StartsWith("八字 ", StringComparison.Ordinal)
                    || trimmed.StartsWith("音韻 ", StringComparison.Ordinal)
                    || trimmed.StartsWith("風格 ", StringComparison.Ordinal))
                    continue;
                // 「原名整體：75　…」→ 去掉分數，保留其後質性句
                if (trimmed.StartsWith("原名整體：", StringComparison.Ordinal)
                    || trimmed.StartsWith("原名整體:", StringComparison.Ordinal))
                {
                    var rest = System.Text.RegularExpressions.Regex.Replace(
                        trimmed, @"^原名整體[:：]\s*\d+\s*", "");
                    if (string.IsNullOrWhiteSpace(rest)) continue;
                    kept.Add(rest.Trim());
                    continue;
                }
                // 「五格分 80.0」尾段
                line = System.Text.RegularExpressions.Regex.Replace(
                    line, @"\s*五格分\s*\d+(?:\.\d+)?", "");
                // 「表現良好（80）」→ 去掉括號分數
                line = System.Text.RegularExpressions.Regex.Replace(
                    line, @"（\d+(?:\.\d+)?）", "");
                line = System.Text.RegularExpressions.Regex.Replace(
                    line, @"\(\d+(?:\.\d+)?\)", "");
                kept.Add(line);
            }
            return string.Join("\n", kept).Trim();
        }

        private static string BuildNewbornCharCards(FlowNamingCopy naming, FlowProfile profile)
        {
            if (naming == null) return "";
            var sb = new StringBuilder();
            sb.AppendLine("<div class=\"char-card-grid\">");
            var entries = naming.CharEntries;
            if (entries == null || entries.Count == 0)
            {
                // 備援：由字串拆解（可能因字義含分號而漏字）
                var wxList = (profile != null && !string.IsNullOrWhiteSpace(profile.CharWuxingText))
                    ? profile.CharWuxingText.Split(new[] { '、', '，', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>();
                var idx = 0;
                entries = ParseCharSegments(naming.CharAnalysis).Select(e =>
                {
                    var wx = idx < wxList.Count ? wxList[idx].Trim() : "";
                    idx++;
                    return new FlowCharEntry { Char = e.Char, Meaning = e.Body, Wuxing = wx };
                }).ToList();
            }
            var ei = 0;
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Char)) { ei++; continue; }
                var wx = entry.Wuxing ?? "";
                var color = WxAccentColor(wx, ei);
                sb.Append("<div class=\"char-card\">");
                sb.Append("<div class=\"cc-head\">");
                sb.Append("<span class=\"cc-char\" style=\"background:").Append(color).Append("\">")
                    .Append(H(entry.Char)).Append("</span>");
                if (!string.IsNullOrWhiteSpace(wx))
                    sb.Append("<span class=\"cc-wx\">").Append(H(wx)).Append("行</span>");
                sb.AppendLine("</div>");
                sb.Append("<div class=\"cc-body\">").Append(H(entry.Meaning ?? "")).AppendLine("</div></div>");
                ei++;
            }
            AppendMetaCharCard(sb, "char-card--combo", "組合評述", StripNamingScores(naming.Combo));
            AppendMetaCharCard(sb, "char-card--destiny", "命理對應", StripNamingScores(naming.DestinyNote));
            AppendMetaCharCard(sb, "char-card--image", "意象", StripNamingScores(naming.Image));
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static void AppendMetaCharCard(StringBuilder sb, string cssClass, string label, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            sb.Append("<div class=\"char-card char-card--meta ").Append(cssClass).Append("\">");
            sb.Append("<div class=\"cc-label\">").Append(H(label)).Append("</div>");
            sb.Append("<div class=\"cc-body\">").Append(H(text)).AppendLine("</div></div>");
        }

        private static string BuildNewbornMessageGrid(FlowNamingCopy naming)
        {
            if (naming == null) return "";
            var cards = new[]
            {
                new { Key = "story", Title = "命名故事", Text = StripNamingScores(naming.Story), Css = "naming-msg-card--story" },
                new { Key = "idea", Title = "命名理念", Text = StripNamingScores(naming.Idea), Css = "naming-msg-card--idea" },
                new { Key = "blessing", Title = "祝福", Text = StripNamingScores(naming.Blessing), Css = "naming-msg-card--blessing" },
                new { Key = "hope", Title = "人生期許", Text = StripNamingScores(naming.Hope), Css = "naming-msg-card--hope" },
            };
            if (cards.All(c => string.IsNullOrWhiteSpace(c.Text))) return "";
            var sb = new StringBuilder();
            sb.AppendLine("<div class=\"naming-msg-grid\">");
            foreach (var c in cards)
            {
                if (string.IsNullOrWhiteSpace(c.Text)) continue;
                sb.Append("<div class=\"naming-msg-card ").Append(c.Css).Append("\">");
                sb.Append("<div class=\"nm-title\">").Append(H(c.Title)).Append("</div>");
                sb.Append("<div class=\"nm-body\">").Append(Nl(c.Text)).AppendLine("</div></div>");
            }
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private sealed class CharSegment
        {
            public string Char { get; set; }
            public string Body { get; set; }
        }

        private static List<CharSegment> ParseCharSegments(string raw)
        {
            var list = new List<CharSegment>();
            if (string.IsNullOrWhiteSpace(raw)) return list;
            var parts = raw.Split(new[] { '；', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var t = part.Trim();
                if (t.Length == 0) continue;
                var sep = t.IndexOf('：');
                if (sep < 0) sep = t.IndexOf(':');
                if (sep > 0 && sep < t.Length - 1)
                {
                    var ch = t.Substring(0, sep).Trim();
                    var body = t.Substring(sep + 1).Trim();
                    if (ch.Length <= 2)
                    {
                        list.Add(new CharSegment { Char = ch, Body = body });
                        continue;
                    }
                }
                list.Add(new CharSegment { Char = "字", Body = t });
            }
            return list;
        }

        private static string WxAccentColor(string wx, int fallbackIndex)
        {
            switch ((wx ?? "").Trim())
            {
                case "木": return "#2e7d4f";
                case "火": return "#b45309";
                case "土": return "#8b6914";
                case "金": return "#5c6b8a";
                case "水": return "#1f4e5f";
                default:
                    var palette = new[] { "#1f4e5f", "#2a6f7a", "#6b4c7a", "#8b6914", "#2e7d4f" };
                    return palette[Math.Abs(fallbackIndex) % palette.Length];
            }
        }

        private static void Block(StringBuilder sb, string title, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            sb.Append("<div class=\"block\"><div class=\"t\">").Append(H(title))
                .Append("</div><div class=\"c\">").Append(H(content)).AppendLine("</div></div>");
        }

        private static void CompactBlock(StringBuilder sb, string title, string content, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            sb.Append("<div class=\"cblock\"><span class=\"t\">").Append(H(title))
                .Append("</span><span class=\"c\">").Append(H(Truncate(content, maxChars)))
                .AppendLine("</span></div>");
        }

        private static void Hint(StringBuilder sb, string title, List<int> months)
        {
            if (months == null || months.Count == 0) return;
            sb.Append("<li>").Append(H(title)).Append("：")
                .Append(H(string.Join("、", months.Select(m => m + "月"))))
                .AppendLine("</li>");
        }

        private static void HintInline(StringBuilder sb, string title, List<int> months)
        {
            if (months == null || months.Count == 0) return;
            sb.Append("<span><b>").Append(H(title)).Append("</b> ")
                .Append(H(string.Join("、", months.Select(m => m + "月"))))
                .Append("</span>");
        }

        private static void RhythmRow(StringBuilder sb, string tag, string tagClass, string desc, List<int> months)
        {
            var rowClass = "rhythm-row";
            if (tagClass == "tag-go") rowClass += " go";
            else if (tagClass == "tag-steady") rowClass += " steady";
            else if (tagClass == "tag-adjust") rowClass += " adjust";
            else if (tagClass == "tag-caution") rowClass += " caution";

            sb.Append("<div class=\"").Append(rowClass).Append("\">");
            sb.Append("<span class=\"rhythm-tag ").Append(tagClass).Append("\">").Append(H(tag)).Append("</span>");
            sb.Append("<div class=\"rhythm-body\">");
            sb.Append("<div class=\"rhythm-desc\">").Append(H(desc)).Append("</div>");
            if (months != null && months.Count > 0)
                sb.Append("<div class=\"rhythm-months\">").Append(H(string.Join("、", months.OrderBy(m => m).Select(m => m + "月")))).Append("</div>");
            else
                sb.Append("<div class=\"rhythm-empty\">（本年無）</div>");
            sb.AppendLine("</div></div>");
        }

        private static string BuildActionCards(FlowNarrative narrative)
        {
            var buckets = narrative != null ? narrative.ActionBuckets : null;
            if (buckets == null || !buckets.Any(b => b.Items != null && b.Items.Count > 0))
            {
                if (narrative != null && !string.IsNullOrWhiteSpace(narrative.Actions))
                    return "<div class=\"prose\">" + Nl(narrative.Actions) + "</div>";
                return "";
            }

            var sb = new StringBuilder();
            sb.AppendLine("<div class=\"action-cards\">");
            foreach (var bucket in buckets)
            {
                var kindClass = string.Equals(bucket.Kind, "dont", StringComparison.OrdinalIgnoreCase) ? "dont" : "do";
                sb.Append("<div class=\"action-card ").Append(kindClass).Append("\">");
                sb.Append("<div class=\"ac-title\">").Append(H(bucket.Title ?? "")).AppendLine("</div>");
                if (bucket.Items == null || bucket.Items.Count == 0)
                {
                    sb.AppendLine("<div class=\"ac-empty\">（無條目）</div>");
                }
                else
                {
                    sb.AppendLine("<ul>");
                    foreach (var item in bucket.Items)
                    {
                        sb.Append("<li>").Append(H(item.Text ?? "")).AppendLine("</li>");
                    }
                    sb.AppendLine("</ul>");
                }
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string Truncate(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || maxChars <= 0) return "";
            var t = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
            while (t.Contains("  ")) t = t.Replace("  ", " ");
            if (t.Length <= maxChars) return t;
            if (maxChars <= 1) return "…";
            return t.Substring(0, maxChars - 1) + "…";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null) return "";
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
            }
            return "";
        }

        private static bool HasMonthHints(YearAnalysisBlock y)
        {
            return (y.StrongMonths != null && y.StrongMonths.Count > 0)
                || (y.StableMonths != null && y.StableMonths.Count > 0)
                || (y.AdjustMonths != null && y.AdjustMonths.Count > 0)
                || (y.CautionMonths != null && y.CautionMonths.Count > 0)
                || !string.IsNullOrWhiteSpace(y.MonthGuideCareer)
                || !string.IsNullOrWhiteSpace(y.MonthGuideWealth)
                || !string.IsNullOrWhiteSpace(y.MonthGuideRelationship);
        }

        private static bool HasNarrative(FlowNarrative n)
        {
            return !string.IsNullOrWhiteSpace(n.Overview)
                || !string.IsNullOrWhiteSpace(n.Phases)
                || !string.IsNullOrWhiteSpace(n.Trend)
                || !string.IsNullOrWhiteSpace(n.Actions)
                || (n.ActionBuckets != null && n.ActionBuckets.Any(b => b.Items != null && b.Items.Count > 0));
        }

        private static string H(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string Nl(string s)
        {
            return H(s).Replace("\r\n", "\n").Replace("\n", "<br/>");
        }

        private static string F0(double v)
        {
            return v.ToString("0", CultureInfo.InvariantCulture);
        }

        private static string ToFileUri(string path)
        {
            return new Uri(Path.GetFullPath(path)).AbsoluteUri;
        }
    }
}
