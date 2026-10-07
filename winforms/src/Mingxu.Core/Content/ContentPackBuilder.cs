using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;
using Mingxu.Core.Scoring;

namespace Mingxu.Core.Content
{

public static class ContentPackBuilder
{
    private static readonly Dictionary<string, string[]> WxImage = new Dictionary<string, string[]>
    {
        ["木"] = new[] { "成長", "生發", "青枝向榮" },
        ["火"] = new[] { "光明", "熱誠", "華彩煥發" },
        ["土"] = new[] { "厚德", "穩載", "大地含章" },
        ["金"] = new[] { "清正", "鋒芒", "金玉其質" },
        ["水"] = new[] { "智慧", "涵養", "清流通達" },
    };

    private static readonly Dictionary<string, string[]> Qiyan = new Dictionary<string, string[]>
    {
        ["木"] = new[] { "青枝向日自亭亭", "雨過新芽處處生", "願汝長年懷遠志", "春風一路伴前程" },
        ["火"] = new[] { "朝陽初起映華堂", "燈火溫柔照遠方", "願汝心光常不熄", "前程錦繡自芬芳" },
        ["土"] = new[] { "厚土含章養百嘉", "山川有信自成家", "願汝腳踏實地走", "德澤綿長護歲華" },
        ["金"] = new[] { "金風玉露洗塵襟", "清響一聲天地心", "願汝立身如朗月", "是非明辨自堪任" },
        ["水"] = new[] { "清波映月自澄明", "江海無聲育性情", "願汝心懷千里志", "源流不竭潤生平" },
    };

    public static Dictionary<string, string> Build(NameSuggestion sug, Pillars pillars, string parents = "", string serviceType = "newborn")
    {
        var title = serviceType == "rename" ? "改名報告" : "命名報告";
        var day = DateTime.Now.ToString("yyyy年MM月dd日");
        var chars = string.Join("；", sug.Meanings.Select((m, i) =>
        {
            var ch = i < sug.Given.Length ? sug.Given[i].ToString() : "";
            return string.IsNullOrEmpty(ch) ? m : ch + "：" + m;
        }));
        var combo = "「" + sug.FullName + "」綜合評等" + sug.Grade + "。";
        var dims = NameScorer.DimensionScores(sug);
        if (dims.Count > 0)
            combo += "各面向：" + string.Join("、", dims.Select(kv => kv.Key + NameScorer.GradeLabel(kv.Value))) + "。";
        var destiny = pillars == null
            ? ""
            : "日主" + pillars.DayMaster + pillars.DayMasterWuxing + "（" + pillars.Strength + "），喜用" + string.Join("、", pillars.XiYong) + "。";
        var primaryWx = PrimaryWx(sug, pillars);
        string[] imageTags;
        if (!WxImage.TryGetValue(primaryWx, out imageTags))
            imageTags = new[] { "清和", "安定", "長遠" };
        var image = "意象偏向" + string.Join("、", imageTags) + "。";
        if (sug.Wuge != null)
            image += "三才" + sug.Wuge.Sancai + "（" + sug.Wuge.SancaiLuck + "），總格" + sug.Wuge.Zong + "。";
        var story = BuildStory(sug, pillars, primaryWx);
        var idea = "取名兼顧命理喜用、音韻、字義與組合美感，使名字可長久使用、值得珍藏。";
        if (!string.IsNullOrWhiteSpace(sug.ComparisonText))
            idea += "\n" + sug.ComparisonText;
        var blessing = string.IsNullOrWhiteSpace(parents)
            ? "願「" + sug.FullName + "」一生平安順遂，志向得伸。"
            : "願父母（" + parents + "）祝福「" + sug.FullName + "」一生平安順遂，志向得伸。";
        var hope = "願此名成為人生序章，承載祝福與方向。";
        var poem = BuildPoem(primaryWx, sug.Grade);
        var draft = "【手寫稿建議】\n正楷書「" + sug.FullName + "」；署名可採" + primaryWx + "行之氣韻（" + string.Join("、", imageTags) + "）。";
        var wugeReading = BuildWugeReading(sug);

        return new Dictionary<string, string>
        {
            ["report_title"] = title,
            ["chars"] = string.IsNullOrWhiteSpace(chars) ? sug.ReasonText : chars,
            ["combo"] = combo,
            ["destiny"] = destiny,
            ["image"] = image,
            ["story"] = story,
            ["idea"] = idea,
            ["blessing"] = blessing,
            ["hope"] = hope,
            ["wuge_reading"] = wugeReading,
            ["poem"] = poem,
            ["handwriting_draft"] = draft,
            ["report_text"] =
                "《" + title + "　" + sug.FullName + "》\n命名日期：" + day + "\n\n" +
                "【命名解析】\n" + chars + "\n" + combo + "\n" + destiny + "\n\n" +
                "【五格整體判讀】\n" + wugeReading + "\n\n" +
                "【意象】\n" + image + "\n\n【命名故事】\n" + story + "\n\n" +
                "【命名理念】\n" + idea + "\n\n【祝福】\n" + blessing + "\n\n【人生期許】\n" + hope +
                "\n\n【七言詩】\n" + poem + "\n\n" + draft + "\n",
        };
    }

    /// <summary>本地備援：五格整體判讀（API 失敗或未設定時使用）。</summary>
    public static string BuildWugeReading(NameSuggestion sug)
    {
        if (sug == null || sug.Wuge == null) return "";
        var w = sug.Wuge;
        var sancai = !string.IsNullOrEmpty(w.TianWx) && !string.IsNullOrEmpty(w.RenWx) && !string.IsNullOrEmpty(w.DiWx)
            ? w.TianWx + "・" + w.RenWx + "・" + w.DiWx
            : (w.Sancai ?? "");
        var rel = (w.SancaiNote ?? "").Replace("；", "、").Trim();
        if (string.IsNullOrEmpty(rel)) rel = "天人、人地關係見三才配置";

        var mid = new List<string>();
        AppendGePhrase(mid, "天格", w.Tian, w.TianLuck);
        AppendGePhrase(mid, "人格", w.Ren, w.RenLuck);
        AppendGePhrase(mid, "地格", w.Di, w.DiLuck);
        AppendGePhrase(mid, "外格", w.Wai, w.WaiLuck);
        AppendGePhrase(mid, "總格", w.Zong, w.ZongLuck);

        var p1 = "此名三才配置為" + sancai;
        if (!string.IsNullOrEmpty(rel)) p1 += "，" + rel;
        if (mid.Count > 0) p1 += "；" + string.Join("，", mid);
        p1 += "。";

        var grade = string.IsNullOrWhiteSpace(sug.Grade) ? NameScorer.GradeLabel(sug.Total) : sug.Grade;
        var p2 = "因此，本名並非單以五格吉凶判斷，而是綜合出生八字、喜用方向、字義、音韻、三才與整體名字使用感後評估"
            + "（綜合評等" + grade + "）。";
        return p1 + "\n" + p2;
    }

    private static void AppendGePhrase(List<string> parts, string name, int num, string luck)
    {
        luck = (luck ?? "").Trim();
        if (luck == "凶" || luck == "大凶")
            parts.Add(name + " " + num + " 為本派數理中的需留意格");
        else if (luck == "中性" || luck == "中")
            parts.Add(name + "為中性");
        else if (luck == "次吉")
            parts.Add(name + "尚可");
        else if (luck == "吉" || luck == "大吉")
            parts.Add(name + "表現良好");
        else if (!string.IsNullOrEmpty(luck))
            parts.Add(name + "為" + luck);
    }

    private static string PrimaryWx(NameSuggestion sug, Pillars pillars)
    {
        if (sug.CharWuxing != null && sug.CharWuxing.Count > 0 && !string.IsNullOrEmpty(sug.CharWuxing[0]))
            return sug.CharWuxing[0];
        if (pillars != null && pillars.XiYong != null && pillars.XiYong.Count > 0)
            return pillars.XiYong[0];
        return "木";
    }

    private static string BuildStory(NameSuggestion sug, Pillars pillars, string primaryWx)
    {
        var parts = new List<string>();
        parts.Add("「" + sug.FullName + "」以" + primaryWx + "行之氣為骨，評等" + sug.Grade + "。");
        if (pillars != null && pillars.XiYong != null && pillars.XiYong.Count > 0)
            parts.Add("對照命盤喜用「" + string.Join("、", pillars.XiYong) + "」，使名字與命局相互呼應。");
        if (sug.Reasons != null && sug.Reasons.Count > 0)
            parts.Add(string.Join("；", sug.Reasons.Take(3)) + "。");
        // 父母合參細節改由報告「父母姓名合參」區塊呈現，此處不重複薄弱一句。
        return string.Join("", parts);
    }

    private static string BuildPoem(string wx, string grade)
    {
        string[] lines;
        if (!Qiyan.TryGetValue(wx, out lines))
            lines = Qiyan["木"];
        return string.Join("\n", lines) + "\n（評等：" + grade + "）";
    }
}
}
