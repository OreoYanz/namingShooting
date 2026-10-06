using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mingxu.Core.Models;

namespace Mingxu.Core.Scoring
{
public static class AdultRename
{
    public const double DirectionBoost = 0.18;
    private const double ImproveThreshold = 3.0;
    private const double DeclineThreshold = -3.0;

    public static readonly string[] RenameReasons =
    {
        "字義不雅／粗俗", "諧音不佳", "命理不合", "五格不吉", "音韻不順",
        "過於常見（菜市場名）", "職場形象／專業感", "性別氣質不符", "其他特殊原因"
    };

    public static readonly string[] ImproveOptions =
    {
        "命理契合", "五行流通", "五格吉數", "音韻流暢",
        "字義寓意", "現代感", "辨識度／稀有", "整體綜合"
    };

    public static readonly Dictionary<string, string> ImproveDirection =
        new Dictionary<string, string>
        {
            ["命理契合"] = "bazi",
            ["五行流通"] = "wuxing",
            ["五格吉數"] = "wuge",
            ["音韻流暢"] = "phonology",
            ["字義寓意"] = "meaning",
            ["現代感"] = "style",
            ["辨識度／稀有"] = "rarity",
            ["整體綜合"] = "total"
        };

    private static readonly Dictionary<string, string> Labels =
        new Dictionary<string, string>
        {
            ["bazi"] = "命理", ["wuxing"] = "五行", ["wuge"] = "五格",
            ["phonology"] = "音韻", ["meaning"] = "字義", ["zodiac"] = "生肖",
            ["style"] = "風格", ["rarity"] = "辨識度", ["total"] = "整體"
        };

    private static readonly Dictionary<string, int> LuckRank =
        new Dictionary<string, int>
        {
            ["大吉"] = 5, ["吉"] = 4, ["平"] = 3, ["凶"] = 2, ["大凶"] = 1
        };

    public static List<string> ParseImproveDirections(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        return raw.Replace("；", ",").Replace("、", ",")
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(ImproveDirection.ContainsKey)
            .Distinct()
            .ToList();
    }

    public static string FormatRenameDirections(string renameReason, IEnumerable<string> directions, string renameFocus)
    {
        var dirs = NormalizeDirections(directions);
        var lines = new List<string> { "本次改名主要方向：" };
        if (!string.IsNullOrWhiteSpace(renameReason))
            lines.Add("改名原因：" + renameReason.Trim());
        var circles = new[] { "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧" };
        for (var i = 0; i < dirs.Count && i < circles.Length; i++)
            lines.Add(circles[i] + " " + dirs[i]);
        if (dirs.Count == 0)
            lines.Add("（未勾選改善方向，將依綜合表現排序）");
        if (!string.IsNullOrWhiteSpace(renameFocus))
            lines.Add("生活關注：" + renameFocus.Trim());
        return string.Join("\n", lines);
    }

    public static CurrentNameDiagnosis DiagnoseCurrentName(NameSuggestion sug, string renameReason)
    {
        var diagnosis = new CurrentNameDiagnosis();
        if (sug == null) return diagnosis;

        diagnosis.FullName = sug.FullName;
        diagnosis.Total = sug.Total;
        var dims = Dimensions(sug).Where(x => x.Key != "zodiac" && x.Key != "total").ToList();
        diagnosis.Strengths = dims.OrderByDescending(x => x.Value).Where(x => x.Value >= 75).Take(3)
            .Select(x => Labels[x.Key] + "表現良好（" + x.Value.ToString("0") + "）").ToList();
        diagnosis.Weaknesses = dims.OrderBy(x => x.Value).Where(x => x.Value < 58).Take(3)
            .Select(x => Labels[x.Key] + "偏弱，仍有調整空間（" + x.Value.ToString("0") + "）").ToList();
        if (diagnosis.Weaknesses.Count < 2)
        {
            foreach (var dim in dims.OrderBy(x => x.Value).Where(x => x.Value < 65))
            {
                var item = Labels[dim.Key] + "仍有調整空間（" + dim.Value.ToString("0") + "）";
                if (!diagnosis.Weaknesses.Contains(item)) diagnosis.Weaknesses.Add(item);
                if (diagnosis.Weaknesses.Count >= 3) break;
            }
        }
        if (diagnosis.Strengths.Count == 0) diagnosis.Strengths.Add("各維度落差不大");
        if (diagnosis.Weaknesses.Count == 0) diagnosis.Weaknesses.Add("尚無明顯短板，改名宜重風格或辨識度");

        diagnosis.Summary = sug.Total >= 80 ? "整體尚可，若改名宜鎖定明確改善點。"
            : sug.Total >= 65 ? "中等表現，具有針對性改名空間。"
            : "綜合偏低，建議依改善方向優先調整。";

        diagnosis.ReportText = EvaluateCurrentName(sug, renameReason);
        return diagnosis;
    }

    public static string EvaluateCurrentName(NameSuggestion sug, string renameReason)
    {
        if (sug == null) return "";
        var dims = Dimensions(sug).Where(x => x.Key != "zodiac" && x.Key != "total").ToList();
        var strengths = dims.OrderByDescending(x => x.Value).Where(x => x.Value >= 75).Take(3)
            .Select(x => Labels[x.Key] + "表現良好（" + x.Value.ToString("0") + "）").ToList();
        var weaknesses = dims.OrderBy(x => x.Value).Where(x => x.Value < 58).Take(3)
            .Select(x => Labels[x.Key] + "偏弱（" + x.Value.ToString("0") + "）").ToList();
        if (weaknesses.Count < 2)
        {
            foreach (var dim in dims.OrderBy(x => x.Value).Where(x => x.Value < 65))
            {
                var item = Labels[dim.Key] + "偏弱（" + dim.Value.ToString("0") + "）";
                if (!weaknesses.Contains(item)) weaknesses.Add(item);
                if (weaknesses.Count >= 3) break;
            }
        }
        if (strengths.Count == 0) strengths.Add("各維度落差不大");
        if (weaknesses.Count == 0) weaknesses.Add("尚無明顯短板，改名宜重風格或辨識度");

        var summary = sug.Total >= 80 ? "整體尚可，若改名宜鎖定明確改善點。"
            : sug.Total >= 65 ? "中等表現，具有針對性改名空間。"
            : "綜合偏低，建議依改善方向優先調整。";
        var focus = ReasonFocus(renameReason);
        if (focus.Count == 0)
        {
            foreach (var dim in dims.OrderBy(x => x.Value).Where(x => x.Value < 65))
            {
                var mapped = ImproveDirection.FirstOrDefault(x => x.Value == dim.Key).Key;
                if (!string.IsNullOrEmpty(mapped) && !focus.Contains(mapped)) focus.Add(mapped);
                if (focus.Count >= 3) break;
            }
        }

        var lines = new List<string>
        {
            "① 原名診斷",
            "原名：" + sug.FullName,
            "原名整體：" + sug.Total.ToString("0") + "　" + summary,
            "",
            "優勢：",
        };
        foreach (var s in strengths) lines.Add("・" + s);
        lines.Add("");
        lines.Add("可改善：");
        foreach (var w in weaknesses) lines.Add("・" + w);
        lines.Add("");
        lines.Add("八字 " + sug.BaziScore.ToString("0") + "　五行 " + sug.WuxingScore.ToString("0") +
            "　五格 " + sug.WugeScore.ToString("0"));
        lines.Add("音韻 " + sug.PhonologyScore.ToString("0") + "　字義 " + sug.MeaningScore.ToString("0") +
            "　生肖 " + sug.ZodiacScore.ToString("0"));
        lines.Add("風格 " + sug.StyleScore.ToString("0") + "　辨識度 " + sug.RarityScore.ToString("0"));
        if (sug.Wuge != null)
        {
            var w = sug.Wuge;
            lines.Add("");
            lines.Add("原名三才五格：");
            lines.Add("天格 " + FormatGridCell(w.Tian, w.TianWx, w.TianLuck) +
                "　人格 " + FormatGridCell(w.Ren, w.RenWx, w.RenLuck) +
                "　地格 " + FormatGridCell(w.Di, w.DiWx, w.DiLuck));
            lines.Add("外格 " + FormatGridCell(w.Wai, w.WaiWx, w.WaiLuck) +
                "　總格 " + FormatGridCell(w.Zong, w.ZongWx, w.ZongLuck));
            lines.Add("三才 " + w.Sancai + "（" + w.SancaiLuck + "）　五格分 " + w.Score.ToString("0.0"));
        }
        if (focus.Count > 0) lines.Add("對應改名焦點：" + string.Join("、", focus));
        return string.Join("\n", lines);
    }

    /// <summary>建立結構化前後比較（數值皆來自既有 NameSuggestion／WugeResult）。</summary>
    public static RenameComparison BuildRenameComparison(
        NameSuggestion original,
        NameSuggestion candidate,
        IEnumerable<string> directions)
    {
        var cmp = new RenameComparison();
        if (original == null || candidate == null) return cmp;

        cmp.OriginalName = original.FullName;
        cmp.NewName = candidate.FullName;
        cmp.Wuge = BuildWugeComparison(original.Wuge, candidate.Wuge);

        cmp.BaziOld = original.BaziScore;
        cmp.BaziNew = candidate.BaziScore;
        cmp.BaziDelta = Round1(candidate.BaziScore - original.BaziScore);

        cmp.WuxingOld = original.WuxingScore;
        cmp.WuxingNew = candidate.WuxingScore;
        cmp.WuxingDelta = Round1(candidate.WuxingScore - original.WuxingScore);

        cmp.WugeOld = original.WugeScore;
        cmp.WugeNew = candidate.WugeScore;
        cmp.WugeDelta = Round1(candidate.WugeScore - original.WugeScore);

        cmp.PhonologyOld = original.PhonologyScore;
        cmp.PhonologyNew = candidate.PhonologyScore;
        cmp.PhonologyDelta = Round1(candidate.PhonologyScore - original.PhonologyScore);

        cmp.MeaningOld = original.MeaningScore;
        cmp.MeaningNew = candidate.MeaningScore;
        cmp.MeaningDelta = Round1(candidate.MeaningScore - original.MeaningScore);

        cmp.ZodiacOld = original.ZodiacScore;
        cmp.ZodiacNew = candidate.ZodiacScore;
        cmp.ZodiacDelta = Round1(candidate.ZodiacScore - original.ZodiacScore);

        cmp.StyleOld = original.StyleScore;
        cmp.StyleNew = candidate.StyleScore;
        cmp.StyleDelta = Round1(candidate.StyleScore - original.StyleScore);

        cmp.RarityOld = original.RarityScore;
        cmp.RarityNew = candidate.RarityScore;
        cmp.RarityDelta = Round1(candidate.RarityScore - original.RarityScore);

        cmp.TotalOld = original.Total;
        cmp.TotalNew = candidate.Total;
        cmp.TotalDelta = Round1(candidate.Total - original.Total);

        foreach (var item in ScorePairs(cmp))
        {
            if (item.Item3 >= ImproveThreshold)
                cmp.ImprovedItems.Add("✓ " + item.Item1 + " " + FormatDelta(item.Item3));
            else if (item.Item3 <= DeclineThreshold)
                cmp.DeclinedItems.Add("△ " + item.Item1 + " " + FormatDelta(item.Item3));
        }

        var dirs = NormalizeDirections(directions);
        cmp.RecommendReason = BuildRecommendReason(cmp, original, candidate, dirs);
        cmp.Conclusion = BuildConclusion(cmp);
        cmp.Summary = BuildSummary(cmp);
        return cmp;
    }

    public static WugeComparison BuildWugeComparison(WugeResult original, WugeResult neu)
    {
        if (original == null || neu == null) return null;
        var change = CompareLuck(original.SancaiLuck, neu.SancaiLuck);
        var cmp = new WugeComparison
        {
            Original = original,
            NewName = neu,
            TianDelta = neu.Tian - original.Tian,
            RenDelta = neu.Ren - original.Ren,
            DiDelta = neu.Di - original.Di,
            WaiDelta = neu.Wai - original.Wai,
            ZongDelta = neu.Zong - original.Zong,
            ScoreDelta = Round1(neu.Score - original.Score),
            SancaiChange = change,
            Summary = "三才由 " + original.Sancai + "（" + original.SancaiLuck + "）→ " +
                neu.Sancai + "（" + neu.SancaiLuck + "），" + change +
                "；五格分 " + FormatDelta(Round1(neu.Score - original.Score)) + "。"
        };
        return cmp;
    }

    /// <summary>保留原 API：回傳可顯示文字，並可搭配 BuildRenameComparison 掛到 NameSuggestion。</summary>
    public static string CompareNames(NameSuggestion original, NameSuggestion candidate, IEnumerable<string> directions)
    {
        var cmp = BuildRenameComparison(original, candidate, directions);
        return FormatComparisonReport(cmp, original, candidate);
    }

    public static string FormatComparisonReport(RenameComparison cmp, NameSuggestion original, NameSuggestion candidate)
    {
        if (cmp == null) return "";
        var sb = new StringBuilder();
        sb.AppendLine("③ 原名 vs 新名");
        sb.AppendLine("原名：" + cmp.OriginalName + "　→　新名：" + cmp.NewName);
        sb.AppendLine();
        sb.AppendLine("【三才五格前後比較】");
        sb.AppendLine(FormatWugeTable(cmp.Wuge));
        sb.AppendLine();
        sb.AppendLine("【五行配置比較】");
        sb.AppendLine(FormatWuxingTable(original, candidate, cmp.Wuge));
        sb.AppendLine();
        sb.AppendLine("【綜合分數比較】");
        sb.AppendLine(FormatScoreTable(cmp));
        sb.AppendLine();
        sb.AppendLine("【改善項目】");
        if (cmp.ImprovedItems.Count == 0) sb.AppendLine("（相對原名，各項提升未達明顯門檻）");
        else foreach (var item in cmp.ImprovedItems) sb.AppendLine(item);
        sb.AppendLine();
        sb.AppendLine("【需要留意】");
        if (cmp.DeclinedItems.Count == 0) sb.AppendLine("（相對原名，各項未出現明顯回落）");
        else foreach (var item in cmp.DeclinedItems) sb.AppendLine(item);
        sb.AppendLine();
        sb.AppendLine(cmp.RecommendReason);
        sb.AppendLine();
        sb.AppendLine(cmp.Conclusion);
        return sb.ToString().TrimEnd();
    }

    public static double AdultRankBonus(
        NameSuggestion sug,
        NameSuggestion original,
        IEnumerable<string> directions)
    {
        if (sug == null || original == null) return 0;
        var dirs = NormalizeDirections(directions);
        if (dirs.Count == 0)
            return Math.Round(Math.Max(0, sug.Total - original.Total) * 0.08, 2);
        var bonus = 0.0;
        foreach (var direction in dirs)
        {
            var key = ImproveDirection[direction];
            var delta = Value(sug, key) - Value(original, key);
            if (delta > 0) bonus += delta * DirectionBoost;
            else if (delta < -5) bonus += delta * (DirectionBoost * 0.35);
        }
        bonus += Math.Max(0, sug.Total - original.Total) * 0.05;
        return Math.Round(bonus, 2);
    }

    public static string FormatGridCell(int number, string wx, string luck)
    {
        return number + "／" + (string.IsNullOrEmpty(wx) ? "—" : wx) + "／" + (string.IsNullOrEmpty(luck) ? "—" : luck);
    }

    public static string FormatDelta(double delta)
    {
        return delta.ToString("+0.0;-0.0;0.0");
    }

    private static string FormatWugeTable(WugeComparison w)
    {
        if (w == null || w.Original == null || w.NewName == null)
            return "（缺少五格資料，無法比較）";
        var o = w.Original;
        var n = w.NewName;
        var lines = new List<string>
        {
            "項目\t原名\t新名\t變化",
            "天格\t" + FormatGridCell(o.Tian, o.TianWx, o.TianLuck) + "\t" + FormatGridCell(n.Tian, n.TianWx, n.TianLuck) + "\t" + SignedInt(w.TianDelta),
            "人格\t" + FormatGridCell(o.Ren, o.RenWx, o.RenLuck) + "\t" + FormatGridCell(n.Ren, n.RenWx, n.RenLuck) + "\t" + SignedInt(w.RenDelta),
            "地格\t" + FormatGridCell(o.Di, o.DiWx, o.DiLuck) + "\t" + FormatGridCell(n.Di, n.DiWx, n.DiLuck) + "\t" + SignedInt(w.DiDelta),
            "外格\t" + FormatGridCell(o.Wai, o.WaiWx, o.WaiLuck) + "\t" + FormatGridCell(n.Wai, n.WaiWx, n.WaiLuck) + "\t" + SignedInt(w.WaiDelta),
            "總格\t" + FormatGridCell(o.Zong, o.ZongWx, o.ZongLuck) + "\t" + FormatGridCell(n.Zong, n.ZongWx, n.ZongLuck) + "\t" + SignedInt(w.ZongDelta),
            "三才\t" + o.Sancai + "\t" + n.Sancai + "\t" + (o.Sancai == n.Sancai ? "維持" : "調整"),
            "三才吉凶\t" + o.SancaiLuck + "\t" + n.SancaiLuck + "\t" + w.SancaiChange,
            "五格分數\t" + o.Score.ToString("0.0") + "\t" + n.Score.ToString("0.0") + "\t" + FormatDelta(w.ScoreDelta)
        };
        if (!string.IsNullOrWhiteSpace(w.Summary)) lines.Add(w.Summary);
        return string.Join("\n", lines);
    }

    private static string FormatWuxingTable(NameSuggestion original, NameSuggestion candidate, WugeComparison wuge)
    {
        var oldChars = original == null || original.CharWuxing == null || original.CharWuxing.Count == 0
            ? "—" : string.Join("／", original.CharWuxing);
        var newChars = candidate == null || candidate.CharWuxing == null || candidate.CharWuxing.Count == 0
            ? "—" : string.Join("／", candidate.CharWuxing);
        var lines = new List<string>
        {
            "項目\t原名\t新名",
            "姓名五行\t" + oldChars + "\t" + newChars
        };
        if (wuge != null && wuge.Original != null && wuge.NewName != null)
        {
            var o = wuge.Original;
            var n = wuge.NewName;
            lines.Add("天格\t" + o.TianWx + "\t" + n.TianWx);
            lines.Add("人格\t" + o.RenWx + "\t" + n.RenWx);
            lines.Add("地格\t" + o.DiWx + "\t" + n.DiWx);
            lines.Add("三才\t" + o.Sancai + "\t" + n.Sancai);
        }
        lines.Add("");
        lines.Add("五行配置調整說明：");
        if (oldChars == newChars)
            lines.Add("姓名用字五行與原名相同，調整重點可能在筆畫結構或其他面向。");
        else
            lines.Add("姓名用字五行由「" + oldChars + "」調整為「" + newChars + "」，可對照命局喜用檢視補益方向（僅供文化參考）。");
        return string.Join("\n", lines);
    }

    private static string FormatScoreTable(RenameComparison cmp)
    {
        var rows = new[]
        {
            Tuple.Create("命理", cmp.BaziOld, cmp.BaziNew, cmp.BaziDelta),
            Tuple.Create("五行", cmp.WuxingOld, cmp.WuxingNew, cmp.WuxingDelta),
            Tuple.Create("五格", cmp.WugeOld, cmp.WugeNew, cmp.WugeDelta),
            Tuple.Create("音韻", cmp.PhonologyOld, cmp.PhonologyNew, cmp.PhonologyDelta),
            Tuple.Create("字義", cmp.MeaningOld, cmp.MeaningNew, cmp.MeaningDelta),
            Tuple.Create("生肖", cmp.ZodiacOld, cmp.ZodiacNew, cmp.ZodiacDelta),
            Tuple.Create("風格", cmp.StyleOld, cmp.StyleNew, cmp.StyleDelta),
            Tuple.Create("辨識度", cmp.RarityOld, cmp.RarityNew, cmp.RarityDelta),
            Tuple.Create("整體", cmp.TotalOld, cmp.TotalNew, cmp.TotalDelta),
        };
        var lines = new List<string> { "分析項目\t原名\t新名\t變化" };
        foreach (var r in rows)
            lines.Add(r.Item1 + "\t" + r.Item2.ToString("0") + "\t" + r.Item3.ToString("0") + "\t" + FormatDelta(r.Item4));
        return string.Join("\n", lines);
    }

    private static string BuildRecommendReason(
        RenameComparison cmp,
        NameSuggestion original,
        NameSuggestion candidate,
        List<string> dirs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【推薦理由】");
        sb.AppendLine();
        sb.AppendLine("① 命理：");
        sb.AppendLine(cmp.BaziDelta >= ImproveThreshold
            ? "相較原名，命理契合度提升。"
            : cmp.BaziDelta <= DeclineThreshold
                ? "命理分數相對原名略有回落，宜一併檢視其他改善面向。"
                : "命理分數與原名接近，改名重點宜看其他面向。");
        sb.AppendLine();
        sb.AppendLine("② 三才五格：");
        if (cmp.Wuge != null && cmp.Wuge.Original != null && cmp.Wuge.NewName != null)
        {
            var parts = new List<string>();
            if (cmp.Wuge.RenDelta != 0) parts.Add("人格");
            if (cmp.Wuge.DiDelta != 0) parts.Add("地格");
            if (cmp.Wuge.ZongDelta != 0) parts.Add("總格");
            if (cmp.Wuge.TianDelta != 0) parts.Add("天格");
            if (cmp.Wuge.WaiDelta != 0) parts.Add("外格");
            if (parts.Count > 0)
                sb.AppendLine(string.Join("、", parts.Take(3)) + "結構有所調整（" + cmp.Wuge.SancaiChange + "）。");
            else
                sb.AppendLine("五格數值與原名相同或接近，三才為" + cmp.Wuge.SancaiChange + "。");
        }
        else sb.AppendLine("五格資料不足，無法細述結構調整。");
        sb.AppendLine();
        sb.AppendLine("③ 音韻：");
        sb.AppendLine(cmp.PhonologyDelta >= ImproveThreshold
            ? "姓名整體讀音更為流暢。"
            : "請實際朗讀，確認日常稱呼是否順口。");
        sb.AppendLine();
        sb.AppendLine("④ 字義：");
        sb.AppendLine(cmp.MeaningDelta >= ImproveThreshold
            ? "兩字寓意與本次改名方向較為相符。"
            : "字義分數與原名接近，可依個人偏好取捨。");
        sb.AppendLine();
        sb.AppendLine("⑤ 整體氣質：");
        var styleNote = cmp.StyleDelta >= ImproveThreshold ? "現代感提升，" : "";
        sb.AppendLine(styleNote + "整體氣質可依個人與職場形象再確認。");
        if (dirs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("與本次勾選方向相關：" + string.Join("、", dirs));
        }
        return sb.ToString().TrimEnd();
    }

    private static string BuildConclusion(RenameComparison cmp)
    {
        var improves = cmp.ImprovedItems
            .Select(x => x.Replace("✓ ", "").Split(' ')[0])
            .Distinct()
            .Take(4)
            .ToList();
        var sb = new StringBuilder();
        sb.AppendLine("【改名分析結論】");
        sb.AppendLine();
        sb.AppendLine("相較於原名「" + cmp.OriginalName + "」，");
        sb.AppendLine("候選名字「" + cmp.NewName + "」主要改善：");
        sb.AppendLine();
        if (improves.Count == 0)
            sb.AppendLine("・各項提升未達明顯門檻，請對照分項與個人訴求再判斷。");
        else
            foreach (var i in improves) sb.AppendLine("・" + i);
        sb.AppendLine();
        if (cmp.TotalDelta >= 0)
            sb.AppendLine("整體分數由 " + cmp.TotalOld.ToString("0") + " 至 " + cmp.TotalNew.ToString("0") +
                "（" + FormatDelta(cmp.TotalDelta) + "）。");
        else
            sb.AppendLine("整體分數由 " + cmp.TotalOld.ToString("0") + " 至 " + cmp.TotalNew.ToString("0") +
                "（" + FormatDelta(cmp.TotalDelta) + "）；若仍推薦，主因可能在特定分項或個人訴求。");
        sb.AppendLine();
        sb.AppendLine("本名字與本次設定的改名方向具有可討論的契合度；以上僅供文化參考，不構成人生或運勢保證。");
        return sb.ToString().TrimEnd();
    }

    private static string BuildSummary(RenameComparison cmp)
    {
        if (cmp.TotalDelta >= 5 && cmp.ImprovedItems.Count > 0)
            return "新名綜合提升明顯，主要改善" + string.Join("、", cmp.ImprovedItems.Take(3)).Replace("✓ ", "") + "。";
        if (cmp.TotalDelta >= 1)
            return "新名綜合略升，可再對照希望改善方向。";
        if (cmp.TotalDelta >= -1)
            return "總分接近，請看分項是否更符合改名目標。";
        return "新名總分未高於原名，除非特定分項符合訴求，否則宜再篩。";
    }

    private static string CompareLuck(string oldLuck, string newLuck)
    {
        var o = LuckScore(oldLuck);
        var n = LuckScore(newLuck);
        if (n > o) return "改善";
        if (n < o) return "下降";
        return "維持";
    }

    private static int LuckScore(string luck)
    {
        int v;
        return LuckRank.TryGetValue(luck ?? "", out v) ? v : 3;
    }

    private static string SignedInt(int delta)
    {
        return delta.ToString("+0;-0;0");
    }

    private static double Round1(double value)
    {
        return Math.Round(value, 1);
    }

    private static List<Tuple<string, double, double>> ScorePairs(RenameComparison cmp)
    {
        return new List<Tuple<string, double, double>>
        {
            Tuple.Create("命理", cmp.BaziNew, cmp.BaziDelta),
            Tuple.Create("五行", cmp.WuxingNew, cmp.WuxingDelta),
            Tuple.Create("五格", cmp.WugeNew, cmp.WugeDelta),
            Tuple.Create("音韻", cmp.PhonologyNew, cmp.PhonologyDelta),
            Tuple.Create("字義", cmp.MeaningNew, cmp.MeaningDelta),
            Tuple.Create("生肖", cmp.ZodiacNew, cmp.ZodiacDelta),
            Tuple.Create("風格", cmp.StyleNew, cmp.StyleDelta),
            Tuple.Create("辨識度", cmp.RarityNew, cmp.RarityDelta),
        };
    }

    private static List<string> NormalizeDirections(IEnumerable<string> directions)
    {
        return (directions ?? Enumerable.Empty<string>()).Select(x => (x ?? "").Trim())
            .Where(ImproveDirection.ContainsKey).Distinct().ToList();
    }

    private static List<KeyValuePair<string, double>> Dimensions(NameSuggestion sug)
    {
        return new List<KeyValuePair<string, double>>
        {
            new KeyValuePair<string, double>("bazi", sug.BaziScore),
            new KeyValuePair<string, double>("wuxing", sug.WuxingScore),
            new KeyValuePair<string, double>("wuge", sug.WugeScore),
            new KeyValuePair<string, double>("phonology", sug.PhonologyScore),
            new KeyValuePair<string, double>("meaning", sug.MeaningScore),
            new KeyValuePair<string, double>("zodiac", sug.ZodiacScore),
            new KeyValuePair<string, double>("style", sug.StyleScore),
            new KeyValuePair<string, double>("rarity", sug.RarityScore),
            new KeyValuePair<string, double>("total", sug.Total)
        };
    }

    private static double Value(NameSuggestion sug, string key)
    {
        return Dimensions(sug).First(x => x.Key == key).Value;
    }

    private static List<string> ReasonFocus(string reason)
    {
        reason = (reason ?? "").Trim();
        var result = new List<string>();
        AddIf(result, reason.Contains("諧音") || reason.Contains("音韻"), "音韻流暢");
        AddIf(result, reason.Contains("命理"), "命理契合");
        AddIf(result, reason.Contains("五格"), "五格吉數");
        AddIf(result, reason.Contains("字義") || reason.Contains("粗俗") || reason.Contains("不雅"), "字義寓意");
        AddIf(result, reason.Contains("常見") || reason.Contains("菜市"), "辨識度／稀有");
        AddIf(result, reason.Contains("職場") || reason.Contains("形象") || reason.Contains("專業"), "現代感");
        AddIf(result, reason.Contains("職場") || reason.Contains("形象") || reason.Contains("專業"), "整體綜合");
        return result;
    }

    private static void AddIf(List<string> list, bool condition, string value)
    {
        if (condition && !list.Contains(value)) list.Add(value);
    }
}
}
