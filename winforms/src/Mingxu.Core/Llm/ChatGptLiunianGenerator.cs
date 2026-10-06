using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mingxu.Core.Models;
using Newtonsoft.Json.Linq;

namespace Mingxu.Core.Llm
{

public sealed class ChatGptLiunianResult
{
    public string Overview { get; set; }
    public string PhasesText { get; set; }
    public string TrendText { get; set; }
    public string ActionsText { get; set; }
    public List<YearLuck> Liunian { get; set; }
    public string Error { get; set; }
    public bool Ok
    {
        get { return Liunian != null && Liunian.Count > 0 && string.IsNullOrEmpty(Error); }
    }

    public ChatGptLiunianResult()
    {
        Overview = "";
        PhasesText = "";
        TrendText = "";
        ActionsText = "";
        Liunian = new List<YearLuck>();
        Error = "";
    }
}

/// <summary>
/// 流年分析服務：以本地干支／歲數為骨架，請 ChatGPT 撰寫各年解說與總覽。
/// </summary>
public static class ChatGptLiunianGenerator
{
    public static ChatGptLiunianResult Enrich(
        AnalysisRequest req,
        NameSuggestion sug,
        Pillars pillars,
        IList<YearLuck> localLiunian)
    {
        var result = new ChatGptLiunianResult();
        if (sug == null || string.IsNullOrWhiteSpace(sug.FullName))
        {
            result.Error = "缺少姓名，無法產生流年解說。";
            return result;
        }
        if (string.IsNullOrWhiteSpace(req.ChatGptApiKey))
        {
            result.Error = "未設定 ChatGPT API Key。";
            return result;
        }

        var years = (localLiunian ?? new List<YearLuck>()).ToList();
        if (years.Count == 0)
        {
            result.Error = "缺少流年骨架。";
            return result;
        }

        var system =
            "你是台灣命理流年／大運決策顧問。請以繁體中文撰寫客觀、理性、具建設性的分析。" +
            "立場是「知命不宿命」的決策輔助：幫助理解節奏與風險，而非宿命論。" +
            "禁止製造恐慌；禁止推銷開運商品、符咒、付費改運或任何商業療癒方案。" +
            "可柔和結合八字喜忌與姓名意象，避免絕對化與醫療／投資保證。嚴格輸出 JSON，勿加 markdown。";
        var user = BuildUserPrompt(req, sug, pillars, years);

        string error;
        var content = OpenAiChatClient.PostJson(
            req.ChatGptBaseUrl,
            req.ChatGptApiKey,
            req.ChatGptModel,
            system,
            user,
            0.7,
            out error);

        if (!string.IsNullOrEmpty(error))
        {
            result.Error = error;
            return result;
        }

        return Parse(content ?? "", years);
    }

    private static string BuildUserPrompt(
        AnalysisRequest req,
        NameSuggestion sug,
        Pillars pillars,
        List<YearLuck> years)
    {
        var n = years.Count;
        var y0 = years[0].Year;
        var y1 = years[years.Count - 1].Year;
        var sb = new StringBuilder();
        sb.AppendLine("這是「流年大運分析」任務。請依下列年份骨架，為姓名「" + sug.FullName + "」撰寫未來 " + n + " 年（" + y0 + "－" + y1 + "）解說。");
        sb.AppendLine();
        sb.AppendLine("【語氣與立場｜必須遵守】");
        sb.AppendLine("- 語氣：客觀、理性、具建設性、不製造恐慌。");
        sb.AppendLine("- 禁止推銷開運商品；強調「知命不宿命」的決策輔助立場。");
        sb.AppendLine("- 風險用「宜留意／建議防守」表述，避免恐嚇語氣。");
        sb.AppendLine();
        sb.AppendLine("【基本資料】");
        sb.AppendLine("性別：" + (req.Gender == "F" ? "女" : "男"));
        sb.AppendLine("出生：" + req.Birth.ToString("yyyy/MM/dd HH:mm") + "　" + req.BirthPlace);
        sb.AppendLine("分析年數：" + n);
        sb.AppendLine("姓名綜合評等：" + sug.Grade + "（" + sug.Total.ToString("0") + "）");
        sb.AppendLine("字義摘要：" + string.Join("；", (sug.Meanings ?? new List<string>()).Take(3)));
        if (pillars != null)
        {
            sb.AppendLine("日主 " + pillars.DayMaster + pillars.DayMasterWuxing +
                "（" + pillars.Strength + "）；生肖 " + pillars.Zodiac);
            sb.AppendLine("喜用：" + string.Join("、", pillars.XiYong ?? new List<string>()) +
                "；忌：" + string.Join("、", pillars.JiShen ?? new List<string>()));
        }
        sb.AppendLine();
        sb.AppendLine("【流年骨架｜不可改動 year／age／ganzhi】");
        foreach (var y in years)
        {
            sb.AppendLine("- year=" + y.Year + ", age=" + y.Age + ", ganzhi=" + (y.Ganzhi ?? "") +
                ", animal=" + (y.Animal ?? "") +
                ", dayun=" + (y.DaYun ?? "") +
                ", local_hint=" + Truncate(y.Summary, 50));
        }
        sb.AppendLine();
        sb.AppendLine("【必須產出的結構】");
        sb.AppendLine("1) overview：總覽（120～200字），說明整體節奏與決策輔助重點。");
        sb.AppendLine("2) phases：將這 " + n + " 年拆成 2～4 個主要運勢週期（如轉折期、耕耘期、收成期等），");
        sb.AppendLine("   每段含 name、from_year、to_year、mission（該階段的「時代任務」40～80字）。");
        sb.AppendLine("3) trend：動態趨勢與風險預警——");
        sb.AppendLine("   defend：需特別防守的年份陣列（year + reason，如破財、官符、人事衝突等，理性表述）；");
        sb.AppendLine("   attack：適合積極進攻的年份陣列（year + reason）；");
        sb.AppendLine("   summary：趨勢總結（60～100字）。");
        sb.AppendLine("4) actions：具體行動建議（條列）——");
        sb.AppendLine("   finance_dos / finance_donts（財務該做／不該做，各 3～5 條）；");
        sb.AppendLine("   career_dos / career_donts（事業該做／不該做，各 3～5 條）。");
        sb.AppendLine("5) liunian：每一年 level（佳／平偏佳／平／慎）、keyword、summary、overall、career、wealth、relationship、life；");
        sb.AppendLine("   必須對應骨架年份；禁止改 year／ganzhi／分數；流月數值一律沿用系統，勿自行計算。");
        sb.AppendLine();
        sb.AppendLine("只輸出 JSON：");
        sb.AppendLine("{");
        sb.AppendLine("  \"overview\":\"...\",");
        sb.AppendLine("  \"phases\":[{\"name\":\"耕耘期\",\"from_year\":2026,\"to_year\":2028,\"mission\":\"...\"}],");
        sb.AppendLine("  \"trend\":{\"defend\":[{\"year\":2027,\"reason\":\"...\"}],\"attack\":[{\"year\":2029,\"reason\":\"...\"}],\"summary\":\"...\"},");
        sb.AppendLine("  \"actions\":{\"finance_dos\":[\"...\"],\"finance_donts\":[\"...\"],\"career_dos\":[\"...\"],\"career_donts\":[\"...\"]},");
        sb.AppendLine("  \"liunian\":[{\"year\":2026,\"level\":\"平偏佳\",\"keyword\":\"穩中求進\",\"summary\":\"...\",\"overall\":\"...\",\"career\":\"...\",\"wealth\":\"...\",\"relationship\":\"...\",\"life\":\"...\"}]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static ChatGptLiunianResult Parse(string content, List<YearLuck> skeleton)
    {
        var result = new ChatGptLiunianResult();
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
            result.Overview = Norm((obj["overview"] ?? "").ToString());
            result.PhasesText = FormatPhases(obj["phases"] as JArray);
            result.TrendText = FormatTrend(obj["trend"] as JObject);
            result.ActionsText = FormatActions(obj["actions"] as JObject);

            var byYear = skeleton.ToDictionary(x => x.Year, x => CloneYear(x));
            var arr = obj["liunian"] as JArray;
            if (arr != null)
            {
                foreach (var token in arr)
                {
                    var row = token as JObject;
                    if (row == null) continue;
                    int year;
                    if (!int.TryParse((row["year"] ?? "").ToString(), out year)) continue;
                    YearLuck target;
                    if (!byYear.TryGetValue(year, out target)) continue;
                    var level = (row["level"] ?? "").ToString().Trim();
                    var summary = Norm((row["summary"] ?? "").ToString());
                    if (!string.IsNullOrWhiteSpace(level)) target.Level = level;
                    if (!string.IsNullOrWhiteSpace(summary)) target.Summary = summary;
                    else if (!string.IsNullOrWhiteSpace(level))
                        target.Summary = target.Year + "（" + target.Age + "歲・" + target.Ganzhi + "）〔" + level + "〕";
                    ApplyText(target, row, "keyword", v => target.Keyword = v);
                    ApplyText(target, row, "overall", v => target.Overall = v);
                    ApplyText(target, row, "career", v => target.Career = v);
                    ApplyText(target, row, "wealth", v => target.Wealth = v);
                    ApplyText(target, row, "relationship", v => target.Relationship = v);
                    ApplyText(target, row, "life", v => target.Life = v);
                    ApplyText(target, row, "outlook", v => target.Outlook = v);
                }
            }

            result.Liunian = byYear.Values.OrderBy(x => x.Year).ToList();
            if (result.Liunian.Count == 0)
                result.Error = "ChatGPT 有回應但缺少流年內容。";
            return result;
        }
        catch (Exception ex)
        {
            result.Error = "無法解析流年文案：" + ex.Message;
            return result;
        }
    }

    private static void ApplyText(YearLuck target, JObject row, string key, Action<string> set)
    {
        var v = Norm((row[key] ?? "").ToString());
        if (!string.IsNullOrWhiteSpace(v)) set(v);
    }

    private static string FormatPhases(JArray phases)
    {
        if (phases == null || phases.Count == 0) return "";
        var lines = new List<string>();
        foreach (var token in phases)
        {
            var row = token as JObject;
            if (row == null) continue;
            var name = (row["name"] ?? "").ToString().Trim();
            var from = (row["from_year"] ?? "").ToString().Trim();
            var to = (row["to_year"] ?? "").ToString().Trim();
            var mission = Norm((row["mission"] ?? "").ToString());
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(mission)) continue;
            var span = from;
            if (!string.IsNullOrWhiteSpace(to) && to != from) span = from + "－" + to;
            lines.Add("・" + name + (string.IsNullOrWhiteSpace(span) ? "" : "（" + span + "）") + "：" + mission);
        }
        return string.Join("\n", lines);
    }

    private static string FormatTrend(JObject trend)
    {
        if (trend == null) return "";
        var lines = new List<string>();
        var defend = trend["defend"] as JArray;
        var attack = trend["attack"] as JArray;
        if (defend != null && defend.Count > 0)
        {
            lines.Add("宜防守：");
            foreach (var token in defend)
            {
                var row = token as JObject;
                if (row == null) continue;
                lines.Add("・" + (row["year"] ?? "") + "：" + Norm((row["reason"] ?? "").ToString()));
            }
        }
        if (attack != null && attack.Count > 0)
        {
            lines.Add("可積極：");
            foreach (var token in attack)
            {
                var row = token as JObject;
                if (row == null) continue;
                lines.Add("・" + (row["year"] ?? "") + "：" + Norm((row["reason"] ?? "").ToString()));
            }
        }
        var summary = Norm((trend["summary"] ?? "").ToString());
        if (!string.IsNullOrWhiteSpace(summary))
            lines.Add(summary);
        return string.Join("\n", lines);
    }

    private static string FormatActions(JObject actions)
    {
        if (actions == null) return "";
        var lines = new List<string>();
        AppendList(lines, "財務・該做", actions["finance_dos"] as JArray);
        AppendList(lines, "財務・不該做", actions["finance_donts"] as JArray);
        AppendList(lines, "事業・該做", actions["career_dos"] as JArray);
        AppendList(lines, "事業・不該做", actions["career_donts"] as JArray);
        return string.Join("\n", lines);
    }

    private static void AppendList(List<string> lines, string title, JArray arr)
    {
        if (arr == null || arr.Count == 0) return;
        lines.Add(title + "：");
        foreach (var token in arr)
        {
            var s = Norm(token.ToString());
            if (!string.IsNullOrWhiteSpace(s))
                lines.Add("・" + s);
        }
    }

    private static YearLuck CloneYear(YearLuck src)
    {
        return new YearLuck
        {
            Year = src.Year,
            Age = src.Age,
            Ganzhi = src.Ganzhi,
            Animal = src.Animal,
            BaziScore = src.BaziScore,
            NameScore = src.NameScore,
            ZodiacScore = src.ZodiacScore,
            TotalScore = src.TotalScore,
            CareerScore = src.CareerScore,
            WealthScore = src.WealthScore,
            RelationshipScore = src.RelationshipScore,
            Level = src.Level,
            Keyword = src.Keyword,
            Summary = src.Summary,
            DaYun = src.DaYun,
            BaziNote = src.BaziNote,
            NameNote = src.NameNote,
            ZodiacNote = src.ZodiacNote,
            Overall = src.Overall,
            Career = src.Career,
            Wealth = src.Wealth,
            Relationship = src.Relationship,
            Life = src.Life,
            Suitable = src.Suitable == null ? new List<string>() : new List<string>(src.Suitable),
            Avoid = src.Avoid == null ? new List<string>() : new List<string>(src.Avoid),
            Risks = src.Risks == null ? new List<string>() : new List<string>(src.Risks),
            Advice = src.Advice == null ? new List<string>() : new List<string>(src.Advice),
            Outlook = src.Outlook,
            MonthGuideCareer = src.MonthGuideCareer,
            MonthGuideWealth = src.MonthGuideWealth,
            MonthGuideRelationship = src.MonthGuideRelationship,
            StrongMonths = src.StrongMonths == null ? new List<int>() : new List<int>(src.StrongMonths),
            StableMonths = src.StableMonths == null ? new List<int>() : new List<int>(src.StableMonths),
            AdjustMonths = src.AdjustMonths == null ? new List<int>() : new List<int>(src.AdjustMonths),
            CautionMonths = src.CautionMonths == null ? new List<int>() : new List<int>(src.CautionMonths),
            Months = src.Months == null ? new List<MonthLuck>() : new List<MonthLuck>(src.Months),
        };
    }

    private static string Norm(string s)
    {
        return (s ?? "").Replace("\\n", "\n").Replace("\r\n", "\n").Trim();
    }

    private static string Truncate(string s, int n)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= n) return s ?? "";
        return s.Substring(0, n) + "…";
    }
}
}
