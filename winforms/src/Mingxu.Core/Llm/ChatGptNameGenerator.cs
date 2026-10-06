using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mingxu.Core.Models;
using Newtonsoft.Json.Linq;

namespace Mingxu.Core.Llm
{

public sealed class ChatGptNameResult
{
    public List<string> Givens { get; set; }
    public string RawNote { get; set; }
    public string Error { get; set; }

    public ChatGptNameResult()
    {
        Givens = new List<string>();
        RawNote = "";
        Error = "";
    }
}

/// <summary>
/// OpenAI Chat Completions（或相容端點）產生名字「名」字組合。
/// 審美／語感為主；命理條件只當軟提示，不做硬過濾。
/// </summary>
public static class ChatGptNameGenerator
{
    public static ChatGptNameResult Generate(
        AnalysisRequest req,
        Pillars pillars,
        string surname,
        int wantCount)
    {
        var result = new ChatGptNameResult();
        var count = Math.Max(12, Math.Min(80, wantCount));
        var system = BuildSystemPrompt();
        var user = BuildUserPrompt(req, pillars, surname, count);

        string error;
        var content = OpenAiChatClient.PostJson(
            req.ChatGptBaseUrl,
            req.ChatGptApiKey,
            req.ChatGptModel,
            system,
            user,
            0.95,
            out error);

        if (!string.IsNullOrEmpty(error))
        {
            result.Error = error;
            return result;
        }

        result.RawNote = OpenAiChatClient.Truncate(content ?? "", 500);
        result.Givens = ParseGivens(content ?? "", surname, req.AllowSingle, req.AllowDouble, count * 2);
        if (result.Givens.Count == 0)
            result.Error = "ChatGPT 有回應但無法解析出有效名（請檢查模型是否支援 JSON）。";
        return result;
    }

    private static string BuildSystemPrompt()
    {
        return string.Join("\n", new[]
        {
            "你是台灣華語姓名美學顧問，擅長取有意境、好念、好寫、不過時的名字。",
            "優先：語感、意象、字形結構、朗朗上口、現代但不俗氣。",
            "命理／五行／筆畫僅作「柔和參考」，不可為了湊命理犧牲美感。",
            "禁止：諧音不雅、低俗、過度常見菜市場名堆砌、生僻到無法書寫辨識的怪字。",
            "只輸出 JSON，格式：{\"givens\":[\"名1\",\"名2\",...],\"note\":\"一句話說明風格\"}",
            "givens 只要「名」不要姓；繁體中文；每項 1～2 字。",
        });
    }

    private static string BuildUserPrompt(AnalysisRequest req, Pillars pillars, string surname, int count)
    {
        var sb = new StringBuilder();
        sb.AppendLine("請為下列條件產生 " + count + " 個「名」候選（去重、風格多樣）。");
        sb.AppendLine("姓氏：" + surname);
        sb.AppendLine("性別：" + (req.Gender == "F" ? "女" : "男"));
        sb.AppendLine("服務：" + (req.Mode == "rename" ? "專業改名" : "新生兒命名"));
        sb.AppendLine("命名模式偏好：" + (req.NamingMode ?? "balanced"));
        if (req.AllowSingle && req.AllowDouble) sb.AppendLine("名長：單名與雙名皆可");
        else if (req.AllowSingle) sb.AppendLine("名長：只要單名");
        else sb.AppendLine("名長：只要雙名");

        if (!string.IsNullOrWhiteSpace(req.Zibei))
            sb.AppendLine("字輩字「" + req.Zibei.Trim() + "」須用於名的" +
                (req.ZibeiPosition == 0 ? "第一字" : "最後一字") + "。");
        if (!string.IsNullOrWhiteSpace(req.PreferredChars))
            sb.AppendLine("可優先融入用字（非強制）：" + req.PreferredChars);
        if (!string.IsNullOrWhiteSpace(req.ForbiddenChars))
            sb.AppendLine("禁用字／詞：" + req.ForbiddenChars);
        if (!string.IsNullOrWhiteSpace(req.FatherName) || !string.IsNullOrWhiteSpace(req.MotherName))
            sb.AppendLine("父母名（避免同字重疊若可能）：父 " + req.FatherName + "／母 " + req.MotherName);
        if (req.Mode == "rename")
        {
            if (!string.IsNullOrWhiteSpace(req.CurrentFullName))
                sb.AppendLine("原名：" + req.CurrentFullName + "（請提出明顯不同且更美的新名）");
            if (!string.IsNullOrWhiteSpace(req.RenameReason))
                sb.AppendLine("改名原因：" + req.RenameReason);
            if (!string.IsNullOrWhiteSpace(req.ImproveDirections))
                sb.AppendLine("改善方向：" + req.ImproveDirections);
            if (!string.IsNullOrWhiteSpace(req.RenameFocus))
                sb.AppendLine("生活關注：" + req.RenameFocus);
        }

        if (pillars != null)
        {
            sb.AppendLine("八字軟參考（勿硬湊）：日主 " + pillars.DayMaster + pillars.DayMasterWuxing +
                "；喜用 " + string.Join("、", pillars.XiYong ?? new List<string>()) +
                "；忌 " + string.Join("、", pillars.JiShen ?? new List<string>()) +
                "；生肖 " + pillars.Zodiac);
        }

        if (!string.IsNullOrWhiteSpace(req.AestheticBrief))
            sb.AppendLine("使用者審美／風格描述（最重要）：" + req.AestheticBrief.Trim());
        else
            sb.AppendLine("若無特別審美描述：偏清雅有骨、現代書卷氣，避免俗氣與過度華麗。");

        sb.AppendLine("請直接輸出 JSON。");
        return sb.ToString();
    }

    private static List<string> ParseGivens(string content, string surname, bool allowSingle, bool allowDouble, int max)
    {
        var list = new List<string>();
        var seen = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(content)) return list;

        var text = content.Trim();
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
            var arr = obj["givens"] as JArray;
            if (arr != null)
            {
                foreach (var token in arr)
                    TryAdd(NormalizeGiven(token.ToString(), surname), allowSingle, allowDouble, list, seen, max);
            }
        }
        catch
        {
            foreach (Match m in Regex.Matches(content, "[\"']([\\u4e00-\\u9fff]{1,2})[\"']"))
                TryAdd(m.Groups[1].Value, allowSingle, allowDouble, list, seen, max);
        }

        return list;
    }

    private static string NormalizeGiven(string raw, string surname)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var s = raw.Trim().Replace(" ", "").Replace("　", "");
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (ch >= 0x4e00 && ch <= 0x9fff)
                sb.Append(ch);
        }
        s = sb.ToString();
        if (!string.IsNullOrEmpty(surname) && s.StartsWith(surname) && s.Length > surname.Length)
            s = s.Substring(surname.Length);
        return s;
    }

    private static void TryAdd(string given, bool allowSingle, bool allowDouble, List<string> list, HashSet<string> seen, int max)
    {
        if (list.Count >= max) return;
        if (string.IsNullOrEmpty(given)) return;
        if (given.Length == 1 && !allowSingle) return;
        if (given.Length == 2 && !allowDouble) return;
        if (given.Length < 1 || given.Length > 2) return;
        if (!seen.Add(given)) return;
        list.Add(given);
    }
}
}
