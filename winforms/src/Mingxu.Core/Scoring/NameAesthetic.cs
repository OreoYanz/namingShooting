using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Core.Scoring
{
public sealed class NameAestheticResult
{
    public double Score { get; set; }
    public double GlyphBalance { get; set; }
    public double SemanticHarmony { get; set; }
    public double ImageryCohesion { get; set; }
    public List<string> Notes { get; set; }
}

public static class NameAesthetic
{
    public static NameAestheticResult Score(string given, IList<CharacterInfo> profiles,
        MeaningScoreResult meaning, PhonologyScoreResult phonology)
    {
        var valid = profiles.Where(x => x != null).ToList();
        var glyph = StrokeBalance(valid);
        var semantic = meaning == null ? 50 : Math.Min(92,
            58 + meaning.CategoryCount * 5 + meaning.ImageryCount * 3 +
            (meaning.KnownCombo ? 14 : given.Length >= 2 ? 6 : 2));
        var imagery = meaning == null ? 50 : Math.Min(95,
            55 + meaning.ImageryCount * 5 + meaning.AllusionCount * 5 +
            (phonology != null && phonology.Score >= 75 ? 5 : 0));
        var total = Math.Round(glyph * .32 + semantic * .38 + imagery * .30, 1);
        return new NameAestheticResult
        {
            Score = total,
            GlyphBalance = glyph,
            SemanticHarmony = semantic,
            ImageryCohesion = imagery,
            Notes = new List<string>
            {
                glyph >= 80 ? "筆畫繁簡均衡，視覺協調" : glyph >= 68 ? "筆畫略有落差，整體仍順眼" : "筆畫落差較大",
                semantic >= 78 ? "字義互補，語意連貫" : "字義組合尚稱自然",
                imagery >= 75 ? "意象鮮明，名字有畫面感" : "意象平實"
            }
        };
    }

    public static double BlendAestheticTotal(double baseTotal, double aestheticScore)
    {
        return Math.Round(baseTotal * .88 + aestheticScore * .12, 1);
    }

    private static double StrokeBalance(IList<CharacterInfo> profiles)
    {
        if (profiles.Count == 0) return 50;
        if (profiles.Count == 1)
            return profiles[0].Stroke >= 6 && profiles[0].Stroke <= 14 ? 78 : 62;
        var diff = Math.Abs(profiles[0].Stroke - profiles[1].Stroke);
        var avg = profiles.Average(x => x.Stroke);
        if (diff <= 3 && avg >= 6 && avg <= 14) return 88;
        return diff <= 6 ? 74 : 58;
    }
}
}
