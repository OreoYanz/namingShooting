using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mingxu.Core.Models;

namespace Mingxu.Core.Rules
{
public sealed class HardRuleResult
{
    public bool Blocked { get; set; }
    public string Reason { get; set; }
    public string Code { get; set; }

    public HardRuleResult(bool blocked, string reason, string code)
    {
        Blocked = blocked;
        Reason = reason;
        Code = code;
    }
}

public static class HardRules
{
    public static readonly HashSet<char> Vulgar = new HashSet<char>(
        "屎尿糞屁臭淫姦奸娼妓嫖賭毒穢腐".ToCharArray());
    public static readonly HashSet<char> Negative = new HashSet<char>(
        ("死亡病鬼屍喪灾災禍魔屠宰凶兇煞殃厄賤哀哭奴髒醜丑慘恨怨殺戮殉殤夭斃" +
         "棺墳墓魂魄冥厲祟瘟疫癌瘤瘧癩瘡囚獄刑斬騙偷盜賊匪辱恥惡邪妖孽罪殘廢").ToCharArray());
    public static readonly string[] NegativePhrases =
    {
        "去死","該死","小死","大死","死掉","找死","送死","枉死","橫死","病死",
        "拉屎","吃屎","放尿","放屁","糞便","發臭","惡臭","腥臭",
        "白痴","白癡","智障","腦殘","廢物","笨蛋","混蛋","王八蛋",
        "倒霉","倒楣","衰尾","短命","夭折","暴斃","窮鬼","死鬼","色狼",
        "嫖客","妓女","強姦","通姦","賭博","吸毒","腐敗","腐爛","災禍","禍害","喪門"
    };

    private static readonly Dictionary<char, string> Zhuyin = BuildZhuyin();
    private static readonly Dictionary<string, string[]> Homophones = BuildHomophones();

    public static HardRuleResult Evaluate(
        string surname, string given, IEnumerable<string> forbiddenTokens, ICharacterRepository repo)
    {
        surname = (surname ?? "").Trim();
        given = (given ?? "").Trim();
        if (given.Length == 0) return Reject("沒有名字", "unreadable");

        foreach (var token in forbiddenTokens ?? Enumerable.Empty<string>())
            if (!string.IsNullOrEmpty(token) && given.Contains(token))
                return Reject("使用者禁用「" + token + "」", "forbidden");

        var manuallyBlocked = UserBlocked.Load();
        foreach (var ch in given)
        {
            if (!IsNormalWritable(ch)) return Reject("「" + ch + "」無法正常讀寫", "unreadable");
            if (repo != null && !repo.TryStroke(ch.ToString()).HasValue)
                return Reject("「" + ch + "」不在康熙／戶政可用字", "unreadable");
            if (manuallyBlocked.Contains(ch)) return Reject("手動剔除字「" + ch + "」", "user_blocked");
            if (Vulgar.Contains(ch)) return Reject("粗俗用字「" + ch + "」", "vulgar");
            if (Negative.Contains(ch)) return Reject("明顯負面用字「" + ch + "」", "negative");
        }

        var full = surname + given;
        foreach (var phrase in NegativePhrases)
            if (full.Contains(phrase)) return Reject("負面詞「" + phrase + "」", "homophone");

        var givenSounds = given.Select(ch => SoundCore(ch, repo)).ToArray();
        var fullSounds = full.Select(ch => SoundCore(ch, repo)).ToArray();
        foreach (var pair in Homophones)
        {
            if (givenSounds.Length == pair.Value.Length && SequenceAt(givenSounds, pair.Value, 0))
                return Reject("負面諧音（近「" + pair.Key + "」）", "homophone");
            if (pair.Value.Length >= 2)
                for (var i = 0; i <= fullSounds.Length - pair.Value.Length; i++)
                    if (SequenceAt(fullSounds, pair.Value, i))
                        return Reject("負面諧音（近「" + pair.Key + "」）", "homophone");
        }
        return new HardRuleResult(false, "", "");
    }

    public static bool IsNormalWritable(char ch)
    {
        var code = (int)ch;
        return (code >= 0x4E00 && code <= 0x9FFF) ||
               (code >= 0x3400 && code <= 0x4DBF) ||
               (code >= 0xF900 && code <= 0xFAFF);
    }

    public static HashSet<char> BlockedCharacters(IEnumerable<string> forbiddenTokens)
    {
        var result = new HashSet<char>(Vulgar);
        result.UnionWith(Negative);
        result.UnionWith(UserBlocked.Load());
        foreach (var token in forbiddenTokens ?? Enumerable.Empty<string>())
            if (!string.IsNullOrEmpty(token) && token.Length == 1) result.Add(token[0]);
        return result;
    }

    private static HardRuleResult Reject(string reason, string code)
    {
        return new HardRuleResult(true, reason, code);
    }

    private static string SoundCore(char ch, ICharacterRepository repo)
    {
        var raw = repo == null ? "" : repo.SoundZhuyin(ch.ToString());
        var core = Regex.Replace(raw ?? "", "[ˊˇˋ˙\\s0-9a-zA-Z]", "");
        string builtIn;
        return core.Length > 0 ? core : Zhuyin.TryGetValue(ch, out builtIn) ? builtIn : "";
    }

    private static bool SequenceAt(string[] source, string[] target, int start)
    {
        if (start < 0 || start + target.Length > source.Length) return false;
        for (var i = 0; i < target.Length; i++)
            if (source[start + i].Length == 0 || source[start + i] != target[i]) return false;
        return true;
    }

    private static Dictionary<string, string[]> BuildHomophones()
    {
        var map = new Dictionary<string, string[]>();
        foreach (var phrase in NegativePhrases)
        {
            var sounds = phrase.Select(ch =>
            {
                string value;
                return Zhuyin.TryGetValue(ch, out value) ? value : "";
            }).ToArray();
            if (sounds.All(x => x.Length > 0)) map[phrase] = sounds;
        }
        return map;
    }

    private static Dictionary<char, string> BuildZhuyin()
    {
        var chars = "去死該小大掉找送枉橫病拉吃屎放尿屁糞便發臭惡腥白痴癡智障腦殘廢物笨蛋混王八倒霉楣衰尾短命夭折暴斃窮鬼色狼嫖客妓女強姦通賭博吸毒腐敗爛災禍害喪門";
        var sounds = "ㄑㄩ,ㄙ,ㄍㄞ,ㄒㄧㄠ,ㄉㄚ,ㄉㄧㄠ,ㄓㄠ,ㄙㄨㄥ,ㄨㄤ,ㄏㄥ,ㄅㄧㄥ,ㄌㄚ,ㄔ,ㄕ,ㄈㄤ,ㄋㄧㄠ,ㄆㄧ,ㄈㄣ,ㄅㄧㄢ,ㄈㄚ,ㄔㄡ,ㄜ,ㄒㄧㄥ,ㄅㄞ,ㄔ,ㄔ,ㄓ,ㄓㄤ,ㄋㄠ,ㄘㄢ,ㄈㄟ,ㄨ,ㄅㄣ,ㄉㄢ,ㄏㄨㄣ,ㄨㄤ,ㄅㄚ,ㄉㄠ,ㄇㄟ,ㄇㄟ,ㄕㄨㄞ,ㄨㄟ,ㄉㄨㄢ,ㄇㄧㄥ,ㄧㄠ,ㄓㄜ,ㄅㄠ,ㄅㄧ,ㄑㄩㄥ,ㄍㄨㄟ,ㄙㄜ,ㄌㄤ,ㄆㄧㄠ,ㄎㄜ,ㄐㄧ,ㄋㄩ,ㄑㄧㄤ,ㄐㄧㄢ,ㄊㄨㄥ,ㄉㄨ,ㄅㄛ,ㄒㄧ,ㄉㄨ,ㄈㄨ,ㄅㄞ,ㄌㄢ,ㄗㄞ,ㄏㄨㄛ,ㄏㄞ,ㄙㄤ,ㄇㄣ".Split(',');
        var map = new Dictionary<char, string>();
        for (var i = 0; i < chars.Length && i < sounds.Length; i++) map[chars[i]] = sounds[i];
        return map;
    }
}
}
