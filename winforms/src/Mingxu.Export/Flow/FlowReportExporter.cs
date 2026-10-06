using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mingxu.Core.Content;
using Mingxu.Core.Llm;
using Mingxu.Core.Models;
using Mingxu.Core.Scoring;

namespace Mingxu.Export.Flow
{
    public static class FlowReportExporter
    {
        public static string BuildHtml(
            AnalysisRequest req,
            AnalysisResult result,
            NameSuggestion sug,
            string logoPath = null)
        {
            if (logoPath == null)
                logoPath = FindLogoPath();

            var mode = req != null ? (req.Mode ?? "").Trim().ToLowerInvariant() : "";
            if (mode == "newborn" || mode == "rename")
                return BuildNamingHtml(req, result, sug, logoPath);

            return BuildLiunianHtml(req, result, sug, logoPath);
        }

        private static string BuildLiunianHtml(
            AnalysisRequest req,
            AnalysisResult result,
            NameSuggestion sug,
            string logoPath)
        {
            var narrativeExtraPoem = "";
            if (req != null && !string.IsNullOrWhiteSpace(req.ChatGptApiKey))
            {
                try
                {
                    var ci = ChatGptCiPoemGenerator.Generate(req, sug, result != null ? result.Pillars : null);
                    if (ci != null && ci.Ok)
                        narrativeExtraPoem = ChatGptCiPoemGenerator.SanitizePoemDisplay(ci.DisplayText ?? "");
                }
                catch
                {
                    // 寄語失敗時略過，不在 PDF 顯示來源註記
                }
            }

            var data = FlowVisualizationMapper.Map(req, result, sug, logoPath);
            if (data.Narrative == null) data.Narrative = new FlowNarrative();
            if (!string.IsNullOrWhiteSpace(narrativeExtraPoem))
                data.Narrative.Poem = narrativeExtraPoem;
            data.Narrative.PoemNote = "";
            return FlowReportHtmlBuilder.Build(data);
        }

        private static string BuildNamingHtml(
            AnalysisRequest req,
            AnalysisResult result,
            NameSuggestion sug,
            string logoPath)
        {
            var pillars = result != null ? result.Pillars : null;
            var parents = string.Join("、", new[] { req.FatherName, req.MotherName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var serviceType = req.Mode == "rename" ? "rename" : "newborn";
            var content = ContentPackBuilder.Build(sug, pillars, parents, serviceType);

            var liunianForPdf = (result.Liunian ?? new List<YearLuck>()).ToList();
            int start, end;
            req.ResolveLiunianRange(out start, out end);
            var take = Math.Max(10, req.LiunianYearCount);
            liunianForPdf = liunianForPdf.Take(take).ToList();

            var useReportApi = !string.IsNullOrWhiteSpace(req.ChatGptApiKey);

            if (useReportApi)
            {
                try
                {
                    var pack = ChatGptNewbornReportGenerator.Generate(req, sug, pillars, liunianForPdf, parents);
                    if (!string.IsNullOrEmpty(pack.Error) && !pack.HasCopy && !(pack.Poem != null && pack.Poem.Ok))
                    {
                        // 改用本地模板，不在 PDF 顯示來源註記
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(pack.Idea)) content["idea"] = pack.Idea;
                        if (!string.IsNullOrWhiteSpace(pack.Blessing)) content["blessing"] = pack.Blessing;
                        if (!string.IsNullOrWhiteSpace(pack.Hope)) content["hope"] = pack.Hope;
                        if (pack.Liunian != null && pack.Liunian.Count > 0)
                            liunianForPdf = MergeLiunianText(
                                (result.Liunian ?? new List<YearLuck>()).Take(take).ToList(),
                                pack.Liunian.Take(take).ToList());
                        if (pack.Poem != null && pack.Poem.Ok)
                        {
                            content["poem"] = ChatGptCiPoemGenerator.SanitizePoemDisplay(pack.Poem.DisplayText);
                        }
                        else
                        {
                            var ci = ChatGptCiPoemGenerator.Generate(req, sug, pillars);
                            if (ci != null && ci.Ok)
                                content["poem"] = ChatGptCiPoemGenerator.SanitizePoemDisplay(ci.DisplayText);
                        }
                    }
                }
                catch
                {
                    // 改用本地模板，不在 PDF 顯示來源註記
                }
            }

            // 映射用裁切後的流年結果（不改動呼叫端原物件）
            var mappedResult = new AnalysisResult
            {
                Pillars = result.Pillars,
                Destiny = result.Destiny,
                Suggestions = result.Suggestions,
                Current = result.Current,
                Liunian = liunianForPdf,
                HumanReadableDestiny = result.HumanReadableDestiny,
            };

            var data = FlowVisualizationMapper.Map(req, mappedResult, sug, logoPath);
            data.Naming = new FlowNamingCopy
            {
                CharAnalysis = ValueOrEmpty(content, "chars"),
                Combo = ValueOrEmpty(content, "combo"),
                DestinyNote = ValueOrEmpty(content, "destiny"),
                Image = ValueOrEmpty(content, "image"),
                Story = ValueOrEmpty(content, "story"),
                Idea = ValueOrEmpty(content, "idea"),
                Blessing = ValueOrEmpty(content, "blessing"),
                Hope = ValueOrEmpty(content, "hope"),
                ParentsText = parents,
                CharEntries = BuildCharEntries(sug),
            };
            if (req.Mode == "rename")
            {
                data.Naming.OriginalName = !string.IsNullOrWhiteSpace(req.CurrentFullName)
                    ? req.CurrentFullName.Trim()
                    : (result.Current != null ? result.Current.FullName : "");
                if (result.Current != null)
                    data.Naming.OriginalGrade = result.Current.Grade ?? "";
                object dirObj;
                if (result.Destiny != null && result.Destiny.TryGetValue("rename_directions", out dirObj) && dirObj != null)
                    data.Naming.DirectionsText = dirObj.ToString();
                data.Naming.RenameReason = req.RenameReason ?? "";
                data.Naming.DirectionItems = AdultRename.ParseImproveDirections(req.ImproveDirections);
                data.Naming.FocusItems = ParseFocusItems(req.RenameFocus);
                object evalObj;
                if (result.Destiny != null && result.Destiny.TryGetValue("current_eval", out evalObj) && evalObj != null)
                    data.Naming.OriginalEval = evalObj.ToString();
                if (sug.RenameComparison != null)
                    data.Naming.Compare = sug.RenameComparison;
                var oldWugeSrc = sug.RenameComparison != null && sug.RenameComparison.Wuge != null
                    ? sug.RenameComparison.Wuge.Original
                    : (result.Current != null ? result.Current.Wuge : null);
                var newWugeSrc = sug.RenameComparison != null && sug.RenameComparison.Wuge != null
                    ? sug.RenameComparison.Wuge.NewName
                    : sug.Wuge;
                data.Naming.OriginalWuge = FlowVisualizationMapper.MapWugeVisual(oldWugeSrc, result.Current);
                data.Naming.NewWuge = FlowVisualizationMapper.MapWugeVisual(newWugeSrc, sug);
                if (!string.IsNullOrWhiteSpace(sug.ComparisonText))
                    data.Naming.ComparisonText = sug.ComparisonText;
                else if (sug.RenameComparison != null)
                    data.Naming.ComparisonText = AdultRename.FormatComparisonReport(
                        sug.RenameComparison, result.Current, sug);
            }
            if (data.Narrative == null) data.Narrative = new FlowNarrative();
            data.Narrative.Poem = ChatGptCiPoemGenerator.SanitizePoemDisplay(ValueOrEmpty(content, "poem"));
            data.Narrative.PoemNote = "";
            return FlowReportHtmlBuilder.Build(data);
        }

        public static void WriteHtmlPreview(
            AnalysisRequest req,
            AnalysisResult result,
            NameSuggestion sug,
            string htmlPath)
        {
            var html = BuildHtml(req, result, sug);
            var dir = Path.GetDirectoryName(Path.GetFullPath(htmlPath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(htmlPath, html, System.Text.Encoding.UTF8);
        }

        /// <summary>ChatGPT 只覆寫文案；分數與本地解說骨架保留（與分析管線一致）。</summary>
        private static List<YearLuck> MergeLiunianText(List<YearLuck> local, List<YearLuck> llm)
        {
            if (local == null || local.Count == 0) return llm ?? new List<YearLuck>();
            if (llm == null || llm.Count == 0) return local;
            var byYear = llm.GroupBy(x => x.Year).ToDictionary(g => g.Key, g => g.First());
            foreach (var y in local)
            {
                YearLuck rich;
                if (!byYear.TryGetValue(y.Year, out rich) || rich == null) continue;
                if (!string.IsNullOrWhiteSpace(rich.Summary)) y.Summary = rich.Summary;
                if (!string.IsNullOrWhiteSpace(rich.Level)) y.Level = rich.Level;
                if (!string.IsNullOrWhiteSpace(rich.Keyword)) y.Keyword = rich.Keyword;
                if (!string.IsNullOrWhiteSpace(rich.Outlook)) y.Outlook = rich.Outlook;
                if (!string.IsNullOrWhiteSpace(rich.Overall)) y.Overall = rich.Overall;
                if (!string.IsNullOrWhiteSpace(rich.Career)) y.Career = rich.Career;
                if (!string.IsNullOrWhiteSpace(rich.Wealth)) y.Wealth = rich.Wealth;
                if (!string.IsNullOrWhiteSpace(rich.Relationship)) y.Relationship = rich.Relationship;
                if (!string.IsNullOrWhiteSpace(rich.Life)) y.Life = rich.Life;
                if (rich.Suitable != null && rich.Suitable.Count > 0) y.Suitable = rich.Suitable;
                if (rich.Avoid != null && rich.Avoid.Count > 0) y.Avoid = rich.Avoid;
                if (rich.Advice != null && rich.Advice.Count > 0) y.Advice = rich.Advice;
                if (rich.Risks != null && rich.Risks.Count > 0) y.Risks = rich.Risks;
                if (string.IsNullOrWhiteSpace(y.Overall) && !string.IsNullOrWhiteSpace(y.Summary))
                    y.Overall = y.Summary;
            }
            return local;
        }

        private static string ValueOrEmpty(Dictionary<string, string> values, string key)
        {
            string value;
            return values != null && values.TryGetValue(key, out value) ? value : "";
        }

        private static List<FlowCharEntry> BuildCharEntries(NameSuggestion sug)
        {
            var list = new List<FlowCharEntry>();
            if (sug == null) return list;
            var given = sug.Given ?? "";
            var meanings = sug.Meanings ?? new List<string>();
            var wxs = sug.CharWuxing ?? new List<string>();
            for (var i = 0; i < given.Length; i++)
            {
                list.Add(new FlowCharEntry
                {
                    Char = given[i].ToString(),
                    Meaning = i < meanings.Count ? (meanings[i] ?? "") : "",
                    Wuxing = i < wxs.Count ? (wxs[i] ?? "") : "",
                });
            }
            // 若 Meanings 比名字長（罕見），補上剩餘說明
            for (var i = given.Length; i < meanings.Count; i++)
            {
                list.Add(new FlowCharEntry
                {
                    Char = "字",
                    Meaning = meanings[i] ?? "",
                    Wuxing = i < wxs.Count ? (wxs[i] ?? "") : "",
                });
            }
            return list;
        }

        private static List<string> ParseFocusItems(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Replace("；", ",").Replace("、", ",").Replace("，", ",")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();
        }

        private static string FindLogoPath()
        {
            var bases = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Assets"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MingxuDesktop", "Assets"),
            };
            foreach (var dir in bases)
            {
                foreach (var name in new[] { "logo-mark.png", "logo-pdf.png", "logo-ui.png", "logo.png" })
                {
                    var path = Path.GetFullPath(Path.Combine(dir, name));
                    if (File.Exists(path)) return path;
                }
            }
            return null;
        }
    }
}
