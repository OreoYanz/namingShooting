using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Core.Scoring
{
public sealed class PhonologyScoreResult
{
    public double Score { get; set; }
    public List<string> Notes { get; set; }
}

internal sealed class SoundPart
{
    public string Char { get; set; }
    public int Tone { get; set; }
    public string Initial { get; set; }
    public string Final { get; set; }
}

public static class PhonologyScorer
{
    private static readonly HashSet<string> Good3 = new HashSet<string>
        { "平仄仄","仄平仄","平平仄","仄仄平" };
    private static readonly HashSet<string> Ok3 = new HashSet<string>
        { "平仄平","仄平平" };
    private static readonly HashSet<string> Good4 = new HashSet<string>
        { "平仄仄平","仄平仄仄","平平仄仄","仄仄平平","平仄平仄","仄平仄平" };

    public static PhonologyScoreResult Score(string surname, string given, ICharacterRepository repo)
    {
        var sounds = new List<SoundPart>();
        foreach (var ch in (surname ?? "") + (given ?? "")) sounds.Add(Parse(ch.ToString(), repo));
        var raw = 0;
        var notes = new List<string>();
        var tones = sounds.Select(x => x.Tone).ToList();
        var pingze = string.Join("", tones.Select(ToneClass));
        var sandhi = SandhiTones(tones);
        if (!tones.SequenceEqual(sandhi))
            notes.Add("上聲連讀變調：" + string.Join("-", tones) + " → " + string.Join("-", sandhi));
        notes.Add((surname ?? "").Length > 1 ? "複姓連讀：" + ((given ?? "").Length > 1 ? "四字連讀" : "三字連讀") :
            ((given ?? "").Length > 1 ? "單姓雙名三字連讀" : "單名兩字連讀"));

        for (var i = 0; i + 1 < sounds.Count; i++)
        {
            if (tones[i] == 3 && tones[i + 1] == 3) { raw -= 8; notes.Add("相鄰上聲-8"); }
            if (sounds[i].Initial.Length > 0 && sounds[i].Initial == sounds[i + 1].Initial)
            { raw -= 10; notes.Add("相鄰聲母重複-10"); }
            if (sounds[i].Final.Length > 0 && sounds[i].Final == sounds[i + 1].Final)
            { raw -= 6; notes.Add("相鄰韻母疊韻-6"); }
        }

        var usable = tones.Where(x => x >= 1 && x <= 4).ToList();
        if (usable.Count >= 2 && usable.Distinct().Count() == 1) raw -= 10;
        else if (Good3.Contains(pingze) || Good4.Contains(pingze)) raw += 12;
        else if (Ok3.Contains(pingze)) raw += 6;
        else
        {
            var alternate = 0;
            for (var i = 0; i + 1 < tones.Count; i++)
                if (ToneClass(tones[i]) != "?" && ToneClass(tones[i + 1]) != "?" &&
                    ToneClass(tones[i]) != ToneClass(tones[i + 1])) alternate++;
            if (alternate == tones.Count - 1 && tones.Count >= 2) raw += 12;
            else if (alternate > 0) raw += 6;
        }
        if (tones.Count > 0 && tones[tones.Count - 1] == 4) raw += 4;
        if (tones.Count > 0 && tones[tones.Count - 1] == 5) raw -= 4;
        raw += ExcessPenalty(sounds.Select(x => x.Initial), -8);
        raw += ExcessPenalty(sounds.Select(x => x.Final), -12);

        notes.Insert(0, "聲調：" + string.Join("-", tones.Select(x => x == 0 ? "?" : x.ToString())) + "；平仄：" + pingze);
        return new PhonologyScoreResult
        {
            Score = Clamp(50 + raw),
            Notes = notes
        };
    }

    private static SoundPart Parse(string ch, ICharacterRepository repo)
    {
        var pinyin = (repo.SoundPinyin(ch) ?? "").Trim().ToLowerInvariant();
        while (pinyin.Length > 0 && char.IsDigit(pinyin[pinyin.Length - 1]))
            pinyin = pinyin.Substring(0, pinyin.Length - 1);
        string initial;
        string finalPart;
        SplitInitialFinal(pinyin, out initial, out finalPart);

        var zhuyin = StripZhuyinTone(repo.SoundZhuyin(ch));
        const string initials = "ㄅㄆㄇㄈㄉㄊㄋㄌㄍㄎㄏㄐㄑㄒㄓㄔㄕㄖㄗㄘㄙ";
        if (zhuyin.Length > 0)
        {
            if (initials.IndexOf(zhuyin[0]) >= 0)
            {
                initial = zhuyin[0].ToString();
                finalPart = zhuyin.Substring(1);
            }
            else finalPart = zhuyin;
        }
        return new SoundPart { Char = ch, Tone = repo.ToneOf(ch), Initial = initial, Final = finalPart };
    }

    private static void SplitInitialFinal(string pinyin, out string initial, out string finalPart)
    {
        var s = (pinyin ?? "").Replace("u:", "ü").Replace("v", "ü");
        initial = "";
        finalPart = "";
        if (s.Length == 0) return;
        if (s[0] == 'y')
        {
            var rest = s.Substring(1);
            finalPart = s == "yong" ? "iong" :
                rest.StartsWith("u") ? "ü" + rest.Substring(1) :
                rest.StartsWith("i") ? rest : "i" + rest;
            finalPart = NormalizeFinal(finalPart);
            return;
        }
        if (s[0] == 'w')
        {
            var rest = s.Substring(1);
            finalPart = rest.Length == 0 ? "u" : rest.StartsWith("u") ? rest : "u" + rest;
            finalPart = NormalizeFinal(finalPart);
            return;
        }
        foreach (var candidate in new[] {"zh","ch","sh","b","p","m","f","d","t","n","l","g","k","h","j","q","x","r","z","c","s"})
            if (s.StartsWith(candidate)) { initial = candidate; break; }
        finalPart = s.Substring(initial.Length);
        if ((initial == "j" || initial == "q" || initial == "x") && finalPart.StartsWith("u"))
            finalPart = "ü" + finalPart.Substring(1);
        if ((initial == "n" || initial == "l") && (finalPart == "ue" || finalPart == "ve"))
            finalPart = "üe";
        finalPart = NormalizeFinal(finalPart);
    }

    private static string NormalizeFinal(string value)
    {
        switch ((value ?? "").Replace("v", "ü"))
        {
            case "iou": return "iu";
            case "uei": return "ui";
            case "uen": return "un";
            case "ueng": return "ong";
            case "ue":
            case "ve": return "üe";
            case "van": return "üan";
            case "vn": return "ün";
            case "v": return "ü";
            default: return (value ?? "").Replace("v", "ü");
        }
    }

    private static List<int> SandhiTones(IList<int> tones)
    {
        var result = tones.ToList();
        var i = 0;
        while (i < result.Count)
        {
            if (result[i] != 3) { i++; continue; }
            var j = i;
            while (j < result.Count && result[j] == 3) j++;
            if (j - i >= 2)
                for (var k = i; k < j - 1; k++) result[k] = 2;
            i = j;
        }
        return result;
    }

    private static string StripZhuyinTone(string text)
    {
        return (text ?? "").Replace("ˊ","").Replace("ˇ","").Replace("ˋ","").Replace("˙","").Trim();
    }

    private static int ExcessPenalty(IEnumerable<string> parts, int penalty)
    {
        var values = parts.Where(x => !string.IsNullOrEmpty(x)).ToList();
        if (values.Count < 3) return 0;
        return values.GroupBy(x => x).Any(g => g.Count() >= values.Count - 1) ? penalty : 0;
    }

    private static string ToneClass(int tone)
    {
        return tone == 1 || tone == 2 ? "平" : tone == 3 || tone == 4 ? "仄" : "?";
    }

    private static double Clamp(double value)
    {
        return Math.Max(0, Math.Min(100, Math.Round(value, 1)));
    }
}
}
