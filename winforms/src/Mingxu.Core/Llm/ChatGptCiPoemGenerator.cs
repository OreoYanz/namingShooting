using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mingxu.Core.Models;
using Newtonsoft.Json.Linq;

namespace Mingxu.Core.Llm
{

public sealed class ChatGptCiPoemResult
{
    public string Title { get; set; }
    public string Cipai { get; set; }
    public string Lyric { get; set; }
    public string Appreciation { get; set; }
    public string DisplayText { get; set; }
    public string Error { get; set; }
    public bool Ok { get { return !string.IsNullOrWhiteSpace(DisplayText) && string.IsNullOrEmpty(Error); } }

    public ChatGptCiPoemResult()
    {
        Title = "";
        Cipai = "";
        Lyric = "";
        Appreciation = "";
        DisplayText = "";
        Error = "";
    }
}

/// <summary>
/// 匯出 PDF 時：依姓名創作嵌名宋詞＋白話文賞析。
/// </summary>
public static class ChatGptCiPoemGenerator
{
    public static ChatGptCiPoemResult Generate(AnalysisRequest req, NameSuggestion sug, Pillars pillars)
    {
        var result = new ChatGptCiPoemResult();
        if (sug == null || string.IsNullOrWhiteSpace(sug.FullName))
        {
            result.Error = "缺少姓名，無法創作宋詞。";
            return result;
        }

        var fullName = sug.FullName.Trim();
        var chars = fullName.Where(ch => ch >= 0x4e00 && ch <= 0x9fff).Select(ch => ch.ToString()).ToList();
        var system = "你是精通宋詞與古典詩學的華文詞人。請以繁體中文創作，嚴格輸出 JSON，勿加 markdown 代號。";
        var user = BuildUserPrompt(fullName, chars, sug, pillars, req);

        string error;
        var content = OpenAiChatClient.PostJson(
            req.ChatGptBaseUrl,
            req.ChatGptApiKey,
            req.ChatGptModel,
            system,
            user,
            0.85,
            out error);

        if (!string.IsNullOrEmpty(error))
        {
            result.Error = error;
            return result;
        }

        return Parse(content ?? "", fullName);
    }

    private static string BuildUserPrompt(
        string fullName,
        List<string> chars,
        NameSuggestion sug,
        Pillars pillars,
        AnalysisRequest req)
    {
        var charList = chars.Count == 0 ? fullName : string.Join("」、「", chars);
        var embedHint = BuildEmbedHint(chars);

        var sb = new StringBuilder();
        sb.AppendLine("請以「" + fullName + "」" + chars.Count + "個字為核心，創作一首宋詞（詞名自訂），將名字自然嵌於詞中，並具備深遠寓意。");
        sb.AppendLine();
        sb.AppendLine("要求如下：");
        sb.AppendLine();
        sb.AppendLine("意境與寓意：結合古典意象（如微風、明月、遠山），傳達出歷經風雨後內心依然澄澈、豁達的堅韌。");
        sb.AppendLine("結構與格律：符合詞牌格律與押韻規範（可標明詞牌；若無既有詞牌請將 cipai 留空）。");
        sb.AppendLine("嵌字巧思：請將「" + charList + "」" +
            (chars.Count > 0 ? "等字分別巧妙融入詞中" : "巧妙融入詞中") +
            "（" + embedHint + "），不可有生硬拼湊感。");
        sb.AppendLine("賞析說明：創作完成後，請附帶一段白話文賞析，說明詞中如何呼應名字與寓意。");
        sb.AppendLine();
        sb.AppendLine("補充參考（勿生硬塞入命理術語）：");
        sb.AppendLine("姓名：" + fullName + "；性別：" + (req != null && req.Gender == "F" ? "女" : "男") +
            "；綜合評等：" + sug.Grade + "（" + sug.Total.ToString("0") + "）。");
        if (pillars != null)
        {
            sb.AppendLine("日主 " + pillars.DayMaster + pillars.DayMasterWuxing +
                "；生肖 " + pillars.Zodiac +
                "；喜用 " + string.Join("、", pillars.XiYong ?? new List<string>()) + "。");
        }
        if (req != null && !string.IsNullOrWhiteSpace(req.AestheticBrief))
            sb.AppendLine("風格偏好：" + req.AestheticBrief.Trim());
        sb.AppendLine();
        sb.AppendLine("只輸出 JSON，格式：");
        sb.AppendLine("{\"title\":\"詞名\",\"cipai\":\"\",\"lyric\":\"詞全文（可用換行）\",\"appreciation\":\"白話文賞析\"}");
        sb.AppendLine("說明：cipai 僅填真實詞牌名稱（如水調歌頭）；沒有詞牌時必須為空字串 \"\"，勿填說明文字。");
        return sb.ToString();
    }

    private static string BuildEmbedHint(List<string> chars)
    {
        if (chars == null || chars.Count == 0)
            return "請自然嵌名";
        if (chars.Count == 1)
            return "例如「" + chars[0] + "」出現於起句或過片";
        if (chars.Count == 2)
            return "例如「" + chars[0] + "」在起句，「" + chars[1] + "」在下闋轉折處";
        return "例如「" + chars[0] + "」在起句，「" + chars[1] + "」與「" + chars[2] + "」在下闋轉折處";
    }

    private static ChatGptCiPoemResult Parse(string content, string fullName)
    {
        var result = new ChatGptCiPoemResult();
        var text = (content ?? "").Trim();
        if (text.StartsWith("```"))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
                text = text.Substring(start, end - start + 1);
        }

        try
        {
            var obj = JObject.Parse(text);
            result.Title = (obj["title"] ?? "").ToString().Trim();
            result.Cipai = (obj["cipai"] ?? "").ToString().Trim();
            result.Lyric = NormalizeNewlines((obj["lyric"] ?? "").ToString());
            result.Appreciation = NormalizeNewlines((obj["appreciation"] ?? "").ToString());

            if (string.IsNullOrWhiteSpace(result.Lyric))
            {
                result.Error = "ChatGPT 有回應但缺少詞文。";
                return result;
            }

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(result.Title))
                sb.AppendLine("《" + result.Title + "》");
            // 詞牌一律不顯示於報告
            sb.AppendLine(result.Lyric.Trim());
            if (!string.IsNullOrWhiteSpace(result.Appreciation))
            {
                sb.AppendLine();
                sb.AppendLine("【賞析】");
                sb.Append(result.Appreciation.Trim());
            }
            result.DisplayText = SanitizePoemDisplay(sb.ToString());
            return result;
        }
        catch
        {
            // 非 JSON 時整段當詞文
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.Lyric = NormalizeNewlines(text);
                result.DisplayText = SanitizePoemDisplay(result.Lyric);
                return result;
            }
            result.Error = "無法解析宋詞回應。";
            return result;
        }
    }

    /// <summary>詞牌一律不顯示於報告（保留方法供相容；恆為 false）。</summary>
    public static bool ShouldShowCipai(string cipai)
    {
        return false;
    }

    /// <summary>從寄語全文剔除詞牌行與不應顯示的標籤／佔位文字。</summary>
    public static string SanitizePoemDisplay(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text;
        // 整行詞牌括註：（水調歌頭）（自度曲）（無則空字串）等
        t = System.Text.RegularExpressions.Regex.Replace(
            t, @"(?m)^\s*[（(【\[][^）)\]]*[）)】\]]\s*$", "");
        // （自度曲）等夾在文中
        t = System.Text.RegularExpressions.Regex.Replace(
            t, @"[（(【\[]\s*自\s*度\s*曲\s*[）)】\]]\s*", "");
        t = System.Text.RegularExpressions.Regex.Replace(
            t, @"[（(【\[]\s*詞\s*牌\s*[（(]?\s*無\s*則\s*空\s*字\s*串\s*[）)]?\s*[）)】\]]\s*", "");
        t = System.Text.RegularExpressions.Regex.Replace(
            t, @"[（(【\[]\s*無\s*則\s*空\s*字\s*串\s*[）)】\]]\s*", "");
        t = System.Text.RegularExpressions.Regex.Replace(
            t, @"無\s*則\s*空\s*字\s*串", "");
        t = System.Text.RegularExpressions.Regex.Replace(
            t, @"自\s*度\s*曲", "");
        t = System.Text.RegularExpressions.Regex.Replace(
            t, @"(?m)^\s*[（(]\s*[）)]\s*$", "");
        t = System.Text.RegularExpressions.Regex.Replace(t, @"\n{3,}", "\n\n");
        return t.Trim();
    }

    private static string NormalizeNewlines(string s)
    {
        return (s ?? "").Replace("\\n", "\n").Replace("\r\n", "\n").Trim();
    }
}
}
