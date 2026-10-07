using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mingxu.Core.Models;
using Newtonsoft.Json.Linq;

namespace Mingxu.Core.Llm
{

public sealed class ChatGptNewbornReportResult
{
    public string Idea { get; set; }
    public string Blessing { get; set; }
    public string Hope { get; set; }
    /// <summary>五格整體判讀：說明單格吉凶與綜合評等為何可並存。</summary>
    public string WugeReading { get; set; }
    public List<YearLuck> Liunian { get; set; }
    public ChatGptCiPoemResult Poem { get; set; }
    public string Error { get; set; }
    public bool HasCopy
    {
        get
        {
            return !string.IsNullOrWhiteSpace(Idea)
                || !string.IsNullOrWhiteSpace(Blessing)
                || !string.IsNullOrWhiteSpace(Hope)
                || !string.IsNullOrWhiteSpace(WugeReading)
                || (Liunian != null && Liunian.Count > 0);
        }
    }

    public ChatGptNewbornReportResult()
    {
        Idea = "";
        Blessing = "";
        Hope = "";
        WugeReading = "";
        Liunian = new List<YearLuck>();
        Poem = new ChatGptCiPoemResult();
        Error = "";
    }
}

/// <summary>
/// 新生兒命名／專業改名 PDF 匯出：一次 API 產生流年文案、理念、祝福、人生期許，以及嵌名宋詞。
/// </summary>
public static class ChatGptNewbornReportGenerator
{
    public static ChatGptNewbornReportResult Generate(
        AnalysisRequest req,
        NameSuggestion sug,
        Pillars pillars,
        IList<YearLuck> localLiunian,
        string parents)
    {
        var result = new ChatGptNewbornReportResult();
        if (sug == null || string.IsNullOrWhiteSpace(sug.FullName))
        {
            result.Error = "缺少姓名，無法產生報告文案。";
            return result;
        }

        var isRename = req != null && req.Mode == "rename";
        var fullName = sug.FullName.Trim();
        var chars = fullName.Where(ch => ch >= 0x4e00 && ch <= 0x9fff).Select(ch => ch.ToString()).ToList();
        var years = (localLiunian ?? new List<YearLuck>()).Take(10).ToList();
        var system = isRename
            ? "你是台灣專業改名顧問與古典詞人。請以繁體中文撰寫莊重、溫暖、可交給當事人閱讀的改名報告文案。" +
              "語氣正向但不浮誇；流年可含溫和提醒，避免恐嚇。嚴格輸出 JSON，勿加 markdown。"
            : "你是台灣新生兒命名顧問與古典詞人。請以繁體中文撰寫溫暖、典雅、可公開給家長的報告文案。" +
              "語氣正向但不浮誇；流年可含溫和提醒，避免恐嚇。嚴格輸出 JSON，勿加 markdown。";
        var user = BuildUserPrompt(fullName, chars, sug, pillars, req, years, parents, isRename);

        string error;
        var content = OpenAiChatClient.PostJson(
            req.ChatGptBaseUrl,
            req.ChatGptApiKey,
            req.ChatGptModel,
            system,
            user,
            0.8,
            out error);

        if (!string.IsNullOrEmpty(error))
        {
            result.Error = error;
            return result;
        }

        return Parse(content ?? "", years, fullName);
    }

    private static string BuildUserPrompt(
        string fullName,
        List<string> chars,
        NameSuggestion sug,
        Pillars pillars,
        AnalysisRequest req,
        List<YearLuck> years,
        string parents,
        bool isRename)
    {
        var charList = chars.Count == 0 ? fullName : string.Join("」、「", chars);
        var sb = new StringBuilder();
        if (isRename)
        {
            sb.AppendLine("這是「專業改名」報告文案任務。建議新名：「" + fullName + "」。");
            sb.AppendLine("請一次產出：未來十年流年解說、五格整體判讀、改名理念、祝福、人生期許，以及一首嵌名宋詞（含賞析）。");
        }
        else
        {
            sb.AppendLine("這是「新生兒命名」報告文案任務。姓名：「" + fullName + "」。");
            sb.AppendLine("請一次產出：未來十年流年解說、五格整體判讀、命名理念、祝福、人生期許，以及一首嵌名宋詞（含賞析）。");
        }
        sb.AppendLine();
        sb.AppendLine("【基本資料】");
        sb.AppendLine("性別：" + (req != null && req.Gender == "F" ? "女" : "男"));
        sb.AppendLine("出生：" + (req == null ? "" : req.Birth.ToString("yyyy/MM/dd HH:mm")) +
            "　" + (req == null ? "" : req.BirthPlace));
        if (!string.IsNullOrWhiteSpace(parents))
            sb.AppendLine("父母／家長：" + parents);
        if (isRename)
        {
            if (req != null && !string.IsNullOrWhiteSpace(req.CurrentFullName))
                sb.AppendLine("原名：" + req.CurrentFullName.Trim());
            if (req != null && !string.IsNullOrWhiteSpace(req.RenameReason))
                sb.AppendLine("改名原因：" + req.RenameReason.Trim());
            if (req != null && !string.IsNullOrWhiteSpace(req.ImproveDirections))
                sb.AppendLine("改善方向：" + req.ImproveDirections.Trim());
            if (req != null && !string.IsNullOrWhiteSpace(req.RenameFocus))
                sb.AppendLine("生活關注：" + req.RenameFocus.Trim());
            if (!string.IsNullOrWhiteSpace(sug.ComparisonText))
                sb.AppendLine("原名對照摘要：" + Truncate(sug.ComparisonText.Replace("\n", " "), 160));
        }
        sb.AppendLine("綜合評等：" + sug.Grade + "（" + sug.Total.ToString("0") + "）");
        sb.AppendLine("字義／理由摘要：" + string.Join("；", (sug.Meanings ?? new List<string>()).Take(4)));
        if (pillars != null)
        {
            sb.AppendLine("日主 " + pillars.DayMaster + pillars.DayMasterWuxing +
                "（" + pillars.Strength + "）；生肖 " + pillars.Zodiac);
            sb.AppendLine("喜用：" + string.Join("、", pillars.XiYong ?? new List<string>()) +
                "；忌：" + string.Join("、", pillars.JiShen ?? new List<string>()));
        }
        if (sug.Wuge != null)
        {
            var w = sug.Wuge;
            sb.AppendLine("【三才五格｜請據此撰寫 wuge_reading，數字與吉凶不可改動】");
            sb.AppendLine("三才：" + FormatSancaiDots(w) + "（" + (w.SancaiLuck ?? "") + "）；關係：" + (w.SancaiNote ?? ""));
            sb.AppendLine("天格 " + w.Tian + "・" + (w.TianLuck ?? "") +
                "；人格 " + w.Ren + "・" + (w.RenLuck ?? "") +
                "；地格 " + w.Di + "・" + (w.DiLuck ?? "") +
                "；外格 " + w.Wai + "・" + (w.WaiLuck ?? "") +
                "；總格 " + w.Zong + "・" + (w.ZongLuck ?? ""));
        }
        if (req != null && !string.IsNullOrWhiteSpace(req.AestheticBrief))
            sb.AppendLine("審美偏好：" + req.AestheticBrief.Trim());
        sb.AppendLine();
        sb.AppendLine("【流年骨架｜請依下列年份撰寫，不可改動 year／age／ganzhi】");
        foreach (var y in years)
        {
            sb.AppendLine("- year=" + y.Year + ", age=" + y.Age + ", ganzhi=" + (y.Ganzhi ?? "") +
                ", animal=" + (y.Animal ?? "") + ", local_hint=" + Truncate(y.Summary, 40));
        }
        if (isRename)
            sb.AppendLine("每一年給 level（佳／平偏佳／平／慎 四選一）與 summary（40～90字，結合新名意象、成年階段與改名後的生活方向，可柔和參考命理，勿堆砌術語）。");
        else
            sb.AppendLine("每一年給 level（佳／平偏佳／平／慎 四選一）與 summary（40～90字，結合姓名意象與成長階段，可柔和參考命理，勿堆砌術語）。");
        sb.AppendLine();
        sb.AppendLine("【五格整體判讀】（JSON 欄位 wuge_reading，兩段、共 120～220 字）");
        sb.AppendLine("目的：預答家長疑問——「某格（尤其總格）為凶／需留意時，為何綜合評等仍可為上佳／良好？」");
        sb.AppendLine("第一段：說明三才配置（用「・」分隔五行）與天人／人地關係，並概述各格表現；若總格為凶或大凶，須點出總格數與「需留意格」，語氣溫和、勿恐嚇。");
        sb.AppendLine("第二段：明確說明本名並非單以五格吉凶判斷，而是綜合出生八字、喜用方向、字義、音韻、三才與整體名字使用感後評估；可呼應綜合評等「" + sug.Grade + "」。");
        sb.AppendLine("範例語氣：「此名三才配置為土・金・水，天人相生、人地相生；人格與外格表現良好，地格為中性，總格 34 為本派數理中的需留意格。因此，本名並非單以五格吉凶判斷，而是綜合出生八字、喜用方向、字義、音韻、三才與整體名字使用感後評估。」");
        sb.AppendLine("勿輸出分數數字（綜合評等文字可保留）；勿改動格位吉凶與總格數。");
        sb.AppendLine();
        if (isRename)
        {
            sb.AppendLine("【改名理念】（JSON 欄位仍用 idea）說明為何建議改為此名、相對原名的提升，結合字義、音韻與改名原因，120～200字。");
            sb.AppendLine("【祝福】給當事人的祝福與鼓勵，80～140字。");
            sb.AppendLine("【人生期許】展望改名後的品格、志向、處世，80～140字。");
        }
        else
        {
            sb.AppendLine("【命名理念】說明為何此名適合此兒，結合字義、音韻、祝福方向，120～200字。");
            sb.AppendLine("【祝福】給家長與孩子的祝福語，80～140字。");
            sb.AppendLine("【人生期許】展望品格、志向、處世，80～140字。");
        }
        sb.AppendLine();
        sb.AppendLine("【嵌名宋詞】");
        sb.AppendLine("請以「" + fullName + "」為核心，創作一首宋詞（詞名自訂），將名字自然嵌於詞中，並具備深遠寓意。");
        sb.AppendLine("意境與寓意：結合古典意象（如微風、明月、遠山），傳達歷經風雨後內心依然澄澈、豁達的堅韌。");
        sb.AppendLine("結構與格律：符合詞牌格律與押韻規範。");
        sb.AppendLine("嵌字巧思：請將「" + charList + "」分別巧妙融入詞中，不可生硬拼湊。");
        sb.AppendLine("賞析說明：附一段白話文賞析，說明如何呼應名字與寓意。");
        sb.AppendLine();
        sb.AppendLine("只輸出 JSON：");
        sb.AppendLine("{");
        sb.AppendLine("  \"wuge_reading\":\"五格整體判讀兩段文字\",");
        sb.AppendLine("  \"idea\":\"" + (isRename ? "改名理念" : "命名理念") + "\",");
        sb.AppendLine("  \"blessing\":\"祝福\",");
        sb.AppendLine("  \"hope\":\"人生期許\",");
        sb.AppendLine("  \"liunian\":[{\"year\":2026,\"age\":5,\"ganzhi\":\"丙午\",\"level\":\"平偏佳\",\"summary\":\"...\"}],");
        sb.AppendLine("  \"title\":\"詞名\",\"cipai\":\"\",\"lyric\":\"詞全文\",\"appreciation\":\"賞析\"");
        sb.AppendLine("cipai 僅填真實詞牌；無詞牌時填空字串，勿填「無則空字串」等說明文字。");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string FormatSancaiDots(WugeResult w)
    {
        if (w == null) return "";
        if (!string.IsNullOrEmpty(w.TianWx) && !string.IsNullOrEmpty(w.RenWx) && !string.IsNullOrEmpty(w.DiWx))
            return w.TianWx + "・" + w.RenWx + "・" + w.DiWx;
        var s = w.Sancai ?? "";
        if (s.Length >= 3)
            return s[0] + "・" + s[1] + "・" + s[2];
        return s;
    }

    private static ChatGptNewbornReportResult Parse(string content, List<YearLuck> skeleton, string fullName)
    {
        var result = new ChatGptNewbornReportResult();
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
            result.WugeReading = Norm((obj["wuge_reading"] ?? obj["wugeReading"] ?? "").ToString());
            result.Idea = Norm((obj["idea"] ?? "").ToString());
            result.Blessing = Norm((obj["blessing"] ?? "").ToString());
            result.Hope = Norm((obj["hope"] ?? "").ToString());

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
                    if (!byYear.TryGetValue(year, out target))
                    {
                        // 若模型多給年份，仍接受但補骨架
                        target = new YearLuck { Year = year };
                        int age;
                        int.TryParse((row["age"] ?? "").ToString(), out age);
                        target.Age = age;
                        target.Ganzhi = (row["ganzhi"] ?? "").ToString();
                        byYear[year] = target;
                    }
                    var level = (row["level"] ?? "").ToString().Trim();
                    var summary = Norm((row["summary"] ?? "").ToString());
                    if (!string.IsNullOrWhiteSpace(level)) target.Level = level;
                    if (!string.IsNullOrWhiteSpace(summary))
                        target.Summary = summary;
                    else if (!string.IsNullOrWhiteSpace(level))
                        target.Summary = target.Year + "（" + target.Age + "歲・" + target.Ganzhi + "）〔" + level + "〕";
                }
            }
            result.Liunian = byYear.Values.OrderBy(x => x.Year).ToList();

            var poem = new ChatGptCiPoemResult();
            poem.Title = (obj["title"] ?? "").ToString().Trim();
            poem.Cipai = (obj["cipai"] ?? "").ToString().Trim();
            poem.Lyric = Norm((obj["lyric"] ?? "").ToString());
            poem.Appreciation = Norm((obj["appreciation"] ?? "").ToString());
            if (!string.IsNullOrWhiteSpace(poem.Lyric))
            {
                var psb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(poem.Title))
                    psb.AppendLine("《" + poem.Title + "》");
                // 詞牌一律不顯示於報告
                psb.AppendLine(poem.Lyric);
                if (!string.IsNullOrWhiteSpace(poem.Appreciation))
                {
                    psb.AppendLine();
                    psb.AppendLine("【賞析】");
                    psb.Append(poem.Appreciation);
                }
                poem.DisplayText = ChatGptCiPoemGenerator.SanitizePoemDisplay(psb.ToString());
            }
            result.Poem = poem;

            if (!result.HasCopy && !result.Poem.Ok)
                result.Error = "ChatGPT 有回應但缺少可用文案。";
            return result;
        }
        catch (Exception ex)
        {
            result.Error = "無法解析報告文案：" + ex.Message;
            return result;
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
