using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;
using Mingxu.Core.Rules;
using Mingxu.Core.Scoring;

namespace Mingxu.Core.Pipeline
{

public sealed class CandidatePoolResult
{
    public List<CharacterInfo> Chars { get; set; }
    public List<CharacterInfo> RankedAll { get; set; }
    public List<string> Notes { get; set; }

    public CandidatePoolResult(List<CharacterInfo> chars, List<CharacterInfo> rankedAll, List<string> notes)
    {
        Chars = chars;
        RankedAll = rankedAll;
        Notes = notes;
    }
}

internal sealed class CharacterRank
{
    public CharacterInfo Character { get; set; }
    public double Score { get; set; }
}

public static class CandidatePoolBuilder
{
    private static readonly Dictionary<string, string[]> MeaningThemes = new Dictionary<string, string[]>()
    {
        ["智慧"] = new string[] { "智", "慧", "敏", "睿", "哲", "明", "聰", "穎", "博", "學", "思" },
        ["品德"] = new string[] { "德", "仁", "義", "信", "誠", "善", "廉", "正", "忠", "孝" },
        ["光明"] = new string[] { "光", "明", "輝", "曜", "昊", "陽", "昱", "晶", "燦", "朗" },
        ["希望"] = new string[] { "希", "望", "盼", "願", "景", "程", "展", "翔", "升", "達" },
        ["成長"] = new string[] { "成", "長", "育", "培", "茂", "盛", "茁", "壯", "進", "升" },
        ["堅毅"] = new string[] { "毅", "堅", "剛", "韌", "恒", "持", "定", "穩", "磐", "勇" },
        ["溫柔"] = new string[] { "柔", "婉", "和", "暖", "溫", "順", "雅", "靜", "恬", "淑" },
        ["才華"] = new string[] { "才", "華", "藝", "技", "巧", "妙", "靈", "秀", "俊", "傑", "英", "彥" },
        ["遠見"] = new string[] { "遠", "宏", "博", "廣", "深", "瞻", "卓", "識", "略", "謀" },
        ["平安"] = new string[] { "安", "平", "寧", "定", "泰", "康", "吉", "祥", "順", "和" },
        ["幸福"] = new string[] { "福", "樂", "喜", "歡", "悦", "悅", "祥", "瑞", "慶", "嘉", "怡" },
    };

    public static CandidatePoolResult Build(
        ICharacterRepository repo,
        Pillars pillars,
        HashSet<char> parentTaboo,
        ExplorationConfig cfg,
        string surname,
        string gender,
        string namingMode,
        IEnumerable<string> forbiddenTokens,
        IEnumerable<string> preferredChars,
        string zibei,
        int seed = 0)
    {
        var rng = new Random(seed == 0 ? Environment.TickCount : seed);
        var mode = NamingModes.Resolve(namingMode);
        var hardChars = HardRules.BlockedCharacters(forbiddenTokens);
        var all = repo.NamingChars(2)
            .Where(c => c.Char.Length == 1)
            .Where(c => !hardChars.Contains(c.Char[0]) && !parentTaboo.Contains(c.Char[0]))
            .Where(c => !GenderChars.GenderMismatch(c.Char, gender, zibei, c.Meaning))
            .ToList();
        if (all.Count < 80)
            all = repo.NamingChars(1)
                .Where(c => c.Char.Length == 1)
                .Where(c => !hardChars.Contains(c.Char[0]) && !parentTaboo.Contains(c.Char[0]))
                .Where(c => !GenderChars.GenderMismatch(c.Char, gender, zibei, c.Meaning))
                .ToList();

        var ranked = all
            .Select(c => new CharacterRank
            {
                Character = c,
                Score = RankScore(c, pillars, mode, gender, parentTaboo, hardChars)
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Character)
            .ToList();

        var notes = new List<string>
        {
            string.Format("探索等級：{0}（{1}）", cfg.Level, LabelOrDefault(cfg.Level)),
            string.Format("字庫可用 {0}，目標池 {1}～{2}", ranked.Count, cfg.PoolMin, cfg.PoolMax),
            "流程：各來源建集→配額抽取→Union"
        };

        var sources = new Dictionary<string, List<CharacterInfo>>();
        sources["穩定Ranking"] = ranked.Take(cfg.StableTop).ToList();
        sources["命理"] = ranked.Where(c => pillars.XiYong.Contains(c.Wuxing) ||
            pillars.XiCi.Contains(c.Wuxing) || pillars.TiaoHou.Contains(c.Wuxing)).Take(120).ToList();
        var xi = pillars.XiYong.Concat(pillars.XiCi).Concat(pillars.TiaoHou).Distinct().ToList();
        sources["五行"] = PickByWuxing(ranked, xi, 100, rng, new HashSet<string>());
        sources["探索·Ranking"] = PickExplore(ranked, cfg.ExploreN + 20, cfg.ExploreFrom,
            cfg.ExploreTo, rng, new HashSet<string>());
        sources["探索·低排名高潛力"] = PickUnderExplored(ranked,
            Math.Min(35, cfg.ExploreN) + 20, pillars, rng, new HashSet<string>());
        sources["探索·組合潛力"] = PickComboPotential(ranked,
            Math.Min(30, cfg.ExploreN) + 20, pillars, rng, new HashSet<string>());
        sources["音韻"] = ranked.OrderByDescending(c => PhonologyScorer.Score(surname, c.Char, repo).Score)
            .Take(80).ToList();
        sources["稀有度"] = PickRarityBand(ranked, 60, rng, new HashSet<string>());
        sources["字義"] = PickMeaningThemes(ranked, MeaningThemes.Count * 20, rng, new HashSet<string>());
        var styleKeys = mode.StylePrefer ?? DefaultStyleKeys(gender);
        sources["風格"] = ranked.OrderByDescending(c => styleKeys.Average(k => NamingModes.StyleValue(c, k)))
            .Take(120).ToList();

        var unionOrder = new string[]
        {
            "穩定Ranking", "命理", "五行", "探索·Ranking", "探索·低排名高潛力",
            "探索·組合潛力", "音韻", "稀有度", "字義", "風格",
        };
        var quotas = ComputeQuotas(cfg.PoolMax, cfg.StableTop, cfg.ExploreN);
        var pooled = new List<CharacterInfo>();
        var recount = new HashSet<string>();
        foreach (var key in unionOrder)
        {
            List<CharacterInfo> chunk;
            if (!sources.TryGetValue(key, out chunk)) continue;
            Shuffle(chunk, rng);
            var quota = quotas[key];
            var added = 0;
            foreach (var c in chunk.Take(quota))
            {
                if (!recount.Add(c.Char)) continue;
                pooled.Add(c);
                added++;
                if (pooled.Count >= cfg.PoolMax) break;
            }
            notes.Add(string.Format("{0} {1}", key, added));
            if (pooled.Count >= cfg.PoolMax) break;
        }

        if (pooled.Count < cfg.PoolMin)
        {
            foreach (var c in ranked)
            {
                if (!recount.Add(c.Char)) continue;
                pooled.Add(c);
                if (pooled.Count >= cfg.PoolMin) break;
            }
        }

        var forced = new List<string>();
        forced.AddRange(preferredChars ?? Enumerable.Empty<string>());
        if (!string.IsNullOrEmpty(zibei)) forced.Insert(0, zibei);
        foreach (var ch in forced.Where(x => !string.IsNullOrEmpty(x) && x.Length == 1).Distinct().Reverse())
        {
            CharacterInfo info;
            if (!repo.TryGet(ch, out info) || !repo.TryStroke(ch).HasValue) continue;
            pooled.RemoveAll(x => x.Char == ch);
            pooled.Insert(0, info);
        }
        if (pooled.Count > cfg.PoolMax) pooled = pooled.Take(cfg.PoolMax).ToList();
        notes.Add(string.Format("Union 候選字 {0}", pooled.Count));
        return new CandidatePoolResult(pooled, ranked, notes);
    }

    public static List<string> BuildGivens(
        List<CharacterInfo> chars,
        bool allowSingle,
        bool allowDouble,
        string zibei,
        int zibeiPosition,
        int seed,
        int maxCombos = 22000)
    {
        if (chars.Count == 0) return new List<string>();
        var rng = new Random(seed == 0 ? 7919 : seed + 7919);
        var givens = new List<string>();
        var seen = new HashSet<string>();
        maxCombos = Math.Max(500, maxCombos);

        var ranked = chars.Select(c => c.Char).Distinct().ToList();

        if (allowSingle)
        {
            if (!string.IsNullOrEmpty(zibei)) AddGiven(givens, seen, zibei);
            else foreach (var c in ranked) AddGiven(givens, seen, c);
        }

        if (allowDouble)
        {
            if (!string.IsNullOrEmpty(zibei))
            {
                foreach (var c in ranked)
                {
                    if (c == zibei) continue;
                    AddGiven(givens, seen, zibeiPosition == 1 ? c + zibei : zibei + c);
                }
                return givens;
            }
            if (ranked.Count <= 120)
            {
                foreach (var a in ranked)
                    foreach (var b in ranked)
                    {
                        if (a == b) continue;
                        AddGiven(givens, seen, a + b);
                        if (givens.Count >= maxCombos) return givens;
                    }
            }
            else
            {
                var split = Math.Max(1, (int)(ranked.Count * .85));
                var quality = ranked.Take(split).ToList();
                var explore = ranked.Skip(split).ToList();
                if (explore.Count == 0) explore = ranked;
                var cumulative = new double[ranked.Count];
                var totalWeight = 0.0;
                for (var i = 0; i < ranked.Count; i++)
                {
                    totalWeight += 1.0 / (1.0 + i * .015);
                    cumulative[i] = totalWeight;
                }
                var attempts = 0;
                while (givens.Count < maxCombos && attempts++ < maxCombos * 12)
                {
                    var firstPool = rng.NextDouble() < .85 ? quality : explore;
                    var a = firstPool[rng.Next(firstPool.Count)];
                    var b = ranked[WeightedIndex(cumulative, totalWeight, rng)];
                    if (a != b) AddGiven(givens, seen, a + b);
                }
                if (givens.Count < maxCombos)
                    foreach (var a in quality)
                        foreach (var b in ranked)
                        {
                            if (a != b) AddGiven(givens, seen, a + b);
                            if (givens.Count >= maxCombos) return givens;
                        }
            }
        }

        return givens;
    }

    private static double RankScore(CharacterInfo c, Pillars pillars, NamingModeProfile mode,
        string gender, HashSet<char> parentTaboo, HashSet<char> hardChars)
    {
        var s = 50.0 + c.Candidate * 8.0;
        s += ElementWeight(c.Wuxing, pillars);
        s += XiYongLevel(c.Wuxing, pillars) * 10;
        s += ZodiacScorer.ZodiacCharBias(pillars.Zodiac, c);
        s += (c.Rarity - 50) * (mode.RarityBias == "rare" ? .35 : .08);
        var styles = mode.StylePrefer ?? DefaultStyleKeys(gender);
        s += styles.Average(k => NamingModes.StyleValue(c, k)) / 10.0;
        if (gender == "F" && GenderChars.IsFeminine(c.Char, c.Meaning)) s += 10;
        if (gender == "M" && GenderChars.IsMasculine(c.Char, c.Meaning)) s += 10;
        if (parentTaboo.Contains(c.Char[0]) || hardChars.Contains(c.Char[0])) s -= 100;
        return s;
    }

    private static double ElementWeight(string wuxing, Pillars pillars)
    {
        int value;
        var scarcity = pillars.ElementScores.TryGetValue(wuxing ?? "", out value)
            ? Math.Max(-12, Math.Min(12, (50 - value) * .25)) : 0;
        if (pillars.XiYong.Contains(wuxing)) scarcity += 12;
        else if (pillars.XiCi.Contains(wuxing)) scarcity += 7;
        else if (pillars.JiShen.Contains(wuxing)) scarcity -= 12;
        else if (pillars.JiCi.Contains(wuxing)) scarcity -= 6;
        return scarcity;
    }

    private static int XiYongLevel(string wuxing, Pillars pillars)
    {
        if (pillars.XiYong.Contains(wuxing)) return 3;
        if (pillars.XiCi.Contains(wuxing)) return 2;
        if (pillars.TiaoHou.Contains(wuxing)) return 1;
        if (pillars.JiShen.Contains(wuxing)) return -2;
        if (pillars.JiCi.Contains(wuxing)) return -1;
        return 0;
    }

    private static List<CharacterInfo> PickByWuxing(
        List<CharacterInfo> ranked, List<string> elements, int limit, Random rng, HashSet<string> seen)
    {
        var outList = new List<CharacterInfo>();
        if (elements.Count == 0) return outList;
        var per = Math.Max(3, limit / elements.Count);
        foreach (var el in elements)
        {
            var bucket = ranked.Where(c => c.Wuxing == el && !seen.Contains(c.Char)).ToList();
            Shuffle(bucket, rng);
            foreach (var c in bucket.Take(per))
            {
                if (!seen.Add(c.Char)) continue;
                outList.Add(c);
                if (outList.Count >= limit) return outList;
            }
        }
        return outList;
    }

    private static List<CharacterInfo> PickExplore(
        List<CharacterInfo> ranked, int count, int from, int to, Random rng, HashSet<string> seen)
    {
        from = Clamp(from, 0, Math.Max(0, ranked.Count - 1));
        to = Clamp(to, from, ranked.Count);
        var band = ranked.Skip(from).Take(Math.Max(0, to - from)).ToList();
        Shuffle(band, rng);
        var outList = new List<CharacterInfo>();
        foreach (var c in band)
        {
            if (!seen.Add(c.Char)) continue;
            outList.Add(c);
            if (outList.Count >= count) break;
        }
        return outList;
    }

    private static List<CharacterInfo> PickUnderExplored(
        List<CharacterInfo> ranked, int count, Pillars pillars, Random rng, HashSet<string> seen)
    {
        var band = ranked
            .Skip(Math.Min(200, ranked.Count))
            .Where(c => pillars.XiYong.Contains(c.Wuxing) || pillars.XiCi.Contains(c.Wuxing))
            .ToList();
        Shuffle(band, rng);
        var outList = new List<CharacterInfo>();
        foreach (var c in band)
        {
            if (!seen.Add(c.Char)) continue;
            outList.Add(c);
            if (outList.Count >= count) break;
        }
        return outList;
    }

    private static List<CharacterInfo> PickComboPotential(
        List<CharacterInfo> ranked, int count, Pillars pillars, Random rng, HashSet<string> seen)
    {
        var band = ranked
            .Where(c => c.Stroke >= 5 && c.Stroke <= 18)
            .Where(c => pillars.XiYong.Contains(c.Wuxing) || pillars.XiCi.Contains(c.Wuxing) || c.Candidate >= 2)
            .ToList();
        Shuffle(band, rng);
        var outList = new List<CharacterInfo>();
        foreach (var c in band)
        {
            if (!seen.Add(c.Char)) continue;
            outList.Add(c);
            if (outList.Count >= count) break;
        }
        return outList;
    }

    private static List<CharacterInfo> PickRarityBand(
        List<CharacterInfo> ranked, int limit, Random rng, HashSet<string> seen)
    {
        var band = ranked.Where(c => c.Frequency >= 28 && c.Frequency <= 72 &&
            c.Rarity >= 35 && c.Rarity <= 78 && !seen.Contains(c.Char)).ToList();
        Shuffle(band, rng);
        var outList = new List<CharacterInfo>();
        foreach (var c in band.Take(limit))
        {
            if (!seen.Add(c.Char)) continue;
            outList.Add(c);
        }
        return outList;
    }

    private static List<CharacterInfo> PickMeaningThemes(
        List<CharacterInfo> ranked, int limit, Random rng, HashSet<string> seen)
    {
        var outList = new List<CharacterInfo>();
        var themes = MeaningThemes.Keys.ToList();
        Shuffle(themes, rng);
        foreach (var theme in themes)
        {
            var keys = MeaningThemes[theme];
            var hits = ranked
                .Where(c => !seen.Contains(c.Char))
                .Where(c => keys.Any(k => c.Char.Contains(k) || (!string.IsNullOrEmpty(c.Meaning) && c.Meaning.Contains(k))))
                .ToList();
            Shuffle(hits, rng);
            foreach (var c in hits.Take(6))
            {
                if (!seen.Add(c.Char)) continue;
                outList.Add(c);
                if (outList.Count >= limit) return outList;
            }
        }
        return outList;
    }

    private static List<CharacterInfo> PickTake(IEnumerable<CharacterInfo> src, int limit, HashSet<string> seen)
    {
        var outList = new List<CharacterInfo>();
        foreach (var c in src)
        {
            if (!seen.Add(c.Char)) continue;
            outList.Add(c);
            if (outList.Count >= limit) break;
        }
        return outList;
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private static void AddSource(Dictionary<string, List<CharacterInfo>> sources, HashSet<string> seen, string name, IEnumerable<CharacterInfo> items)
    {
        var list = new List<CharacterInfo>();
        foreach (var c in items)
        {
            if (!seen.Add(c.Char)) continue;
            list.Add(c);
        }
        sources[name] = list;
    }

    private static void AddGiven(List<string> givens, HashSet<string> seen, string given)
    {
        if (!string.IsNullOrEmpty(given) && seen.Add(given))
            givens.Add(given);
    }

    private static int WeightedIndex(double[] cumulative, double total, Random rng)
    {
        var value = rng.NextDouble() * total;
        var index = Array.BinarySearch(cumulative, value);
        if (index < 0) index = ~index;
        return Math.Min(cumulative.Length - 1, index);
    }

    private static string LabelOrDefault(string level)
    {
        string label;
        return Exploration.Labels.TryGetValue(level, out label) ? label : level;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private static Dictionary<string, int> ComputeQuotas(int poolMax, int stableTop, int exploreN)
    {
        var quotas = new Dictionary<string, int>
        {
            {"穩定Ranking", Math.Min(stableTop, (int)(poolMax * .38))},
            {"命理", (int)(poolMax * .12)},
            {"五行", (int)(poolMax * .10)},
            {"探索·Ranking", exploreN},
            {"探索·低排名高潛力", Math.Min(35, exploreN)},
            {"探索·組合潛力", Math.Min(30, exploreN)},
            {"音韻", (int)(poolMax * .08)},
            {"稀有度", (int)(poolMax * .07)},
            {"字義", MeaningThemes.Count * 7},
            {"風格", 60}
        };
        var total = quotas.Values.Sum();
        if (total <= poolMax) return quotas;
        var scale = (double)poolMax / total;
        return quotas.ToDictionary(x => x.Key, x => Math.Max(1, (int)(x.Value * scale)));
    }

    private static string[] DefaultStyleKeys(string gender)
    {
        if (gender == "M") return new[] {"modern","classical","literary","elegant","strong","neutral"};
        if (gender == "F") return new[] {"modern","literary","elegant","soft","cute","classical"};
        return new[] {"modern","classical","literary","elegant","cute","neutral","strong","soft"};
    }
}
}
