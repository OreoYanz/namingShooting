using System;
using System.IO;
using System.Linq;
using System.Text;
using Mingxu.Core.Content;
using Mingxu.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mingxu.Core.Site
{
    /// <summary>
    /// 官網「真實案例」字卡資料。匯出 PDF 時寫出 .case.json，
    /// 並由 <see cref="CaseSitePublisher"/> 併入 site／docs 的 cases.json 後 commit／push。
    /// </summary>
    public static class CaseCardBuilder
    {
        public static JObject Build(AnalysisRequest req, AnalysisResult result, NameSuggestion sug)
        {
            if (req == null) throw new ArgumentNullException("req");
            if (sug == null) throw new ArgumentNullException("sug");

            var mode = (req.Mode ?? "newborn").Trim().ToLowerInvariant();
            if (mode != "newborn" && mode != "rename" && mode != "liunian")
                mode = "newborn";

            var genderLabel = GenderLabel(req.Gender);
            var surname = sug.Surname ?? "";
            var displayName = MaskDisplayName(surname, sug.Given);
            // 公開案例僅顯示年月，避免完整出生日期外洩
            var birthDate = req.Birth.ToString("yyyy-MM");
            var region = string.IsNullOrWhiteSpace(req.BirthPlace) ? "" : req.BirthPlace.Trim();
            var id = BuildId(mode, req.Birth, displayName);

            var card = new JObject
            {
                ["id"] = id,
                ["service"] = mode,
                ["serviceLabel"] = ServiceLabel(mode),
                ["birthDate"] = birthDate,
                ["region"] = region,
                ["displayName"] = displayName,
                ["surname"] = surname,
                ["isCompoundSurname"] = surname.Length >= 2,
                ["gender"] = genderLabel,
                ["exportedAt"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            };

            if (mode == "liunian")
            {
                card["macroStage"] = BuildMacroStage(result);
                card["image"] = "";
            }
            else
            {
                card["image"] = BuildImageSummary(sug, result != null ? result.Pillars : null);
                card["macroStage"] = "";
            }

            return card;
        }

        /// <summary>寫入與 PDF 同名的 .case.json，回傳路徑。</summary>
        public static string WriteBesidePdf(string pdfPath, AnalysisRequest req, AnalysisResult result, NameSuggestion sug)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException("pdfPath");

            var card = Build(req, result, sug);
            var jsonPath = Path.ChangeExtension(pdfPath, ".case.json");
            var text = card.ToString(Formatting.Indented);
            File.WriteAllText(jsonPath, text, new UTF8Encoding(false));
            return jsonPath;
        }

        public static string MaskDisplayName(string surname, string given)
        {
            var s = surname ?? "";
            if (string.IsNullOrEmpty(s) && !string.IsNullOrEmpty(given))
                return "OO";
            return s + "OO";
        }

        public static string GenderLabel(string gender)
        {
            if (string.Equals(gender, "F", StringComparison.OrdinalIgnoreCase)) return "女";
            if (string.Equals(gender, "M", StringComparison.OrdinalIgnoreCase)) return "男";
            return "—";
        }

        public static string ServiceLabel(string mode)
        {
            if (mode == "rename") return "專業改名";
            if (mode == "liunian") return "流年分析";
            return "新生兒命名";
        }

        private static string BuildId(string mode, DateTime birth, string displayName)
        {
            var prefix = mode == "rename" ? "rn" : mode == "liunian" ? "ln" : "nb";
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var safe = new string((displayName ?? "").Where(c => c < 128 && char.IsLetterOrDigit(c)).ToArray());
            if (string.IsNullOrEmpty(safe)) safe = birth.ToString("yyyyMMdd");
            return prefix + "-" + stamp + "-" + safe;
        }

        private static string BuildImageSummary(NameSuggestion sug, Pillars pillars)
        {
            var parents = "";
            var pack = ContentPackBuilder.Build(sug, pillars, parents, "newborn");
            string image;
            if (pack != null && pack.TryGetValue("image", out image) && !string.IsNullOrWhiteSpace(image))
            {
                // 「意象偏向成長、生發、青枝向榮。…」→ 取標籤段
                var t = image.Trim();
                const string head = "意象偏向";
                if (t.StartsWith(head, StringComparison.Ordinal))
                    t = t.Substring(head.Length);
                var cut = t.IndexOf('。');
                if (cut > 0) t = t.Substring(0, cut);
                t = t.Trim('。', '，', ' ', '　');
                if (t.Length > 0) return t;
            }
            return "清和、安定、長遠";
        }

        private static string BuildMacroStage(AnalysisResult result)
        {
            if (result == null || result.Liunian == null || result.Liunian.Count == 0)
                return "流年觀察";

            var years = result.Liunian;
            var keyword = years
                .Where(y => y != null && !string.IsNullOrWhiteSpace(y.Keyword))
                .GroupBy(y => y.Keyword.Trim())
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Average(x => x.TotalScore))
                .Select(g => g.Key)
                .FirstOrDefault();

            var dayun = years
                .Where(y => y != null && !string.IsNullOrWhiteSpace(y.DaYun))
                .GroupBy(y => y.DaYun.Trim())
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(dayun) && !string.IsNullOrEmpty(keyword))
                return "大運" + dayun + "｜" + keyword;
            if (!string.IsNullOrEmpty(keyword))
                return keyword;
            if (!string.IsNullOrEmpty(dayun))
                return "大運" + dayun;
            return "流年觀察";
        }
    }
}
