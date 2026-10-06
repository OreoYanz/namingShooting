using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Data
{

public sealed class CharacterRepository : ICharacterRepository
{
    private readonly Dictionary<string, CharacterInfo> _byChar;
    private readonly Dictionary<string, HashSet<string>> _classicHot;
    private readonly List<HotNameEntry> _hotNames;
    private readonly List<string> _compoundSurnames;

    private sealed class HotNameEntry
    {
        public int Year { get; set; }
        public string Gender { get; set; }
        public string Given { get; set; }
        public int Rank { get; set; }
    }

    public CharacterRepository(string dataDirectory = null)
    {
        if (dataDirectory == null)
            dataDirectory = FindDataDir();
        var seed = Path.Combine(dataDirectory, "characters.seed.csv");
        if (!File.Exists(seed))
            throw new FileNotFoundException("找不到 characters.seed.csv，請先執行 scripts/generate_csharp_data.py", seed);

        _byChar = new Dictionary<string, CharacterInfo>();
        foreach (var line in File.ReadLines(seed, Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = SplitCsv(line);
            if (parts.Count < 20) continue;
            var ch = parts[0];
            if (string.IsNullOrEmpty(ch)) continue;
            // UTF-8 BOM 可能殘在首欄
            if (ch.Length > 0 && ch[0] == '\uFEFF') ch = ch.TrimStart('\uFEFF');
            if (string.IsNullOrEmpty(ch)) continue;
            _byChar[ch] = new CharacterInfo
            {
                Char = ch,
                Stroke = ParseInt(parts[1]),
                Wuxing = parts[2],
                Candidate = ParseInt(parts[3]),
                Tone = parts.Count > 4 ? parts[4] : "",
                Pinyin = parts.Count > 5 ? parts[5] : "",
                Zhuyin = parts[6],
                Meaning = parts[7],
                Radical = parts[8],
                Frequency = ParseIntDefault(parts[9], 50),
                Rarity = ParseIntDefault(parts[10], 50),
                GenderTag = string.IsNullOrWhiteSpace(parts[11]) ? "U" : parts[11].Trim().ToUpperInvariant(),
                Modern = ParseIntDefault(parts[12], 50),
                Classical = ParseIntDefault(parts[13], 50),
                Literary = ParseIntDefault(parts[14], 50),
                Elegant = ParseIntDefault(parts[15], 50),
                Cute = ParseIntDefault(parts[16], 50),
                Neutral = ParseIntDefault(parts[17], 50),
                Strong = ParseIntDefault(parts[18], 50),
                Soft = ParseIntDefault(parts[19], 50),
            };
        }

        _classicHot = new Dictionary<string, HashSet<string>>
        {
            { "M", new HashSet<string>() },
            { "F", new HashSet<string>() },
            { "U", new HashSet<string>() },
        };
        var hotPath = Path.Combine(dataDirectory, "classic_hot.json");
        if (File.Exists(hotPath))
        {
            var array = JToken.Parse(File.ReadAllText(hotPath, Encoding.UTF8)) as JArray;
            if (array != null)
            {
                foreach (var token in array)
                {
                    string value = null;
                    var gender = "U";
                    if (token.Type == JTokenType.String)
                        value = token.Value<string>();
                    else if (token.Type == JTokenType.Object)
                    {
                        var givenName = token["given_name"] ?? token["given"];
                        if (givenName != null) value = givenName.Value<string>();
                        var genderToken = token["gender"];
                        if (genderToken != null && !string.IsNullOrWhiteSpace(genderToken.Value<string>()))
                            gender = genderToken.Value<string>().Trim().ToUpperInvariant();
                    }
                    HashSet<string> set;
                    if (!_classicHot.TryGetValue(gender, out set)) set = _classicHot["U"];
                    if (!string.IsNullOrWhiteSpace(value)) set.Add(value);
                }
            }
        }

        _hotNames = new List<HotNameEntry>();
        var recentPath = Path.Combine(dataDirectory, "hot_names.json");
        if (File.Exists(recentPath))
        {
            var array = JToken.Parse(File.ReadAllText(recentPath, Encoding.UTF8)) as JArray;
            if (array != null)
            {
                foreach (var token in array)
                {
                    if (token.Type != JTokenType.Object) continue;
                    var given = token.Value<string>("given");
                    if (string.IsNullOrWhiteSpace(given)) continue;
                    var year = token.Value<int?>("year") ?? 0;
                    if (year > 0 && year < 1911) year += 1911;
                    _hotNames.Add(new HotNameEntry
                    {
                        Year = year,
                        Gender = (token.Value<string>("gender") ?? "U").Trim().ToUpperInvariant(),
                        Given = given,
                        Rank = token.Value<int?>("rank") ?? 0,
                    });
                }
            }
        }

        _compoundSurnames = new List<string>();
        var compoundPath = Path.Combine(dataDirectory, "compound_surnames.json");
        if (File.Exists(compoundPath))
        {
            var array = JToken.Parse(File.ReadAllText(compoundPath, Encoding.UTF8)) as JArray;
            if (array != null)
                _compoundSurnames.AddRange(array.Values<string>().Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        _compoundSurnames = _compoundSurnames.Distinct().OrderByDescending(x => x.Length).ToList();
    }

    public IReadOnlyList<CharacterInfo> NamingChars(int minCandidate = 2)
    {
        return _byChar.Values.Where(c => c.Candidate >= minCandidate && c.Stroke > 0).ToList();
    }

    public IReadOnlyList<CharacterInfo> AllChars()
    {
        return _byChar.Values.ToList();
    }

    public int? TryStroke(string ch)
    {
        CharacterInfo info;
        if (_byChar.TryGetValue(ch, out info) && info.Stroke > 0) return info.Stroke;
        return null;
    }

    public int StrokeOf(string ch)
    {
        var stroke = TryStroke(ch);
        if (stroke.HasValue) return stroke.Value;
        throw new KeyNotFoundException("字典無「" + ch + "」之康熙筆畫。");
    }

    public string WuxingOf(string ch)
    {
        CharacterInfo info;
        return _byChar.TryGetValue(ch, out info) ? info.Wuxing : "";
    }

    public CharacterInfo Get(string ch)
    {
        CharacterInfo info;
        return _byChar.TryGetValue(ch, out info) ? info : null;
    }

    public bool TryGet(string ch, out CharacterInfo info)
    {
        return _byChar.TryGetValue(ch, out info);
    }

    public bool IsHotGiven(string given)
    {
        return _classicHot.Values.Any(x => x.Contains(given)) || _hotNames.Any(x => x.Given == given);
    }

    public bool IsClassicHot(string given, string gender)
    {
        HashSet<string> set;
        if (_classicHot["U"].Contains(given)) return true;
        return _classicHot.TryGetValue((gender ?? "U").ToUpperInvariant(), out set) && set.Contains(given);
    }

    public bool IsRecentHot(string given, string gender, int years, int rankLimit, int refYear)
    {
        var normalizedGender = (gender ?? "U").ToUpperInvariant();
        var start = refYear - Math.Max(1, years) + 1;
        return _hotNames.Any(x => x.Given == given && x.Year >= start && x.Year <= refYear &&
            (x.Gender == "U" || x.Gender == normalizedGender) &&
            (x.Rank <= 0 || x.Rank <= Math.Max(1, rankLimit)));
    }

    public int HotRankPenalty(string given, string gender, int refYear)
    {
        var start = refYear - 4;
        var matches = _hotNames.Where(x => x.Given == given && x.Year >= start && x.Year <= refYear)
            .Where(x => x.Gender == "U" || x.Gender == (gender ?? "U").ToUpperInvariant())
            .ToList();
        var penalty = 0;
        if (matches.Count > 0)
        {
            var bestRank = matches.Where(x => x.Rank > 0).Select(x => x.Rank).DefaultIfEmpty(100).Min();
            penalty += bestRank <= 10 ? 10 : bestRank <= 30 ? 8 : 6;
        }
        if (IsClassicHot(given, gender)) penalty += 9;
        return penalty;
    }

    public string SoundZhuyin(string ch)
    {
        var info = Get(ch);
        return info == null ? "" : info.Zhuyin;
    }

    public string SoundPinyin(string ch)
    {
        var info = Get(ch);
        return info == null ? "" : info.Pinyin;
    }

    public int ToneOf(string ch)
    {
        var info = Get(ch);
        if (info == null) return 0;
        var tone = ParseInt(info.Tone);
        if (tone > 0) return tone;
        var py = info.Pinyin ?? "";
        return py.Length > 0 && char.IsDigit(py[py.Length - 1]) ? py[py.Length - 1] - '0' : 0;
    }

    public IReadOnlyList<string> CompoundSurnames()
    {
        return _compoundSurnames;
    }

    private static string FindDataDir()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        for (var i = 0; i < 10 && dir != null; i++)
        {
            var p1 = Path.Combine(dir.FullName, "characters.seed.csv");
            var p2 = Path.Combine(dir.FullName, "Data", "characters.seed.csv");
            var p3 = Path.Combine(dir.FullName, "_gen", "characters.seed.csv");
            if (File.Exists(p1)) return dir.FullName;
            if (File.Exists(p2)) return Path.Combine(dir.FullName, "Data");
            if (File.Exists(p3)) return Path.Combine(dir.FullName, "_gen");
            dir = dir.Parent;
        }
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    }

    private static List<string> SplitCsv(string line)
    {
        var list = new List<string>();
        var cur = new System.Text.StringBuilder();
        var inQ = false;
        foreach (var ch in line)
        {
            if (ch == '"') { inQ = !inQ; continue; }
            if (ch == ',' && !inQ) { list.Add(cur.ToString()); cur.Clear(); continue; }
            cur.Append(ch);
        }
        list.Add(cur.ToString());
        return list;
    }

    private static int ParseInt(string value)
    {
        int result;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;
    }

    private static int ParseIntDefault(string value, int fallback)
    {
        int result;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : fallback;
    }
}
}
