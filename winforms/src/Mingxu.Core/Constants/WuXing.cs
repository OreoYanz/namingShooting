using System.Collections.Generic;
using System.Linq;

namespace Mingxu.Core.Constants
{

public sealed class HiddenStem
{
    public string Gan { get; set; }
    public double Weight { get; set; }

    public HiddenStem(string gan, double weight)
    {
        Gan = gan;
        Weight = weight;
    }
}

public static class WuXing
{
    public const string Stems = "甲乙丙丁戊己庚辛壬癸";
    public const string Branches = "子丑寅卯辰巳午未申酉戌亥";
    public const string Zodiac = "鼠牛虎兔龍蛇馬羊猴雞狗豬";

    public static readonly string[] Order = new string[] { "木", "火", "土", "金", "水" };

    public static readonly Dictionary<string, string> StemWuxing = new Dictionary<string, string>()
    {
        ["甲"] = "木", ["乙"] = "木", ["丙"] = "火", ["丁"] = "火", ["戊"] = "土",
        ["己"] = "土", ["庚"] = "金", ["辛"] = "金", ["壬"] = "水", ["癸"] = "水",
    };

    public static readonly HashSet<string> StemYang = new HashSet<string>(new string[] { "甲", "丙", "戊", "庚", "壬" });

    public static readonly Dictionary<string, string> BranchWuxing = new Dictionary<string, string>()
    {
        ["子"] = "水", ["丑"] = "土", ["寅"] = "木", ["卯"] = "木", ["辰"] = "土", ["巳"] = "火",
        ["午"] = "火", ["未"] = "土", ["申"] = "金", ["酉"] = "金", ["戌"] = "土", ["亥"] = "水",
    };

    public static readonly Dictionary<string, string> BranchZodiac = BuildBranchZodiac();

    private static Dictionary<string, string> BuildBranchZodiac()
    {
        var map = new Dictionary<string, string>();
        for (var i = 0; i < Branches.Length && i < Zodiac.Length; i++)
            map[Branches[i].ToString()] = Zodiac[i].ToString();
        return map;
    }

    public static readonly Dictionary<string, List<HiddenStem>> ZhiCanggan = new Dictionary<string, List<HiddenStem>>()
    {
        ["子"] = new List<HiddenStem> { new HiddenStem("癸", 1.0) },
        ["丑"] = new List<HiddenStem> { new HiddenStem("己", 0.60), new HiddenStem("癸", 0.30), new HiddenStem("辛", 0.10) },
        ["寅"] = new List<HiddenStem> { new HiddenStem("甲", 0.60), new HiddenStem("丙", 0.30), new HiddenStem("戊", 0.10) },
        ["卯"] = new List<HiddenStem> { new HiddenStem("乙", 1.0) },
        ["辰"] = new List<HiddenStem> { new HiddenStem("戊", 0.60), new HiddenStem("乙", 0.30), new HiddenStem("癸", 0.10) },
        ["巳"] = new List<HiddenStem> { new HiddenStem("丙", 0.60), new HiddenStem("庚", 0.30), new HiddenStem("戊", 0.10) },
        ["午"] = new List<HiddenStem> { new HiddenStem("丁", 0.70), new HiddenStem("己", 0.30) },
        ["未"] = new List<HiddenStem> { new HiddenStem("己", 0.60), new HiddenStem("丁", 0.30), new HiddenStem("乙", 0.10) },
        ["申"] = new List<HiddenStem> { new HiddenStem("庚", 0.60), new HiddenStem("壬", 0.30), new HiddenStem("戊", 0.10) },
        ["酉"] = new List<HiddenStem> { new HiddenStem("辛", 1.0) },
        ["戌"] = new List<HiddenStem> { new HiddenStem("戊", 0.60), new HiddenStem("辛", 0.30), new HiddenStem("丁", 0.10) },
        ["亥"] = new List<HiddenStem> { new HiddenStem("壬", 0.70), new HiddenStem("甲", 0.30) },
    };

    public static readonly Dictionary<string, string> Sheng = new Dictionary<string, string>()
    {
        ["木"] = "火", ["火"] = "土", ["土"] = "金", ["金"] = "水", ["水"] = "木",
    };

    public static readonly Dictionary<string, string> Ke = new Dictionary<string, string>()
    {
        ["木"] = "土", ["土"] = "水", ["水"] = "火", ["火"] = "金", ["金"] = "木",
    };

    public static readonly Dictionary<int, string> StrokeWuxing = new Dictionary<int, string>()
    {
        [1] = "木", [2] = "木", [3] = "火", [4] = "火", [5] = "土",
        [6] = "土", [7] = "金", [8] = "金", [9] = "水", [0] = "水",
    };

    public static bool Generates(string a, string b)
    {
        string x;
        return Sheng.TryGetValue(a, out x) && x == b;
    }

    public static bool Controls(string a, string b)
    {
        string x;
        return Ke.TryGetValue(a, out x) && x == b;
    }

    public static string StrokeElement(int n) => StrokeWuxing[((n % 10) + 10) % 10];

    public static int WugeLuckPoints(string luck)
    {
        switch (luck)
        {
            case "大吉": return 18;
            case "吉": return 12;
            case "次吉": return 6;
            case "中性":
            case "中": return 0;
            case "凶": return -12;
            case "大凶": return -18;
            default: return 0;
        }
    }

    public static string Wuge81Class(int n)
    {
        n = ((n - 1) % 81) + 1;
        string luck;
        return Wuge81Table.Luck.TryGetValue(n, out luck) ? luck : "中性";
    }

    public static string Wuge81Text(int n)
    {
        n = ((n - 1) % 81) + 1;
        string text;
        return Wuge81Table.Text.TryGetValue(n, out text) ? text : "";
    }
}
}
