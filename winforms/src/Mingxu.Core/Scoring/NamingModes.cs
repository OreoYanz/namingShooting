using System.Collections.Generic;
using Mingxu.Core.Models;

namespace Mingxu.Core.Scoring
{
public sealed class NamingModeProfile
{
    public string Key { get; set; }
    public string Label { get; set; }
    public Dictionary<string, double> Weights { get; set; }
    public string[] StylePrefer { get; set; }
    public string[] StyleSoft { get; set; }
    public string RarityBias { get; set; }
    public bool ExcludeClassicHot { get; set; }
}

public static class NamingModes
{
    public static readonly Dictionary<string, double> CompositeWeights = Weights(
        .40, .10, .15, .05, .15, .05, .05, .05);

    private static readonly Dictionary<string, NamingModeProfile> Profiles =
        new Dictionary<string, NamingModeProfile>
    {
        { "traditional", Profile("traditional", "傳統命理",
            Weights(.35,.15,.25,.05,.05,.10,.03,.02),
            new[] {"classical","elegant","strong","neutral","literary"}, new[] {"cute"}, "balanced", false) },
        { "modern", Profile("modern", "現代命名",
            Weights(.15,.08,.08,.25,.12,.05,.20,.07),
            new[] {"modern","elegant","neutral","literary","cute","soft"}, new[] {"strong"}, "balanced", true) },
        { "literary", Profile("literary", "文學命名",
            Weights(.12,.08,.08,.18,.28,.04,.18,.04),
            new[] {"literary","classical","elegant","soft","modern"}, new[] {"cute"}, "balanced", true) },
        { "niche", Profile("niche", "小眾精品",
            Weights(.12,.08,.08,.15,.15,.04,.18,.20),
            new[] {"literary","elegant","modern","classical","neutral"}, new string[0], "rare", true) },
        { "balanced", Profile("balanced", "綜合推薦",
            Weights(.40,.10,.15,.05,.15,.05,.05,.05), null, null, "balanced", false) },
    };

    public static NamingModeProfile Resolve(string mode)
    {
        var aliases = new Dictionary<string, string>
        {
            {"綜合","balanced"},{"綜合推薦","balanced"},{"傳統","traditional"},{"傳統命理","traditional"},
            {"現代","modern"},{"現代命名","modern"},{"文學","literary"},{"文學命名","literary"},
            {"小眾","niche"},{"小眾精品","niche"},{"精品","niche"}
        };
        var key = (mode ?? "balanced").Trim().ToLowerInvariant();
        string alias;
        if (aliases.TryGetValue(mode ?? "", out alias)) key = alias;
        NamingModeProfile result;
        return Profiles.TryGetValue(key, out result) ? result : Profiles["balanced"];
    }

    public static int StyleValue(CharacterInfo info, string key)
    {
        switch (key)
        {
            case "modern": return info.Modern;
            case "classical": return info.Classical;
            case "literary": return info.Literary;
            case "elegant": return info.Elegant;
            case "cute": return info.Cute;
            case "neutral": return info.Neutral;
            case "strong": return info.Strong;
            case "soft": return info.Soft;
            default: return 50;
        }
    }

    private static NamingModeProfile Profile(string key, string label, Dictionary<string, double> weights,
        string[] prefer, string[] soft, string rarityBias, bool excludeClassic)
    {
        return new NamingModeProfile
        {
            Key = key, Label = label, Weights = weights, StylePrefer = prefer, StyleSoft = soft,
            RarityBias = rarityBias, ExcludeClassicHot = excludeClassic
        };
    }

    private static Dictionary<string, double> Weights(double bazi, double wuxing, double wuge,
        double phonology, double meaning, double zodiac, double style, double rarity)
    {
        return new Dictionary<string, double>
        {
            {"bazi",bazi},{"wuxing",wuxing},{"wuge",wuge},{"phonology",phonology},
            {"meaning",meaning},{"zodiac",zodiac},{"style",style},{"rarity",rarity}
        };
    }
}
}
