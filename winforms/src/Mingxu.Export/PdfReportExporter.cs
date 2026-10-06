using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mingxu.Core.Content;
using Mingxu.Core.Liunian;
using Mingxu.Core.Llm;
using Mingxu.Core.Models;
using Mingxu.Core.Scoring;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Mingxu.Export
{

public static class PdfReportExporter
{
    public static string Export(
        AnalysisRequest req,
        AnalysisResult result,
        NameSuggestion sug,
        string outputPath)
    {
        WindowsFontResolver.EnsureInstalled();

        var pillars = result.Pillars;
        var parents = string.Join("、", new string[] { req.FatherName, req.MotherName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var content = ContentPackBuilder.Build(
            sug,
            pillars,
            parents,
            req.Mode == "rename" ? "rename" : "newborn");

        var liunianForPdf = (result.Liunian ?? new List<YearLuck>()).ToList();
        if (req != null && req.Mode != "liunian")
        {
            int start, end;
            req.ResolveLiunianRange(out start, out end);
            // newborn/rename：沿用起迄或舊 LiunianYears 推算後的結果筆數
            var take = Math.Max(10, req.LiunianYearCount);
            liunianForPdf = liunianForPdf.Take(take).ToList();
        }
        var useReportApi = req != null
            && !string.IsNullOrWhiteSpace(req.ChatGptApiKey)
            && (req.Mode == "newborn" || req.Mode == "rename");

        if (req != null && req.Mode == "liunian")
        {
            // 流年解說已在分析階段打 API；此處只組 PDF
            object overviewObj;
            if (result.Destiny != null && result.Destiny.TryGetValue("liunian_overview", out overviewObj) && overviewObj != null)
                content["idea"] = overviewObj.ToString();
            PutDestiny(content, result.Destiny, "liunian_phases", "phases");
            PutDestiny(content, result.Destiny, "liunian_trend", "trend");
            PutDestiny(content, result.Destiny, "liunian_actions", "actions");
            // 流年 PDF 不強制再產寄語；若有 Key 可選產一首
            if (!string.IsNullOrWhiteSpace(req.ChatGptApiKey))
            {
                var ci = ChatGptCiPoemGenerator.Generate(req, sug, pillars);
                if (ci.Ok)
                {
                    content["poem"] = ci.DisplayText;
                }
            }
        }
        else if (useReportApi)
        {
            // 新生兒／改名：一次 API 產流年文案＋理念／祝福／期許＋專屬寄語
            var pack = ChatGptNewbornReportGenerator.Generate(req, sug, pillars, liunianForPdf, parents);
            if (!string.IsNullOrEmpty(pack.Error) && !pack.HasCopy && !(pack.Poem != null && pack.Poem.Ok))
            {
                // 改用本地，不寫入客戶可見來源註記
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(pack.Idea)) content["idea"] = pack.Idea;
                if (!string.IsNullOrWhiteSpace(pack.Blessing)) content["blessing"] = pack.Blessing;
                if (!string.IsNullOrWhiteSpace(pack.Hope)) content["hope"] = pack.Hope;
                if (pack.Liunian != null && pack.Liunian.Count > 0)
                {
                    var localTake = (result.Liunian ?? new List<YearLuck>()).Take(10).ToList();
                    liunianForPdf = MergeNewbornLiunianForPdf(localTake, pack.Liunian.Take(10).ToList());
                }
                if (pack.Poem != null && pack.Poem.Ok)
                {
                    content["poem"] = pack.Poem.DisplayText;
                    content["poem_title"] = pack.Poem.Title;
                    content["poem_appreciation"] = pack.Poem.Appreciation;
                }
                else
                {
                    var ci = ChatGptCiPoemGenerator.Generate(req, sug, pillars);
                    if (ci.Ok)
                    {
                        content["poem"] = ci.DisplayText;
                    }
                }
            }
        }
        else if (req != null && !string.IsNullOrWhiteSpace(req.ChatGptApiKey))
        {
            var ci = ChatGptCiPoemGenerator.Generate(req, sug, pillars);
            if (ci.Ok)
            {
                content["poem"] = ci.DisplayText;
                content["poem_title"] = ci.Title;
                content["poem_appreciation"] = ci.Appreciation;
            }
        }
        content["poem_note"] = "";

        var full = Path.GetFullPath(outputPath);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var doc = new PdfDocument();
        doc.Info.Title = $"名序命名剖象－{sug.FullName}";
        doc.Info.Author = "名序";
        var fontTitle = SafeFont(16, true);
        var fontH = SafeFont(11, true);
        var font = SafeFont(9, false);
        var fontSmall = SafeFont(8, false);
        var writer = new PdfWriter(doc);

        writer.DrawLogo(FindLogoPath(), 36);
        writer.WriteLine("名序 － 以命為本．以字成名", fontTitle, 22);
        writer.WriteLine("論命．姓名學．流年大運", fontSmall, 14);
        var now = DateTime.Now;
        var age = Math.Max(1, now.Year - req.Birth.Year + 1);
        var service = req.Mode == "rename" ? "改名" : req.Mode == "liunian" ? "流年" : "命名";
        writer.WriteLine($"{now:yyyy　年　MM　月　dd　日}　　虛歲 {age}　　本案：{service}　{sug.FullName}", font, 16);
        writer.AddVerticalSpace(4);
        writer.WriteLine("基本資料", fontH, 16);
        writer.WriteLine($"姓氏：{sug.Surname}　　姓名：{sug.FullName}　　性別：{(req.Gender == "M" ? "男" : "女")}", font, 14);
        writer.WriteLine($"出生：{req.Birth:yyyy/MM/dd HH:mm}　{req.BirthPlace}　{(pillars == null ? "" : pillars.LunarText)}", font, 14);
        if (!string.IsNullOrWhiteSpace(req.FatherName) || !string.IsNullOrWhiteSpace(req.MotherName))
            writer.WriteLine($"父親：{req.FatherName}　母親：{req.MotherName}", font, 14);
        writer.AddVerticalSpace(6);
        writer.WriteLine("命名剖象 - 綜合命理八字．三才易數", fontH, 16);
        writer.WriteLine($"綜合評估：{sug.Total:0}（{sug.Grade}）", font, 14);
        writer.WriteLine($"八字（{sug.BaziScore:0}）：{(pillars == null ? "" : pillars.Year.Ganzhi)} {(pillars == null ? "" : pillars.Month.Ganzhi)} {(pillars == null ? "" : pillars.Day.Ganzhi)} {(pillars == null ? "" : pillars.Hour.Ganzhi)}　日主{(pillars == null ? "" : pillars.DayMaster)}{(pillars == null ? "" : pillars.DayMasterWuxing)}", font, 14);
        writer.Wrap($"喜用：{string.Join("、", pillars == null ? new List<string>() : pillars.XiYong)}；忌：{string.Join("、", pillars == null ? new List<string>() : pillars.JiShen)}", font);
        writer.Wrap($"五行（{sug.WuxingScore:0}）：{string.Join("、", sug.CharWuxing)}", font);
        writer.Wrap($"音韻（{sug.PhonologyScore:0}）／字義（{sug.MeaningScore:0}）：{string.Join("；", sug.Meanings.Take(3))}", font);
        if (sug.Wuge != null)
        {
            var w = sug.Wuge;
            writer.Wrap($"總格：天{w.Tian}({w.TianWx}·{w.TianLuck}) 人{w.Ren}({w.RenWx}·{w.RenLuck}) 地{w.Di}({w.DiWx}·{w.DiLuck}) 外{w.Wai} 總{w.Zong}；三才{w.Sancai}（{w.SancaiLuck}）", font);
        }

        if (req.Mode == "rename")
        {
            writer.AddVerticalSpace(6);
            writer.WriteLine("專業改名 － 前後比較分析", fontH, 16);
            object dirObj;
            if (result.Destiny != null && result.Destiny.TryGetValue("rename_directions", out dirObj) && dirObj != null)
                writer.Wrap(dirObj.ToString(), font);
            object evalObj;
            if (result.Destiny != null && result.Destiny.TryGetValue("current_eval", out evalObj) && evalObj != null)
            {
                writer.AddVerticalSpace(4);
                writer.Wrap(evalObj.ToString(), font);
            }
            if (result.Current != null && result.Current.Wuge != null)
            {
                var ow = result.Current.Wuge;
                writer.Wrap(
                    "原名三才五格：天" + AdultRename.FormatGridCell(ow.Tian, ow.TianWx, ow.TianLuck) +
                    " 人" + AdultRename.FormatGridCell(ow.Ren, ow.RenWx, ow.RenLuck) +
                    " 地" + AdultRename.FormatGridCell(ow.Di, ow.DiWx, ow.DiLuck) +
                    "；三才" + ow.Sancai + "（" + ow.SancaiLuck + "）", font);
            }
            writer.Wrap("候選新名：" + sug.FullName + "（綜合 " + sug.Total.ToString("0") + "）", font);
            if (!string.IsNullOrWhiteSpace(sug.ComparisonText))
            {
                writer.AddVerticalSpace(4);
                writer.Wrap(sug.ComparisonText, fontSmall);
            }
            else if (sug.RenameComparison != null)
            {
                writer.Wrap(AdultRename.FormatComparisonReport(sug.RenameComparison, result.Current, sug), fontSmall);
            }
        }

        writer.AddVerticalSpace(6);
        writer.EnsureSpace(80);
        writer.WriteLine("命名剖象 - 報告", fontH, 16);
        writer.Wrap("【命名解析】" + ValueOrEmpty(content, "chars"), font);
        writer.Wrap(ValueOrEmpty(content, "combo") + " " + ValueOrEmpty(content, "destiny"), font);
        writer.AddVerticalSpace(4);
        writer.Wrap("【未來" + liunianForPdf.Count + "年】", fontH);
        if (req.Mode == "liunian")
        {
            writer.Wrap(LiunianEngine.FormatYearOverviewTable(liunianForPdf), fontSmall);
            writer.AddVerticalSpace(4);
            foreach (var yr in liunianForPdf)
            {
                writer.Wrap(LiunianEngine.FormatYearDetail(yr, yr.Months != null && yr.Months.Count > 0), fontSmall);
                writer.AddVerticalSpace(3);
            }
        }
        else
        {
            foreach (var yr in liunianForPdf)
                writer.Wrap($"{yr.Year}（{yr.Age}歲・{yr.Ganzhi}）〔{yr.Level}〕{yr.Summary}", fontSmall);
        }
        writer.AddVerticalSpace(4);
        var ideaTitle = req.Mode == "rename" ? "【改名理念】" : req.Mode == "liunian" ? "【流年總覽】" : "【命名理念】";
        writer.Wrap(ideaTitle + ValueOrEmpty(content, "idea"), font);
        if (req.Mode == "liunian")
        {
            if (!string.IsNullOrWhiteSpace(ValueOrEmpty(content, "phases")))
                writer.Wrap("【宏觀階段】\n" + ValueOrEmpty(content, "phases"), font);
            if (!string.IsNullOrWhiteSpace(ValueOrEmpty(content, "trend")))
                writer.Wrap("【趨勢與風險預警】\n" + ValueOrEmpty(content, "trend"), font);
            if (!string.IsNullOrWhiteSpace(ValueOrEmpty(content, "actions")))
                writer.Wrap("【行動建議 Do's / Don'ts】\n" + ValueOrEmpty(content, "actions"), font);
        }
        else
        {
            writer.Wrap("【祝福】" + ValueOrEmpty(content, "blessing"), font);
            writer.Wrap("【人生期許】" + ValueOrEmpty(content, "hope"), font);
        }
        writer.Wrap("【專屬寄語】\n" + ChatGptCiPoemGenerator.SanitizePoemDisplay(ValueOrEmpty(content, "poem")), font);
        writer.AddVerticalSpace(10);
        writer.Wrap("內容僅供文化參考，命理分析不構成人生保證。用字與改名請向戶政事務所確認。", fontSmall);
        writer.Wrap("替每一個人生，寫下值得珍藏的第一行文字。", fontSmall);

        doc.Save(full);
        return full;
    }

    private static string ValueOrEmpty(Dictionary<string, string> values, string key)
    {
        string value;
        return values.TryGetValue(key, out value) ? value : "";
    }

    private static void PutDestiny(Dictionary<string, string> content, Dictionary<string, object> destiny, string srcKey, string dstKey)
    {
        if (content == null || destiny == null) return;
        object value;
        if (!destiny.TryGetValue(srcKey, out value) || value == null) return;
        var text = value.ToString();
        if (!string.IsNullOrWhiteSpace(text))
            content[dstKey] = text;
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

    private sealed class PdfWriter
    {
        private readonly PdfDocument _document;
        private PdfPage _page;
        private XGraphics _graphics;
        private double _y;
        private const double Left = 40;

        public PdfWriter(PdfDocument document)
        {
            _document = document;
            AddPage();
        }

        private double Width { get { return _page.Width - 80; } }

        public void DrawLogo(string path, double size)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                using (var img = XImage.FromFile(path))
                {
                    EnsureSpace(size + 8);
                    _graphics.DrawImage(img, Left, _y, size, size);
                    _y += size + 6;
                }
            }
            catch
            {
                // skip logo if image cannot be decoded
            }
        }

        public void WriteLine(string text, XFont font, double lineHeight)
        {
            _graphics.DrawString(text, font, XBrushes.Black, new XRect(Left, _y, Width, lineHeight + 8), XStringFormats.TopLeft);
            _y += lineHeight;
        }

        public void Wrap(string text, XFont font)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            foreach (var line in BreakLines(_graphics, text.Replace("\r", ""), font, Width))
            {
                EnsureSpace(20);
                _graphics.DrawString(line, font, XBrushes.Black, new XRect(Left, _y, Width, 16), XStringFormats.TopLeft);
                _y += font.Size + 5;
            }
        }

        public void AddVerticalSpace(double amount)
        {
            _y += amount;
        }

        public void EnsureSpace(double need)
        {
            if (_y + need < _page.Height - 40) return;
            AddPage();
        }

        private void AddPage()
        {
            _page = _document.AddPage();
            _page.Size = PdfSharpCore.PageSize.A4;
            _graphics = XGraphics.FromPdfPage(_page);
            _y = 36;
        }
    }

    private static List<string> BreakLines(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        var result = new List<string>();
        foreach (var para in text.Split('\n'))
        {
            if (para.Length == 0) { result.Add(""); continue; }
            var sb = new StringBuilder();
            foreach (var ch in para)
            {
                var trial = sb.ToString() + ch;
                if (gfx.MeasureString(trial, font).Width > maxWidth && sb.Length > 0)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                sb.Append(ch);
            }
            if (sb.Length > 0) result.Add(sb.ToString());
        }
        return result;
    }

    private static List<YearLuck> MergeNewbornLiunianForPdf(List<YearLuck> local, List<YearLuck> llm)
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
            if (string.IsNullOrWhiteSpace(y.Overall) && !string.IsNullOrWhiteSpace(y.Summary))
                y.Overall = y.Summary;
        }
        return local;
    }

    private static XFont SafeFont(double size, bool bold)
    {
        foreach (var family in new string[] { "DFKai-SB", "標楷體", "Microsoft JhengHei", "Arial" })
        {
            try
            {
                return new XFont(family, size, bold ? XFontStyle.Bold : XFontStyle.Regular);
            }
            catch
            {
                // try next
            }
        }
        return new XFont("Arial", size, XFontStyle.Regular);
    }
}
}
