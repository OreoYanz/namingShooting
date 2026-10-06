using Mingxu.Core.Models;

namespace Mingxu.Core.Scoring
{
public sealed class HotNameResult
{
    public double Penalty { get; set; }
    public string Note { get; set; }
}

public static class HotNames
{
    public const double PenaltyRecentHot = 6.0;
    public const double PenaltyClassicHot = 10.0;
    public const double StrongMultiplier = 1.6;

    public static HotNameResult Penalty(string given, string gender, int refYear,
        int years, int rankLimit, bool excludeRecent, bool excludeClassic,
        ICharacterRepository repo)
    {
        var penalty = 0.0;
        var note = "";
        if (repo.IsRecentHot(given, gender, years, rankLimit, refYear))
        {
            var p = PenaltyRecentHot * (excludeRecent ? StrongMultiplier : 0.85);
            penalty += p;
            note = "近" + years + "年熱門名（降權 " + p.ToString("0") + "）";
        }
        if (repo.IsClassicHot(given, gender))
        {
            var p = PenaltyClassicHot * (excludeClassic ? StrongMultiplier : 0.9);
            penalty += p;
            if (note.Length > 0) note += "；";
            note += "經典常見名（降權 " + p.ToString("0") + "）";
        }
        return new HotNameResult { Penalty = System.Math.Round(penalty, 1), Note = note };
    }
}
}
