using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Bazi;
using Mingxu.Core.Constants;
using Mingxu.Core.Liunian;
using Mingxu.Core.Llm;
using Mingxu.Core.Models;
using Mingxu.Core.Parents;
using Mingxu.Core.Rules;
using Mingxu.Core.Scoring;

namespace Mingxu.Core.Pipeline
{

internal sealed class NameParts
{
    public string Surname { get; set; }
    public string Given { get; set; }

    public NameParts(string surname, string given)
    {
        Surname = surname;
        Given = given;
    }
}

public static class NamingService
{
    private static readonly Dictionary<string, double> PlaceLon = new Dictionary<string, double>()
    {
        ["台北市"] = 121.56, ["新北市"] = 121.46, ["基隆市"] = 121.74, ["桃園市"] = 121.30,
        ["新竹市"] = 120.97, ["新竹縣"] = 121.18, ["苗栗縣"] = 120.82, ["台中市"] = 120.67,
        ["彰化縣"] = 120.54, ["南投縣"] = 120.69, ["雲林縣"] = 120.53, ["嘉義市"] = 120.45,
        ["嘉義縣"] = 120.30, ["台南市"] = 120.21, ["高雄市"] = 120.31, ["屏東縣"] = 120.49,
        ["宜蘭縣"] = 121.75, ["花蓮縣"] = 121.61, ["台東縣"] = 121.15, ["澎湖縣"] = 119.57,
        ["金門縣"] = 118.32, ["連江縣（馬祖）"] = 119.93,
    };

    public static AnalysisResult Run(AnalysisRequest req, ICharacterRepository repo)
    {
        Validate(req);
        if (req.Mode == "rename")
        {
            req.ExcludeHot5y = true;
            req.ExcludeClassicHot = true;
        }
        double lon;
        if (req.Longitude <= 0 && PlaceLon.TryGetValue(req.BirthPlace, out lon))
            req.Longitude = lon;

        var pillars = PillarCalculator.Compute(
            req.Birth, req.Gender, req.UseTrueSolar, req.Longitude, req.BirthPlace);

        var result = new AnalysisResult
        {
            Pillars = pillars,
            HumanReadableDestiny = BuildHumanDestiny(pillars),
            Destiny = BuildDestinyDict(pillars),
        };

        if (req.Mode == "liunian")
        {
            var name = ResolveFullName(req, repo);
            var sug = NameScorer.Analyze(name.Surname, name.Given, pillars, repo, BuildScoreOptions(req, false));
            result.Current = sug;
            result.Suggestions = new List<NameSuggestion> { sug };
            int startYear, endYear;
            req.ResolveLiunianRange(out startYear, out endYear);
            var detailed = string.Equals(req.LiunianMonthDetail, "detailed", StringComparison.OrdinalIgnoreCase);
            var localYears = LiunianEngine.Compute(
                pillars, sug, req.Birth, req.Gender,
                startYear, endYear,
                req.IncludeLiunianMonths,
                detailed);
            result.Liunian = localYears;
            result.Destiny["liunian_year_table"] = LiunianEngine.FormatYearOverviewTable(localYears);
            result.Destiny["liunian_range"] = startYear + "～" + endYear;

            if (!string.IsNullOrWhiteSpace(req.ChatGptApiKey))
            {
                var llm = ChatGptLiunianGenerator.Enrich(req, sug, pillars, localYears);
                if (llm.Ok)
                {
                    result.Liunian = MergeLiunianPreserveMonths(localYears, llm.Liunian);
                    if (!string.IsNullOrWhiteSpace(llm.Overview))
                        result.Destiny["liunian_overview"] = llm.Overview;
                    if (!string.IsNullOrWhiteSpace(llm.PhasesText))
                        result.Destiny["liunian_phases"] = llm.PhasesText;
                    if (!string.IsNullOrWhiteSpace(llm.TrendText))
                        result.Destiny["liunian_trend"] = llm.TrendText;
                    if (!string.IsNullOrWhiteSpace(llm.ActionsText))
                        result.Destiny["liunian_actions"] = llm.ActionsText;
                    result.Destiny["pipeline_notes"] = new List<string>
                    {
                        "流年解說：ChatGPT（文字）；分數／干支／流月：本地",
                        "期間 " + startYear + "～" + endYear + "（" + result.Liunian.Count + " 年）",
                        req.IncludeLiunianMonths ? "含流月分析" : "不含流月"
                    };
                }
                else
                {
                    result.Destiny["pipeline_notes"] = new List<string>
                    {
                        "流年解說改用本地（" + (llm.Error ?? "API 失敗") + "）",
                        "期間 " + startYear + "～" + endYear
                    };
                }
            }
            else
            {
                result.Destiny["pipeline_notes"] = new List<string>
                {
                    "流年解說：本地（未設定 API Key）",
                    "期間 " + startYear + "～" + endYear +
                        (req.IncludeLiunianMonths ? "；含流月" : "")
                };
            }
            return result;
        }

        string surname;
        string currentGiven = null;
        if (req.Mode == "rename")
        {
            var name = ResolveFullName(req, repo);
            surname = name.Surname;
            currentGiven = name.Given;
            var currentOptions = BuildScoreOptions(req, false);
            currentOptions.AvoidParentChars = false;
            result.Current = NameScorer.Analyze(surname, currentGiven, pillars, repo, currentOptions);
            var diagnosis = AdultRename.DiagnoseCurrentName(result.Current, req.RenameReason);
            result.Destiny["current_diagnosis"] = diagnosis;
            result.Destiny["current_eval"] = diagnosis.ReportText;
            var improveDirsEarly = AdultRename.ParseImproveDirections(req.ImproveDirections);
            result.Destiny["rename_directions"] = AdultRename.FormatRenameDirections(
                req.RenameReason, improveDirsEarly, req.RenameFocus);
            result.Destiny["rename_notes"] = new List<string>
            {
                string.IsNullOrWhiteSpace(req.RenameReason) ? "未指定改名原因" : "改名原因：" + req.RenameReason,
                string.IsNullOrWhiteSpace(req.ImproveDirections) ? "未指定改善方向" : "改善方向：" + req.ImproveDirections,
                string.IsNullOrWhiteSpace(req.RenameFocus) ? "未指定生活關注" : "生活關注：" + req.RenameFocus
            };
        }
        else
        {
            surname = (req.Surname ?? "").Trim().Replace(" ", "").Replace("　", "");
            if (surname.Length == 0 && !string.IsNullOrWhiteSpace(req.CurrentFullName))
                surname = ResolveFullName(req, repo).Surname;
        }

        var parentTaboo = req.AvoidParentChars
            ? ParentTaboo.Extract(req.FatherName, req.MotherName, repo.CompoundSurnames())
            : new HashSet<char>();
        var forbiddenTokens = NameParse.ParseForbidden(req.ForbiddenChars);
        var requestedPreferred = ParseChars(req.PreferredChars).Select(c => c.ToString()).ToList();
        var preferredNotes = XiyongChars.ValidatePreferred(requestedPreferred, repo);
        var preferred = XiyongChars.ValidPreferred(requestedPreferred, repo);
        var zibei = ParseChars(req.Zibei).Select(c => c.ToString()).FirstOrDefault() ?? "";

        var reqLike = new AnalysisRequestLike
        {
            Mode = req.Mode,
            NamingMode = req.NamingMode,
            ExplorationLevel = req.ExplorationLevel,
            Zibei = req.Zibei,
            PreferredChars = req.PreferredChars,
            ForbiddenChars = req.ForbiddenChars,
            AllowSingle = req.AllowSingle,
            AllowDouble = req.AllowDouble,
        };
        var cfg = Exploration.ConfigFor(reqLike);
        var seed = req.CandidateSeed != 0 ? req.CandidateSeed : Environment.TickCount;

        List<string> givens;
        var poolNotes = new List<string>();
        List<CharacterInfo> rankedAll = new List<CharacterInfo>();
        var useLlm = req.UseChatGptGivens && req.Mode != "liunian";

        if (useLlm)
        {
            var want = Math.Max(req.ResultCount * 2, 36);
            var llm = ChatGptNameGenerator.Generate(req, pillars, surname, want);
            if (!string.IsNullOrEmpty(llm.Error) && llm.Givens.Count == 0)
                throw new InvalidOperationException(llm.Error);
            givens = llm.Givens;
            poolNotes.Add("組名來源：ChatGPT（審美優先）");
            if (!string.IsNullOrEmpty(llm.RawNote))
                poolNotes.Add("模型備註：" + llm.RawNote);
            if (!string.IsNullOrEmpty(llm.Error))
                poolNotes.Add("警告：" + llm.Error);
            // 字輩後處理：若模型漏放字輩，嘗試輕修正或剔除
            if (!string.IsNullOrEmpty(zibei))
            {
                givens = ApplyZibeiSoft(givens, zibei, req.ZibeiPosition, req.AllowSingle, req.AllowDouble);
                poolNotes.Add("已套用字輩軟約束：" + zibei);
            }
            if (preferred.Count > 0)
                givens = XiyongChars.FilterGivensByPreferred(givens, preferred);
        }
        else
        {
            var poolResult = CandidatePoolBuilder.Build(repo, pillars, parentTaboo, cfg,
                surname, req.Gender, req.NamingMode, forbiddenTokens, preferred, zibei, seed);
            rankedAll = poolResult.RankedAll;
            poolNotes.AddRange(poolResult.Notes);
            givens = CandidatePoolBuilder.BuildGivens(
                poolResult.Chars,
                req.AllowSingle,
                req.AllowDouble,
                zibei,
                req.ZibeiPosition,
                seed,
                maxCombos: 22000);
            givens = XiyongChars.FilterGivensByPreferred(givens, preferred);
        }

        // 有字輩＋喜用字：強制產生雙名組合，並保證最終至少一組
        var forcedGivens = XiyongChars.BuildForcedZibeiPreferredGivens(
            zibei, preferred, req.ZibeiPosition, req.AllowDouble);
        var forcedSet = new HashSet<string>(forcedGivens);
        if (forcedGivens.Count > 0)
        {
            var rest = givens.Where(g => !forcedSet.Contains(g)).ToList();
            givens = forcedGivens.Concat(rest).ToList();
            poolNotes.Add("強制字輩＋喜用字組合：" + string.Join("、", forcedGivens));
        }
        else if (!string.IsNullOrEmpty(zibei) && preferred.Count > 0 && !req.AllowDouble)
        {
            poolNotes.Add("已填字輩＋喜用字，但未開雙名，無法強制組合（請開啟雙名）");
        }

        var preferredSet = new HashSet<string>(preferred);
        if (preferredSet.Count > 0)
        {
            givens = givens
                .OrderByDescending(g => forcedSet.Contains(g))
                .ThenByDescending(g => preferredSet.Count(p => g.Contains(p)))
                .ThenBy(g => g.Length)
                .ToList();
        }

        var scored = new List<NameSuggestion>();
        var quickReject = 0;
        var hardReject = 0;
        foreach (var given in givens)
        {
            var isForcedCombo = forcedSet.Contains(given)
                || XiyongChars.ContainsZibeiAndPreferred(given, zibei, preferred);

            if (given.Any(parentTaboo.Contains)) { hardReject++; continue; }
            if (currentGiven != null && given == currentGiven) continue;
            var blocked = Blacklist.IsGloballyBlocked(surname, given, repo, forbiddenTokens);
            if (!string.IsNullOrEmpty(blocked)) { hardReject++; continue; }

            if (!useLlm)
            {
                // 強制組合不因性別硬刪（否則男＋怡永遠無法出「孟怡」）
                if (!isForcedCombo)
                {
                    var mismatch = false;
                    foreach (var ch in given)
                    {
                        CharacterInfo info;
                        repo.TryGet(ch.ToString(), out info);
                        if (GenderChars.GenderMismatch(ch.ToString(), req.Gender, zibei,
                            info == null ? "" : info.Meaning))
                        { mismatch = true; break; }
                    }
                    if (mismatch) { hardReject++; continue; }
                }
                if (given.Any(ch => !repo.TryStroke(ch.ToString()).HasValue)) { hardReject++; continue; }

                if (!isForcedCombo)
                {
                    var wx = given.Select(c => repo.WuxingOf(c.ToString())).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    if (wx.Count > 0)
                    {
                        var hitJi = wx.Count(w => pillars.JiShen.Contains(w));
                        var hitXi = wx.Count(w => pillars.XiYong.Contains(w) || pillars.XiCi.Contains(w));
                        if (hitJi >= given.Length && hitXi == 0) { quickReject++; continue; }
                    }
                }
            }
            else
            {
                // ChatGPT 模式：缺康熙筆畫的字仍嘗試評分（五格可能弱），不因性別標籤硬刪
                if (given.Any(ch => !repo.TryStroke(ch.ToString()).HasValue))
                    poolNotes.Add("字庫缺筆畫（仍保留）：" + given);
            }

            try
            {
                var sug = NameScorer.Analyze(surname, given, pillars, repo, BuildScoreOptions(req, true));
                if (sug == null) { hardReject++; continue; }
                if (!useLlm)
                {
                    if (!isForcedCombo)
                    {
                        if (sug.Wuge != null && sug.Wuge.SancaiLuck == "大凶") { hardReject++; continue; }
                        if (sug.WuxingScore < 38 || sug.PhonologyScore < 34) { quickReject++; continue; }
                    }
                    else if (sug.PhonologyScore < 15)
                    {
                        // 強制組合僅擋極端不雅音韻
                        quickReject++;
                        continue;
                    }
                }
                else
                {
                    // 僅剔除極端不雅音韻；命理分數只影響排序
                    if (sug.PhonologyScore < 20 && !isForcedCombo) { quickReject++; continue; }
                }
                scored.Add(sug);
            }
            catch
            {
                hardReject++;
            }

            if (!useLlm && scored.Count >= FinalRanking.FullScorePool * 3
                && forcedSet.All(f => scored.Any(s => s.Given == f)))
                break;
        }

        var improveDirections = AdultRename.ParseImproveDirections(req.ImproveDirections);
        if (req.Mode == "rename" && result.Current != null)
        {
            foreach (var suggestion in scored)
                suggestion.RankBonus = AdultRename.AdultRankBonus(suggestion, result.Current, improveDirections);
            scored = scored.OrderByDescending(s => s.Total + s.RankBonus)
                .ThenByDescending(s => s.Total).ToList();
        }

        var topN = Math.Max(5, req.ResultCount);
        var firstMax = !string.IsNullOrEmpty(zibei) && req.ZibeiPosition == 0 ? 8 : 3;
        var secondMax = !string.IsNullOrEmpty(zibei) && req.ZibeiPosition == 1 ? 8 : 3;
        var rankResult = FinalRanking.Rank(
            scored,
            topN,
            true,
            firstMax,
            secondMax,
            5,
            preferred);
        var finalList = rankResult.Selected;
        var rankNotes = rankResult.Notes;
        if (!useLlm)
        {
            var synergy = ComboSynergy.PickComboSynergy(scored, rankedAll, Math.Max(4, topN / 5));
            finalList = ComboSynergy.MergeComboExploreIntoTop(finalList, synergy, topN, rankNotes);
        }
        else
        {
            // ChatGPT 結果：評分後嚴格由高到低
            finalList = scored
                .OrderByDescending(s => s.Total + s.RankBonus)
                .ThenByDescending(s => s.Total)
                .ThenByDescending(s => s.AestheticScore)
                .Take(topN)
                .ToList();
            rankNotes.Add("最終排序：ChatGPT 候選依綜合評分由高到低");
        }

        finalList = EnsureZibeiPreferredInResults(
            finalList, scored, zibei, preferred, topN, rankNotes);

        result.Suggestions = finalList;
        var pipeNotes = new List<string>(poolNotes);
        pipeNotes.AddRange(preferredNotes);
        pipeNotes.AddRange(rankNotes);
        pipeNotes.Add(string.Format("組合 {0} → 快速拒 {1} → Hard拒 {2} → 評分 {3} → 最終 {4}",
            givens.Count, quickReject, hardReject, scored.Count, finalList.Count));
        result.Destiny["pipeline_notes"] = pipeNotes;
        result.Destiny["category_lists"] = Content.CategoryLists.Build(scored);
        if (finalList.Count > 0)
        {
            var parents = string.Join("、", new[] { req.FatherName, req.MotherName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            result.Destiny["content_pack"] = Content.ContentPackBuilder.Build(
                finalList[0], pillars, parents, req.Mode);
        }

        if (req.Mode == "rename" && result.Current != null)
        {
            foreach (var suggestion in result.Suggestions)
            {
                var cmp = AdultRename.BuildRenameComparison(result.Current, suggestion, improveDirections);
                suggestion.RenameComparison = cmp;
                suggestion.ComparisonText = AdultRename.FormatComparisonReport(cmp, result.Current, suggestion);
            }
        }

        if (result.Suggestions.Count > 0)
        {
            var childDomains = string.Equals(req.Mode, "newborn", StringComparison.OrdinalIgnoreCase);
            result.Liunian = ComputeLiunian(
                pillars, result.Suggestions[0], req.Birth, req.Gender,
                Math.Min(10, req.LiunianYears), repo, childDomains);
        }

        return result;
    }

    /// <summary>
    /// 有字輩＋喜用字時，保證最終列表至少一組同時含兩者；插入最優強制組合並置頂。
    /// </summary>
    private static List<NameSuggestion> EnsureZibeiPreferredInResults(
        List<NameSuggestion> finalList,
        List<NameSuggestion> scored,
        string zibei,
        List<string> preferred,
        int topN,
        List<string> notes)
    {
        if (string.IsNullOrEmpty(zibei) || preferred == null || preferred.Count == 0)
            return finalList ?? new List<NameSuggestion>();

        var list = finalList ?? new List<NameSuggestion>();
        if (list.Any(s => XiyongChars.ContainsZibeiAndPreferred(s.Given, zibei, preferred)))
        {
            notes.Add("已含字輩＋喜用字組合（強制保障通過）");
            return list;
        }

        var best = scored
            .Where(s => XiyongChars.ContainsZibeiAndPreferred(s.Given, zibei, preferred))
            .OrderByDescending(s => s.Total + s.RankBonus)
            .ThenByDescending(s => s.Total)
            .FirstOrDefault();
        if (best == null)
        {
            notes.Add("無法強制輸出字輩＋喜用字（候選被黑名單／缺筆畫／父母避字擋下）");
            return list;
        }

        list = list.Where(s => s.FullName != best.FullName).ToList();
        list.Insert(0, best);
        if (list.Count > topN)
            list = list.Take(topN).ToList();
        notes.Add("已強制置頂字輩＋喜用字：" + best.FullName);
        return list;
    }

    private static List<string> ApplyZibeiSoft(
        IEnumerable<string> givens, string zibei, int position, bool allowSingle, bool allowDouble)
    {
        var zb = (zibei ?? "").Trim();
        if (zb.Length == 0) return givens.ToList();
        var zc = zb[0];
        var list = new List<string>();
        var seen = new HashSet<string>();
        foreach (var g0 in givens)
        {
            var g = g0 ?? "";
            if (g.Length == 0) continue;
            if (position == 0)
            {
                if (g[0] != zc)
                {
                    if (g.Length == 1 && allowDouble) g = zc + g;
                    else if (g.Length >= 1) g = zc + (g.Length >= 2 ? g.Substring(1, 1) : g);
                }
            }
            else
            {
                if (g[g.Length - 1] != zc)
                {
                    if (g.Length == 1 && allowDouble) g = g + zc;
                    else if (g.Length >= 2) g = g.Substring(0, 1) + zc;
                }
            }
            if (g.Length == 1 && !allowSingle) continue;
            if (g.Length == 2 && !allowDouble) continue;
            if (g.Length < 1 || g.Length > 2) continue;
            if (seen.Add(g)) list.Add(g);
        }
        return list;
    }

    private static ScoreOptions BuildScoreOptions(AnalysisRequest req, bool candidate)
    {
        return new ScoreOptions
        {
            NamingMode = req.NamingMode,
            Gender = req.Gender,
            RefYear = DateTime.Now.Year,
            FatherName = req.FatherName,
            MotherName = req.MotherName,
            AvoidParentChars = candidate && req.AvoidParentChars,
            ApplyParentWeight = candidate,
            ParentWeight = req.Mode == "rename" ? 0.10 : 0.15
            ,ExcludeHot5y = req.ExcludeHot5y
            ,ExcludeClassicHot = req.ExcludeClassicHot
            ,HotYears = Math.Max(1, req.HotYears)
            ,HotRankLimit = Math.Max(1, req.HotRankLimit)
        };
    }

    private static HashSet<char> ParseChars(string raw)
    {
        return new HashSet<char>((raw ?? "").Where(c => !char.IsWhiteSpace(c) && c != ',' && c != '，' && c != '、'));
    }

    public static List<YearLuck> ComputeLiunian(
        Pillars pillars, NameSuggestion sug, DateTime birth, string gender, int years, ICharacterRepository repo,
        bool childDomains = false)
    {
        return LiunianEngine.Compute(pillars, sug, birth, gender, years, includeMonths: false, childDomains: childDomains);
    }

    public static List<YearLuck> ComputeLiunianRange(
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
        return LiunianEngine.Compute(pillars, sug, birth, gender, startYear, endYear, includeMonths, detailedMonths, childDomains);
    }

    /// <summary>ChatGPT 只覆蓋文字欄位；本地分數／流月骨架必須保留。</summary>
    private static List<YearLuck> MergeLiunianPreserveMonths(List<YearLuck> local, List<YearLuck> llm)
    {
        if (local == null) return llm ?? new List<YearLuck>();
        if (llm == null || llm.Count == 0) return local;
        var byYear = llm.GroupBy(x => x.Year).ToDictionary(g => g.Key, g => g.First());
        foreach (var y in local)
        {
            YearLuck rich;
            if (!byYear.TryGetValue(y.Year, out rich) || rich == null) continue;
            if (!string.IsNullOrWhiteSpace(rich.Summary)) y.Summary = rich.Summary;
            if (!string.IsNullOrWhiteSpace(rich.Level)) y.Level = rich.Level;
            if (!string.IsNullOrWhiteSpace(rich.Outlook)) y.Outlook = rich.Outlook;
            if (!string.IsNullOrWhiteSpace(rich.Overall)) y.Overall = rich.Overall;
            if (!string.IsNullOrWhiteSpace(rich.Career)) y.Career = rich.Career;
            if (!string.IsNullOrWhiteSpace(rich.Wealth)) y.Wealth = rich.Wealth;
            if (!string.IsNullOrWhiteSpace(rich.Relationship)) y.Relationship = rich.Relationship;
            if (!string.IsNullOrWhiteSpace(rich.Life)) y.Life = rich.Life;
            if (!string.IsNullOrWhiteSpace(rich.Keyword)) y.Keyword = rich.Keyword;
            if (rich.Suitable != null && rich.Suitable.Count > 0) y.Suitable = rich.Suitable;
            if (rich.Avoid != null && rich.Avoid.Count > 0) y.Avoid = rich.Avoid;
            if (rich.Advice != null && rich.Advice.Count > 0) y.Advice = rich.Advice;
            if (rich.Risks != null && rich.Risks.Count > 0) y.Risks = rich.Risks;
            // Months / scores / ganzhi 一律沿用本地
        }
        return local;
    }

    /// <summary>
    /// 解析全名：姓氏欄優先（複姓可手填），否則依複姓表自動辨識。
    /// </summary>
    private static ParsedName ResolveFullName(AnalysisRequest req, ICharacterRepository repo)
    {
        return NameParse.SplitFullName(
            req.CurrentFullName,
            repo.CompoundSurnames(),
            req.Surname);
    }

    private static void Validate(AnalysisRequest req)
    {
        if (req.Mode != "newborn" && req.Mode != "rename" && req.Mode != "liunian")
            throw new ArgumentException("服務模式須為新生兒命名、專業改名或流年分析");
        if (req.Mode == "liunian" && string.IsNullOrWhiteSpace(req.CurrentFullName))
            throw new ArgumentException("流年分析請填姓名");
        if (req.Mode == "liunian")
        {
            int start, end;
            req.ResolveLiunianRange(out start, out end);
            if (end - start + 1 > 50)
                throw new ArgumentException("流年分析期間最多 50 年");
            if (end < start)
                throw new ArgumentException("結束年份不可早於起始年份");
        }
        if (req.Mode == "rename" && string.IsNullOrWhiteSpace(req.CurrentFullName))
            throw new ArgumentException("專業改名請填原姓名");
        if (req.Mode == "newborn" && string.IsNullOrWhiteSpace(req.Surname) && string.IsNullOrWhiteSpace(req.CurrentFullName))
            throw new ArgumentException("請輸入姓氏");
        if (!req.AllowSingle && !req.AllowDouble)
            throw new ArgumentException("請至少允許單名或雙名其中一種");
    }

    private static string BuildHumanDestiny(Pillars p) =>
        $"四柱：{p.Year.Ganzhi}　{p.Month.Ganzhi}　{p.Day.Ganzhi}　{p.Hour.Ganzhi}\n" +
        $"日主：{p.DayMaster}{p.DayMasterWuxing}　{p.Strength}（{p.Strength100}）\n" +
        $"生肖：{p.Zodiac}\n" +
        $"喜用：{string.Join("、", p.XiYong)}　次喜：{string.Join("、", p.XiCi)}\n" +
        $"忌：{string.Join("、", p.JiShen)}　次忌：{string.Join("、", p.JiCi)}\n" +
        string.Join("\n", p.Notes);

    private static Dictionary<string, object> BuildDestinyDict(Pillars p)
    {
        return new Dictionary<string, object>
        {
        ["human_readable"] = BuildHumanDestiny(p),
        ["zodiac"] = p.Zodiac,
        ["lunar_text"] = p.LunarText,
        ["day_master"] = new Dictionary<string, object>
        {
            ["gan"] = p.DayMaster,
            ["wuxing"] = p.DayMasterWuxing,
            ["strength"] = p.Strength,
            ["strength_100"] = p.Strength100,
        },
        ["naming_direction"] = new Dictionary<string, object>
        {
            ["primary"] = p.XiYong.FirstOrDefault() ?? "",
            ["secondary"] = p.XiCi.FirstOrDefault() ?? p.XiYong.Skip(1).FirstOrDefault() ?? "",
            ["avoid"] = p.JiShen.FirstOrDefault() ?? "",
            ["explanation"] = $"日主{p.DayMaster}{p.DayMasterWuxing}，強度{p.Strength100}/100（{p.Strength}）。命名宜以「{p.XiYong.FirstOrDefault() ?? "中和"}」為主。",
            ["xi_yong"] = p.XiYong,
            ["ji_shen"] = p.JiShen,
        },
        ["wuxing"] = new Dictionary<string, object>
        {
            ["levels"] = p.ElementLevels,
            ["share"] = p.ElementShare,
            ["lines"] = p.ElementLevels.Select(kv => $"{kv.Key}氣{kv.Value}").ToList(),
        },
        ["four_pillars"] = new Dictionary<string, object>
        {
            ["year"] = PillarDict(p.Year, "年柱"),
            ["month"] = PillarDict(p.Month, "月柱"),
            ["day"] = PillarDict(p.Day, "日柱"),
            ["hour"] = PillarDict(p.Hour, "時柱"),
        },
        };
    }

    private static Dictionary<string, object> PillarDict(Pillar p, string role)
    {
        return new Dictionary<string, object>
        {
        ["role"] = role,
        ["ganzhi"] = p.Ganzhi,
        ["gan"] = p.Gan,
        ["zhi"] = p.Zhi,
        ["gan_wuxing"] = p.GanWuxing,
        ["zhi_wuxing"] = p.ZhiWuxing,
        };
    }
}
}
