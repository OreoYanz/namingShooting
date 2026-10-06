using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Bazi;
using Mingxu.Core.Models;

namespace Mingxu.Export.Flow
{
    /// <summary>
    /// Web / PDF 共用視覺化資料。僅做 Mapping／Formatting／Grouping／Sorting，不重算命理分數。
    /// </summary>
    public sealed class FlowVisualizationData
    {
        public string ReportMode { get; set; }
        public string ReportTitle { get; set; }
        public FlowProfile Profile { get; set; }
        public FlowNamingCopy Naming { get; set; }
        public List<DayunSegment> DayunTimeline { get; set; }
        /// <summary>報告期間內各大運涵蓋的年份標註。</summary>
        public List<DayunPeriodNote> DayunPeriods { get; set; }
        public List<YearTrendPoint> YearlyTrend { get; set; }
        public List<YearAnalysisBlock> YearlyAnalysis { get; set; }
        public List<MonthTrendSeries> MonthlyTrend { get; set; }
        public List<MonthAnalysisBlock> MonthlyAnalysis { get; set; }
        public FlowNarrative Narrative { get; set; }
        public int ReportStartYear { get; set; }
        public int ReportEndYear { get; set; }
        public string GeneratedAt { get; set; }
        public string LogoPath { get; set; }
    }

    public sealed class FlowNamingCopy
    {
        public string CharAnalysis { get; set; }
        public string Combo { get; set; }
        public string DestinyNote { get; set; }
        public string Image { get; set; }
        public string Story { get; set; }
        public string Idea { get; set; }
        public string Blessing { get; set; }
        public string Hope { get; set; }
        public string ParentsText { get; set; }
        /// <summary>改名：原姓名。</summary>
        public string OriginalName { get; set; }
        /// <summary>改名：原因／改善方向／關注（純文字備援）。</summary>
        public string DirectionsText { get; set; }
        /// <summary>改名：改善方向清單（字卡）。</summary>
        public List<string> DirectionItems { get; set; }
        /// <summary>改名：生活關注清單。</summary>
        public List<string> FocusItems { get; set; }
        /// <summary>改名：改名原因。</summary>
        public string RenameReason { get; set; }
        /// <summary>改名：原名診斷摘要。</summary>
        public string OriginalEval { get; set; }
        /// <summary>改名：原名／新名比較報告（純文字備援）。</summary>
        public string ComparisonText { get; set; }
        /// <summary>改名：結構化前後比較（字卡用）。</summary>
        public RenameComparison Compare { get; set; }
        /// <summary>改名：原名評等。</summary>
        public string OriginalGrade { get; set; }
        /// <summary>改名：原名三才五格示意。</summary>
        public FlowWugeVisual OriginalWuge { get; set; }
        /// <summary>改名：新名三才五格示意。</summary>
        public FlowWugeVisual NewWuge { get; set; }
        /// <summary>用字解析字卡（依名字字序，避免字串拆解漏字）。</summary>
        public List<FlowCharEntry> CharEntries { get; set; }
    }

    public sealed class FlowCharEntry
    {
        public string Char { get; set; }
        public string Meaning { get; set; }
        public string Wuxing { get; set; }
    }

    public sealed class FlowProfile
    {
        public string FullName { get; set; }
        public string Surname { get; set; }
        public string GenderLabel { get; set; }
        public string BirthDateText { get; set; }
        public string BirthTimeText { get; set; }
        public string BirthPlace { get; set; }
        public string LunarText { get; set; }
        public string YearPillar { get; set; }
        public string MonthPillar { get; set; }
        public string DayPillar { get; set; }
        public string HourPillar { get; set; }
        public string DayMaster { get; set; }
        public string DayMasterWuxing { get; set; }
        public string XiYongText { get; set; }
        public string JiShenText { get; set; }
        public string WugeSummary { get; set; }
        public FlowWugeVisual Wuge { get; set; }
        public string CharWuxingText { get; set; }
        public double TotalScore { get; set; }
        public string Grade { get; set; }
    }

    public sealed class FlowWugeVisual
    {
        public string Surname { get; set; }
        public string Given { get; set; }
        public bool IsCompoundSurname { get; set; }
        public List<FlowWugeChar> Chars { get; set; }
        public int Tian { get; set; }
        public int Ren { get; set; }
        public int Di { get; set; }
        public int Wai { get; set; }
        public int Zong { get; set; }
        public string TianWx { get; set; }
        public string RenWx { get; set; }
        public string DiWx { get; set; }
        public string WaiWx { get; set; }
        public string ZongWx { get; set; }
        public string TianLuck { get; set; }
        public string RenLuck { get; set; }
        public string DiLuck { get; set; }
        public string WaiLuck { get; set; }
        public string ZongLuck { get; set; }
        public string Sancai { get; set; }
        public string SancaiLuck { get; set; }
        public string SancaiNote { get; set; }
    }

    public sealed class FlowWugeChar
    {
        public string Char { get; set; }
        public int Stroke { get; set; }
        public bool IsSurname { get; set; }
    }

    public sealed class DayunSegment
    {
        /// <summary>流年干支。</summary>
        public string Ganzhi { get; set; }
        /// <summary>該年所屬大運干支。</summary>
        public string DaYun { get; set; }
        public int StartYear { get; set; }
        public int EndYear { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public bool IsCurrent { get; set; }
        public string Summary { get; set; }
    }

    /// <summary>大運區間標註（哪幾年屬同一大運）。</summary>
    public sealed class DayunPeriodNote
    {
        public string Ganzhi { get; set; }
        public int StartYear { get; set; }
        public int EndYear { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public bool IsCurrent { get; set; }
    }

    public sealed class YearTrendPoint
    {
        public int Year { get; set; }
        public int Age { get; set; }
        public string Ganzhi { get; set; }
        public double TotalScore { get; set; }
        public double CareerScore { get; set; }
        public double WealthScore { get; set; }
        public double RelationshipScore { get; set; }
        public string Level { get; set; }
        public string Keyword { get; set; }
        public string DaYun { get; set; }
    }

    public sealed class YearAnalysisBlock
    {
        public int Year { get; set; }
        public int Age { get; set; }
        public string Ganzhi { get; set; }
        public string Animal { get; set; }
        public string DaYun { get; set; }
        public string Keyword { get; set; }
        public string Level { get; set; }
        public string Summary { get; set; }
        public double TotalScore { get; set; }
        public double CareerScore { get; set; }
        public double WealthScore { get; set; }
        public double RelationshipScore { get; set; }
        public string Overall { get; set; }
        public string Career { get; set; }
        public string Wealth { get; set; }
        public string Relationship { get; set; }
        public string Life { get; set; }
        public List<string> Suitable { get; set; }
        public List<string> Avoid { get; set; }
        public List<string> Advice { get; set; }
        public string Outlook { get; set; }
        public List<int> StrongMonths { get; set; }
        public List<int> StableMonths { get; set; }
        public List<int> AdjustMonths { get; set; }
        public List<int> CautionMonths { get; set; }
        public string MonthGuideCareer { get; set; }
        public string MonthGuideWealth { get; set; }
        public string MonthGuideRelationship { get; set; }
    }

    public sealed class MonthTrendSeries
    {
        public int Year { get; set; }
        public List<MonthTrendPoint> Points { get; set; }
    }

    public sealed class MonthTrendPoint
    {
        public int Month { get; set; }
        public string MonthGanzhi { get; set; }
        public double TotalScore { get; set; }
        public string Level { get; set; }
    }

    public sealed class MonthAnalysisBlock
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthGanzhi { get; set; }
        public string SolarTermRange { get; set; }
        public string Level { get; set; }
        public double TotalScore { get; set; }
        public string Overall { get; set; }
        public string Career { get; set; }
        public string Wealth { get; set; }
        public string Relationship { get; set; }
        public string Life { get; set; }
        public List<string> Suitable { get; set; }
        public List<string> Avoid { get; set; }
        public string Advice { get; set; }
        public string Summary { get; set; }
    }

    public sealed class FlowNarrative
    {
        public string Overview { get; set; }
        public string Phases { get; set; }
        public string Trend { get; set; }
        public string Actions { get; set; }
        public List<FlowActionBucket> ActionBuckets { get; set; }
        public string Poem { get; set; }
        public string PoemNote { get; set; }
    }

    public sealed class FlowActionBucket
    {
        public string Kind { get; set; } // do / dont
        public string Title { get; set; }
        public List<FlowActionItem> Items { get; set; }
    }

    public sealed class FlowActionItem
    {
        public string Domain { get; set; }
        public string Text { get; set; }
    }

    public static class FlowVisualizationMapper
    {
        public static FlowVisualizationData Map(
            AnalysisRequest req,
            AnalysisResult result,
            NameSuggestion sug,
            string logoPath = null)
        {
            if (req == null) throw new ArgumentNullException("req");
            if (result == null) throw new ArgumentNullException("result");
            if (sug == null) throw new ArgumentNullException("sug");

            var years = (result.Liunian ?? new List<YearLuck>()).OrderBy(y => y.Year).ToList();
            int startYear, endYear;
            req.ResolveLiunianRange(out startYear, out endYear);
            if (years.Count > 0)
            {
                startYear = years[0].Year;
                endYear = years[years.Count - 1].Year;
            }

            var pillars = result.Pillars;
            var genderLabel = req.Gender == "M" ? "男" : "女";
            var mode = (req.Mode ?? "newborn").Trim().ToLowerInvariant();
            var title = mode == "liunian" ? "流年大運分析"
                : mode == "rename" ? "專業改名剖象"
                : "新生兒命名剖象";
            var data = new FlowVisualizationData
            {
                ReportMode = mode,
                ReportTitle = title,
                ReportStartYear = startYear,
                ReportEndYear = endYear,
                GeneratedAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
                LogoPath = logoPath,
                Profile = BuildProfile(req, sug, pillars, genderLabel),
                YearlyTrend = years.Select(MapTrend).ToList(),
                YearlyAnalysis = years.Select(MapYearBlock).ToList(),
                MonthlyTrend = BuildMonthlyTrend(years),
                MonthlyAnalysis = BuildMonthlyAnalysis(years),
                DayunTimeline = BuildDayunTimeline(req, years),
                DayunPeriods = BuildDayunPeriods(req, years),
                Narrative = mode == "liunian" ? BuildNarrative(result) : new FlowNarrative(),
            };
            return data;
        }

        private static FlowProfile BuildProfile(
            AnalysisRequest req,
            NameSuggestion sug,
            Pillars pillars,
            string genderLabel)
        {
            var profile = new FlowProfile
            {
                FullName = sug.FullName ?? "",
                Surname = sug.Surname ?? "",
                GenderLabel = genderLabel,
                BirthDateText = req.Birth.ToString("yyyy/MM/dd"),
                BirthTimeText = req.Birth.ToString("HH:mm"),
                BirthPlace = req.BirthPlace ?? "",
                LunarText = pillars != null ? (pillars.LunarText ?? "") : "",
                YearPillar = pillars != null && pillars.Year != null ? pillars.Year.Ganzhi : "",
                MonthPillar = pillars != null && pillars.Month != null ? pillars.Month.Ganzhi : "",
                DayPillar = pillars != null && pillars.Day != null ? pillars.Day.Ganzhi : "",
                HourPillar = pillars != null && pillars.Hour != null ? pillars.Hour.Ganzhi : "",
                DayMaster = pillars != null ? (pillars.DayMaster ?? "") : "",
                DayMasterWuxing = pillars != null ? (pillars.DayMasterWuxing ?? "") : "",
                XiYongText = pillars != null && pillars.XiYong != null ? string.Join("、", pillars.XiYong) : "",
                JiShenText = pillars != null && pillars.JiShen != null ? string.Join("、", pillars.JiShen) : "",
                CharWuxingText = sug.CharWuxing != null ? string.Join("、", sug.CharWuxing) : "",
                TotalScore = sug.Total,
                Grade = sug.Grade ?? "",
            };
            if (sug.Wuge != null)
            {
                var w = sug.Wuge;
                profile.WugeSummary = string.Format(
                    "天{0}({1}·{2}) 人{3}({4}·{5}) 地{6}({7}·{8})；三才{9}（{10}）",
                    w.Tian, w.TianWx, w.TianLuck,
                    w.Ren, w.RenWx, w.RenLuck,
                    w.Di, w.DiWx, w.DiLuck,
                    w.Sancai, w.SancaiLuck);
                profile.Wuge = MapWugeVisual(w, sug);
            }
            return profile;
        }

        public static FlowWugeVisual MapWugeVisual(WugeResult w, NameSuggestion sug)
        {
            if (w == null) return null;
            return new FlowWugeVisual
            {
                Surname = w.Surname ?? (sug != null ? sug.Surname : "") ?? "",
                Given = w.Given ?? (sug != null ? sug.Given : "") ?? "",
                IsCompoundSurname = !string.IsNullOrEmpty(w.Surname) && w.Surname.Length >= 2,
                Chars = BuildWugeChars(w, sug),
                Tian = w.Tian,
                Ren = w.Ren,
                Di = w.Di,
                Wai = w.Wai,
                Zong = w.Zong,
                TianWx = w.TianWx ?? "",
                RenWx = w.RenWx ?? "",
                DiWx = w.DiWx ?? "",
                WaiWx = w.WaiWx ?? "",
                ZongWx = w.ZongWx ?? "",
                TianLuck = w.TianLuck ?? "",
                RenLuck = w.RenLuck ?? "",
                DiLuck = w.DiLuck ?? "",
                WaiLuck = w.WaiLuck ?? "",
                ZongLuck = w.ZongLuck ?? "",
                Sancai = w.Sancai ?? "",
                SancaiLuck = w.SancaiLuck ?? "",
                SancaiNote = w.SancaiNote ?? "",
            };
        }

        private static List<FlowWugeChar> BuildWugeChars(WugeResult w, NameSuggestion sug)
        {
            var list = new List<FlowWugeChar>();
            var surname = (w != null && !string.IsNullOrEmpty(w.Surname))
                ? w.Surname
                : (sug != null ? sug.Surname ?? "" : "");
            var surLen = surname.Length;
            if (w != null && w.CharStrokes != null && w.CharStrokes.Count > 0)
            {
                for (var i = 0; i < w.CharStrokes.Count; i++)
                {
                    var cs = w.CharStrokes[i];
                    list.Add(new FlowWugeChar
                    {
                        Char = cs.Char ?? "",
                        Stroke = cs.Stroke,
                        IsSurname = i < surLen,
                    });
                }
                return list;
            }

            var full = ((sug != null ? sug.FullName : null) ?? "").ToCharArray();
            for (var i = 0; i < full.Length; i++)
            {
                list.Add(new FlowWugeChar
                {
                    Char = full[i].ToString(),
                    Stroke = 0,
                    IsSurname = i < surLen,
                });
            }
            return list;
        }

        private static YearTrendPoint MapTrend(YearLuck y)
        {
            return new YearTrendPoint
            {
                Year = y.Year,
                Age = y.Age,
                Ganzhi = y.Ganzhi ?? "",
                TotalScore = y.TotalScore,
                CareerScore = y.CareerScore,
                WealthScore = y.WealthScore,
                RelationshipScore = y.RelationshipScore,
                Level = y.Level ?? "",
                Keyword = y.Keyword ?? "",
                DaYun = y.DaYun ?? "",
            };
        }

        private static YearAnalysisBlock MapYearBlock(YearLuck y)
        {
            return new YearAnalysisBlock
            {
                Year = y.Year,
                Age = y.Age,
                Ganzhi = y.Ganzhi ?? "",
                Animal = y.Animal ?? "",
                DaYun = y.DaYun ?? "",
                Keyword = y.Keyword ?? "",
                Level = y.Level ?? "",
                Summary = y.Summary ?? "",
                TotalScore = y.TotalScore,
                CareerScore = y.CareerScore,
                WealthScore = y.WealthScore,
                RelationshipScore = y.RelationshipScore,
                Overall = y.Overall ?? "",
                Career = y.Career ?? "",
                Wealth = y.Wealth ?? "",
                Relationship = y.Relationship ?? "",
                Life = y.Life ?? "",
                Suitable = y.Suitable ?? new List<string>(),
                Avoid = y.Avoid ?? new List<string>(),
                Advice = y.Advice ?? new List<string>(),
                Outlook = y.Outlook ?? "",
                StrongMonths = y.StrongMonths ?? new List<int>(),
                StableMonths = y.StableMonths ?? new List<int>(),
                AdjustMonths = y.AdjustMonths ?? new List<int>(),
                CautionMonths = y.CautionMonths ?? new List<int>(),
                MonthGuideCareer = y.MonthGuideCareer ?? "",
                MonthGuideWealth = y.MonthGuideWealth ?? "",
                MonthGuideRelationship = y.MonthGuideRelationship ?? "",
            };
        }

        private static List<MonthTrendSeries> BuildMonthlyTrend(List<YearLuck> years)
        {
            var list = new List<MonthTrendSeries>();
            foreach (var y in years)
            {
                if (y.Months == null || y.Months.Count == 0) continue;
                list.Add(new MonthTrendSeries
                {
                    Year = y.Year,
                    Points = y.Months.OrderBy(m => m.Month).Select(m => new MonthTrendPoint
                    {
                        Month = m.Month,
                        MonthGanzhi = m.MonthGanzhi ?? "",
                        TotalScore = m.TotalScore,
                        Level = m.Level ?? "",
                    }).ToList(),
                });
            }
            return list;
        }

        private static List<MonthAnalysisBlock> BuildMonthlyAnalysis(List<YearLuck> years)
        {
            var list = new List<MonthAnalysisBlock>();
            foreach (var y in years)
            {
                if (y.Months == null) continue;
                foreach (var m in y.Months.OrderBy(x => x.Month))
                {
                    list.Add(new MonthAnalysisBlock
                    {
                        Year = m.Year > 0 ? m.Year : y.Year,
                        Month = m.Month,
                        MonthGanzhi = m.MonthGanzhi ?? "",
                        SolarTermRange = m.SolarTermRange ?? "",
                        Level = m.Level ?? "",
                        TotalScore = m.TotalScore,
                        Overall = m.Overall ?? "",
                        Career = m.Career ?? "",
                        Wealth = m.Wealth ?? "",
                        Relationship = m.Relationship ?? "",
                        Life = m.Life ?? "",
                        Suitable = m.Suitable ?? new List<string>(),
                        Avoid = m.Avoid ?? new List<string>(),
                        Advice = m.Advice ?? "",
                        Summary = m.Summary ?? "",
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// 全模式統一：一年一格。大運區間另見 DayunPeriods 標註。
        /// </summary>
        private static List<DayunSegment> BuildDayunTimeline(AnalysisRequest req, List<YearLuck> years)
        {
            return BuildYearlyTimeline(years, req.Birth.Year);
        }

        /// <summary>一年一段；標題用流年干支，並帶所屬大運。</summary>
        private static List<DayunSegment> BuildYearlyTimeline(List<YearLuck> years, int birthYear)
        {
            var list = new List<DayunSegment>();
            if (years == null || years.Count == 0) return list;
            var nowYear = DateTime.Now.Year;
            foreach (var y in years.OrderBy(x => x.Year))
            {
                var age = y.Age > 0 ? y.Age : Math.Max(1, y.Year - birthYear + 1);
                list.Add(new DayunSegment
                {
                    Ganzhi = y.Ganzhi ?? "",
                    DaYun = y.DaYun ?? "",
                    StartYear = y.Year,
                    EndYear = y.Year,
                    StartAge = age,
                    EndAge = age,
                    Summary = y.Summary ?? "",
                    IsCurrent = y.Year == nowYear,
                });
            }
            return list;
        }

        /// <summary>依大運干支合併，標註報告期間內哪幾年屬同一大運。</summary>
        private static List<DayunPeriodNote> BuildDayunPeriods(AnalysisRequest req, List<YearLuck> years)
        {
            var notes = new List<DayunPeriodNote>();
            if (years == null || years.Count == 0) return notes;

            var birth = req.Birth;
            var nowYear = DateTime.Now.Year;
            int reportStart = years.Min(y => y.Year);
            int reportEnd = years.Max(y => y.Year);

            // 優先用八字大運表，範圍含報告前後以便切出完整大運段再裁切
            var entries = PillarCalculator.DaYunList(
                birth, req.Gender ?? "M", birth.Year, Math.Max(reportEnd + 9, birth.Year + 89));
            if (entries != null && entries.Count > 0)
            {
                string currentGz = null;
                DayunPeriodNote open = null;
                foreach (var e in entries.OrderBy(x => x.Year))
                {
                    var gz = e.DaYun ?? "";
                    if (open == null || !string.Equals(currentGz, gz, StringComparison.Ordinal))
                    {
                        if (open != null) notes.Add(open);
                        currentGz = gz;
                        open = new DayunPeriodNote
                        {
                            Ganzhi = gz,
                            StartYear = e.Year,
                            EndYear = e.Year,
                            StartAge = Math.Max(1, e.Year - birth.Year + 1),
                            EndAge = Math.Max(1, e.Year - birth.Year + 1),
                        };
                    }
                    else
                    {
                        open.EndYear = e.Year;
                        open.EndAge = Math.Max(1, e.Year - birth.Year + 1);
                    }
                }
                if (open != null) notes.Add(open);
            }
            else
            {
                // fallback：用各年 YearLuck.DaYun 合併
                DayunPeriodNote open = null;
                string current = null;
                foreach (var y in years.OrderBy(x => x.Year))
                {
                    var gz = y.DaYun ?? "";
                    var age = y.Age > 0 ? y.Age : Math.Max(1, y.Year - birth.Year + 1);
                    if (open == null || !string.Equals(current, gz, StringComparison.Ordinal))
                    {
                        if (open != null) notes.Add(open);
                        current = gz;
                        open = new DayunPeriodNote
                        {
                            Ganzhi = gz,
                            StartYear = y.Year,
                            EndYear = y.Year,
                            StartAge = age,
                            EndAge = age,
                        };
                    }
                    else
                    {
                        open.EndYear = y.Year;
                        open.EndAge = age;
                    }
                }
                if (open != null) notes.Add(open);
            }

            // 只保留與報告流年重疊的大運；顯示時裁切起迄到報告範圍內較易讀
            var clipped = new List<DayunPeriodNote>();
            foreach (var n in notes.Where(s => s.EndYear >= reportStart && s.StartYear <= reportEnd))
            {
                var start = Math.Max(n.StartYear, reportStart);
                var end = Math.Min(n.EndYear, reportEnd);
                clipped.Add(new DayunPeriodNote
                {
                    Ganzhi = n.Ganzhi,
                    StartYear = start,
                    EndYear = end,
                    StartAge = Math.Max(1, start - birth.Year + 1),
                    EndAge = Math.Max(1, end - birth.Year + 1),
                    IsCurrent = nowYear >= n.StartYear && nowYear <= n.EndYear,
                });
            }
            return clipped;
        }

        private static List<DayunSegment> GroupFromYearLuck(List<YearLuck> years, int birthYear)
        {
            return BuildYearlyTimeline(years, birthYear);
        }

        private static FlowNarrative BuildNarrative(AnalysisResult result)
        {
            var n = new FlowNarrative();
            if (result.Destiny == null) return n;
            n.Overview = DestinyText(result.Destiny, "liunian_overview");
            n.Phases = DestinyText(result.Destiny, "liunian_phases");
            n.Trend = DestinyText(result.Destiny, "liunian_trend");
            n.Actions = DestinyText(result.Destiny, "liunian_actions");
            n.ActionBuckets = ParseActionBuckets(n.Actions);
            return n;
        }

        /// <summary>
        /// 解析 FormatActions 文字：財務・該做：／事業・不該做： 等區塊。
        /// </summary>
        private static List<FlowActionBucket> ParseActionBuckets(string actionsText)
        {
            var financeDos = NewBucket("do", "財務該做");
            var financeDonts = NewBucket("dont", "財務不該做");
            var careerDos = NewBucket("do", "事業該做");
            var careerDonts = NewBucket("dont", "事業不該做");
            var buckets = new List<FlowActionBucket> { financeDos, financeDonts, careerDos, careerDonts };

            if (string.IsNullOrWhiteSpace(actionsText))
                return buckets;

            FlowActionBucket current = null;
            foreach (var raw in actionsText.Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.IndexOf("該做", StringComparison.Ordinal) >= 0
                    || line.IndexOf("不該做", StringComparison.Ordinal) >= 0
                    || line.IndexOf("Don't", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("Do's", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("Dos", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var isDont = line.IndexOf("不該做", StringComparison.Ordinal) >= 0
                        || line.IndexOf("Don't", StringComparison.OrdinalIgnoreCase) >= 0
                        || line.IndexOf("Dont", StringComparison.OrdinalIgnoreCase) >= 0;
                    var isFinance = line.IndexOf("財務", StringComparison.Ordinal) >= 0
                        || line.IndexOf("財運", StringComparison.Ordinal) >= 0;
                    var isCareer = line.IndexOf("事業", StringComparison.Ordinal) >= 0
                        || line.IndexOf("職涯", StringComparison.Ordinal) >= 0;

                    if (isFinance && isDont) current = financeDonts;
                    else if (isFinance) current = financeDos;
                    else if (isCareer && isDont) current = careerDonts;
                    else if (isCareer) current = careerDos;
                    else current = isDont ? financeDonts : financeDos; // 未標領域時先歸財務
                    continue;
                }

                var item = line.TrimStart('・', '•', '-', '－', '–', '*', ' ');
                if (string.IsNullOrWhiteSpace(item)) continue;
                if (current == null) current = financeDos;
                current.Items.Add(new FlowActionItem { Domain = "", Text = item });
            }
            return buckets;
        }

        private static FlowActionBucket NewBucket(string kind, string title)
        {
            return new FlowActionBucket
            {
                Kind = kind,
                Title = title,
                Items = new List<FlowActionItem>(),
            };
        }

        private static string DestinyText(Dictionary<string, object> destiny, string key)
        {
            object value;
            if (!destiny.TryGetValue(key, out value) || value == null) return "";
            return value.ToString() ?? "";
        }
    }
}
