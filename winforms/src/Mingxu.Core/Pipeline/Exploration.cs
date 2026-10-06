using System;
using System.Collections.Generic;

namespace Mingxu.Core.Pipeline
{

public sealed class ExplorationConfig
{
    public string Level { get; set; }
    public int PoolMin { get; set; }
    public int PoolMax { get; set; }
    public int StableTop { get; set; }
    public int ExploreN { get; set; }
    public int ExploreFrom { get; set; }
    public int ExploreTo { get; set; }

    public ExplorationConfig(string level, int poolMin, int poolMax, int stableTop, int exploreN, int exploreFrom, int exploreTo)
    {
        Level = level;
        PoolMin = poolMin;
        PoolMax = poolMax;
        StableTop = stableTop;
        ExploreN = exploreN;
        ExploreFrom = exploreFrom;
        ExploreTo = exploreTo;
    }
}

public static class Exploration
{
    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        ["conservative"] = "穩健",
        ["balanced"] = "均衡",
        ["exploratory"] = "探索",
        ["creative"] = "創意",
    };

    private static readonly Dictionary<string, ExplorationConfig> Configs = new Dictionary<string, ExplorationConfig>()
    {
        ["conservative"] = new ExplorationConfig("conservative", 200, 300, 220, 24, 220, 700),
        ["balanced"] = new ExplorationConfig("balanced", 300, 500, 300, 50, 300, 900),
        ["exploratory"] = new ExplorationConfig("exploratory", 400, 600, 350, 80, 320, 1000),
        ["creative"] = new ExplorationConfig("creative", 450, 600, 320, 100, 280, 1100),
    };

    public static string AutoLevel(AnalysisRequestLike req)
    {
        var forced = (req.ExplorationLevel ?? "").Trim().ToLowerInvariant();
        if (Configs.ContainsKey(forced)) return forced;

        var strict = 0;
        if (!string.IsNullOrWhiteSpace(req.Zibei)) strict++;
        if (!string.IsNullOrWhiteSpace(req.PreferredChars)) strict++;
        if (!string.IsNullOrWhiteSpace(req.ForbiddenChars)) strict++;
        if (!req.AllowSingle || !req.AllowDouble) strict++;

        if (req.Mode == "rename")
            return strict >= 2 ? "balanced" : "exploratory";
        if (strict >= 2) return "conservative";
        if (req.NamingMode == "niche" || req.NamingMode == "literary") return "creative";
        return "balanced";
    }

    public static ExplorationConfig ConfigFor(AnalysisRequestLike req) => Configs[AutoLevel(req)];
}

/// <summary>輕量請求視圖，避免 Pipeline 循環依賴 AnalysisRequest 擴充欄位。</summary>
public sealed class AnalysisRequestLike
{
    public string Mode { get; set; } = "newborn";
    public string NamingMode { get; set; } = "balanced";
    public string ExplorationLevel { get; set; } = "";
    public string Zibei { get; set; } = "";
    public string PreferredChars { get; set; } = "";
    public string ForbiddenChars { get; set; } = "";
    public bool AllowSingle { get; set; } = true;
    public bool AllowDouble { get; set; } = true;
}
}
