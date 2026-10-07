using System;
using System.Collections.Generic;

namespace Mingxu.Core.Models
{

public sealed class Pillar
{
    public string Gan { get; set; } = "";
    public string Zhi { get; set; } = "";
    public string GanWuxing { get; set; } = "";
    public string ZhiWuxing { get; set; } = "";
    public string ShishenGan { get; set; } = "";
    public List<string> HideGans { get; set; } = new List<string>();
    public List<string> HideShishen { get; set; } = new List<string>();
    public string Ganzhi => Gan + Zhi;
}

public sealed class Pillars
{
    public Pillar Year { get; set; } = new Pillar();
    public Pillar Month { get; set; } = new Pillar();
    public Pillar Day { get; set; } = new Pillar();
    public Pillar Hour { get; set; } = new Pillar();
    public string DayMaster { get; set; } = "";
    public string DayMasterWuxing { get; set; } = "";
    public string Strength { get; set; } = "中和";
    public int Strength100 { get; set; } = 50;
    public Dictionary<string, int> ElementScores { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, string> ElementLevels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, double> ElementShare { get; set; } = new Dictionary<string, double>();
    public List<string> XiYong { get; set; } = new List<string>();
    public List<string> XiCi { get; set; } = new List<string>();
    public List<string> JiShen { get; set; } = new List<string>();
    public List<string> JiCi { get; set; } = new List<string>();
    public List<string> TiaoHou { get; set; } = new List<string>();
    public List<string> Notes { get; set; } = new List<string>();
    public string LunarText { get; set; } = "";
    public string Zodiac { get; set; } = "";
    public string YearBranch { get; set; } = "";
    public string BirthPlace { get; set; } = "";
    public double Longitude { get; set; }
    public bool UseTrueSolar { get; set; }
    public string Gender { get; set; } = "M";
}

public sealed class WugeResult
{
    public string Surname { get; set; } = "";
    public string Given { get; set; } = "";
    public string FullName => Surname + Given;
    public int Tian { get; set; }
    public int Ren { get; set; }
    public int Di { get; set; }
    public int Wai { get; set; }
    public int Zong { get; set; }
    public string TianWx { get; set; } = "";
    public string RenWx { get; set; } = "";
    public string DiWx { get; set; } = "";
    public string WaiWx { get; set; } = "";
    public string ZongWx { get; set; } = "";
    public string TianLuck { get; set; } = "";
    public string RenLuck { get; set; } = "";
    public string DiLuck { get; set; } = "";
    public string WaiLuck { get; set; } = "";
    public string ZongLuck { get; set; } = "";
    public string Sancai { get; set; } = "";
    public string SancaiLuck { get; set; } = "";
    public string SancaiNote { get; set; } = "";
    public double Score { get; set; }
    public List<string> Notes { get; set; } = new List<string>();
    public List<CharStroke> CharStrokes { get; set; } = new List<CharStroke>();
}

public sealed class CharStroke
{
    public string Char { get; set; }
    public int Stroke { get; set; }

    public CharStroke(string character, int stroke)
    {
        Char = character;
        Stroke = stroke;
    }
}

public sealed class WugeComparison
{
    public WugeResult Original { get; set; }
    public WugeResult NewName { get; set; }

    public int TianDelta { get; set; }
    public int RenDelta { get; set; }
    public int DiDelta { get; set; }
    public int WaiDelta { get; set; }
    public int ZongDelta { get; set; }

    public double ScoreDelta { get; set; }

    public string SancaiChange { get; set; } = "";
    public string Summary { get; set; } = "";
}

public sealed class RenameComparison
{
    public string OriginalName { get; set; } = "";
    public string NewName { get; set; } = "";

    public WugeComparison Wuge { get; set; }

    public double BaziOld { get; set; }
    public double BaziNew { get; set; }
    public double BaziDelta { get; set; }

    public double WuxingOld { get; set; }
    public double WuxingNew { get; set; }
    public double WuxingDelta { get; set; }

    public double WugeOld { get; set; }
    public double WugeNew { get; set; }
    public double WugeDelta { get; set; }

    public double PhonologyOld { get; set; }
    public double PhonologyNew { get; set; }
    public double PhonologyDelta { get; set; }

    public double MeaningOld { get; set; }
    public double MeaningNew { get; set; }
    public double MeaningDelta { get; set; }

    public double ZodiacOld { get; set; }
    public double ZodiacNew { get; set; }
    public double ZodiacDelta { get; set; }

    public double StyleOld { get; set; }
    public double StyleNew { get; set; }
    public double StyleDelta { get; set; }

    public double RarityOld { get; set; }
    public double RarityNew { get; set; }
    public double RarityDelta { get; set; }

    public double TotalOld { get; set; }
    public double TotalNew { get; set; }
    public double TotalDelta { get; set; }

    public List<string> ImprovedItems { get; set; } = new List<string>();
    public List<string> DeclinedItems { get; set; } = new List<string>();

    public string RecommendReason { get; set; } = "";
    public string Conclusion { get; set; } = "";
    public string Summary { get; set; } = "";
}

public sealed class CurrentNameDiagnosis
{
    public string FullName { get; set; } = "";
    public double Total { get; set; }
    public List<string> Strengths { get; set; } = new List<string>();
    public List<string> Weaknesses { get; set; } = new List<string>();
    public string Summary { get; set; } = "";
    public string ReportText { get; set; } = "";
}

public sealed class NameSuggestion
{
    public string Surname { get; set; } = "";
    public string Given { get; set; } = "";
    public string FullName => Surname + Given;
    public double Total { get; set; }
    public string Grade { get; set; } = "";
    public double BaziScore { get; set; }
    public double WuxingScore { get; set; }
    public double WugeScore { get; set; }
    public double PhonologyScore { get; set; }
    public double MeaningScore { get; set; }
    public double ZodiacScore { get; set; }
    public double StyleScore { get; set; }
    public double RarityScore { get; set; }
    public double AestheticScore { get; set; }
    public double ParentScore { get; set; } = 100;
    public bool ParentUsed { get; set; }
    public double RankBonus { get; set; }
    public string ComparisonText { get; set; } = "";
    public RenameComparison RenameComparison { get; set; }
    public List<string> CharWuxing { get; set; } = new List<string>();
    public List<string> Meanings { get; set; } = new List<string>();
    public List<string> Reasons { get; set; } = new List<string>();
    public string ReasonText { get; set; } = "";
    public WugeResult Wuge { get; set; }
}

public sealed class MonthLuck
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthGanzhi { get; set; } = "";
    public string SolarTermRange { get; set; } = "";
    public double BaziScore { get; set; }
    public double NameScore { get; set; }
    public double ZodiacScore { get; set; }
    public double TotalScore { get; set; }
    public string Level { get; set; } = "";
    public string Overall { get; set; } = "";
    public string Career { get; set; } = "";
    public string Wealth { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string Life { get; set; } = "";
    public List<string> Suitable { get; set; } = new List<string>();
    public List<string> Avoid { get; set; } = new List<string>();
    public string Advice { get; set; } = "";
    public string Summary { get; set; } = "";
}

public sealed class YearLuck
{
    public int Year { get; set; }
    public int Age { get; set; }
    public string Ganzhi { get; set; } = "";
    public string Animal { get; set; } = "";
    public double BaziScore { get; set; }
    public double NameScore { get; set; }
    public double ZodiacScore { get; set; }
    public double TotalScore { get; set; }
    public double CareerScore { get; set; }
    public double WealthScore { get; set; }
    public double RelationshipScore { get; set; }
    public string Level { get; set; } = "";
    public string Keyword { get; set; } = "";
    public string Summary { get; set; } = "";
    public string DaYun { get; set; } = "";
    public string BaziNote { get; set; } = "";
    public string NameNote { get; set; } = "";
    public string ZodiacNote { get; set; } = "";
    public string Overall { get; set; } = "";
    public string Career { get; set; } = "";
    public string Wealth { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string Life { get; set; } = "";
    public List<string> Suitable { get; set; } = new List<string>();
    public List<string> Avoid { get; set; } = new List<string>();
    public List<string> Risks { get; set; } = new List<string>();
    public List<string> Advice { get; set; } = new List<string>();
    public string Outlook { get; set; } = "";
    public string MonthGuideCareer { get; set; } = "";
    public string MonthGuideWealth { get; set; } = "";
    public string MonthGuideRelationship { get; set; } = "";
    public List<int> StrongMonths { get; set; } = new List<int>();
    public List<int> StableMonths { get; set; } = new List<int>();
    public List<int> AdjustMonths { get; set; } = new List<int>();
    public List<int> CautionMonths { get; set; } = new List<int>();
    public List<MonthLuck> Months { get; set; } = new List<MonthLuck>();
}

public sealed class AnalysisRequest
{
    public string Mode { get; set; } = "newborn"; // newborn / rename / liunian
    public string Gender { get; set; } = "M";
    public DateTime Birth { get; set; }
    public string Surname { get; set; } = "";
    public string CurrentFullName { get; set; } = "";
    public string BirthPlace { get; set; } = "台北市";
    public double Longitude { get; set; } = 121.56;
    public bool UseTrueSolar { get; set; } = true;
    public string FatherName { get; set; } = "";
    public string MotherName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public int ResultCount { get; set; } = 30;
    /// <summary>相容舊欄位：若未設起迄年，以「今年起算 N 年」推算。</summary>
    public int LiunianYears { get; set; } = 10;
    public int LiunianStartYear { get; set; }
    public int LiunianEndYear { get; set; }
    public bool IncludeLiunianMonths { get; set; } = true;
    /// <summary>standard｜detailed</summary>
    public string LiunianMonthDetail { get; set; } = "detailed";
    public int LiunianYearCount
    {
        get
        {
            int start, end;
            ResolveLiunianRange(out start, out end);
            return end - start + 1;
        }
    }

    public void ResolveLiunianRange(out int startYear, out int endYear)
    {
        if (LiunianStartYear > 0 && LiunianEndYear > 0)
        {
            startYear = Math.Min(LiunianStartYear, LiunianEndYear);
            endYear = Math.Max(LiunianStartYear, LiunianEndYear);
            return;
        }
        startYear = DateTime.Now.Year;
        var n = LiunianYears <= 0 ? 10 : Math.Min(50, LiunianYears);
        endYear = startYear + n - 1;
    }

    public bool AllowSingle { get; set; } = true;
    public bool AllowDouble { get; set; } = true;
    public string NamingMode { get; set; } = "balanced";
    public string ExplorationLevel { get; set; } = "";
    public string PreferredChars { get; set; } = "";
    public string ForbiddenChars { get; set; } = "";
    public string Zibei { get; set; } = "";
    public string RenameReason { get; set; } = "";
    public string ImproveDirections { get; set; } = "";
    public string RenameFocus { get; set; } = "";
    public bool AvoidParentChars { get; set; } = true;
    public int ZibeiPosition { get; set; }
    public int CandidateSeed { get; set; }
    public bool ExcludeHot5y { get; set; }
    public bool ExcludeClassicHot { get; set; }
    public int HotYears { get; set; } = 5;
    public int HotRankLimit { get; set; } = 50;

    /// <summary>以 ChatGPT 產生名之組合（審美優先）；評分仍用本地引擎。</summary>
    public bool UseChatGptGivens { get; set; }
    public string ChatGptApiKey { get; set; } = "";
    public string ChatGptModel { get; set; } = "gpt-4o-mini";
    public string ChatGptBaseUrl { get; set; } = "https://api.openai.com/v1";
    /// <summary>使用者審美／風格描述，會送進提示詞。</summary>
    public string AestheticBrief { get; set; } = "";
}

public sealed class AnalysisResult
{
    public Pillars Pillars { get; set; }
    public Dictionary<string, object> Destiny { get; set; } = new Dictionary<string, object>();
    public List<NameSuggestion> Suggestions { get; set; } = new List<NameSuggestion>();
    public NameSuggestion Current { get; set; }
    public List<YearLuck> Liunian { get; set; } = new List<YearLuck>();
    public string HumanReadableDestiny { get; set; } = "";
}

public sealed class CharacterInfo
{
    public string Char { get; set; } = "";
    public int Stroke { get; set; }
    public string Wuxing { get; set; } = "";
    public int Candidate { get; set; }
    public string Tone { get; set; } = "";
    public string Pinyin { get; set; } = "";
    public string Zhuyin { get; set; } = "";
    public string Meaning { get; set; } = "";
    public string Radical { get; set; } = "";
    public int Frequency { get; set; } = 50;
    public int Rarity { get; set; } = 50;
    public string GenderTag { get; set; } = "U";
    public int Modern { get; set; } = 50;
    public int Classical { get; set; } = 50;
    public int Literary { get; set; } = 50;
    public int Elegant { get; set; } = 50;
    public int Cute { get; set; } = 50;
    public int Neutral { get; set; } = 50;
    public int Strong { get; set; } = 50;
    public int Soft { get; set; } = 50;
}

public interface ICharacterRepository
{
    IReadOnlyList<CharacterInfo> NamingChars(int minCandidate = 2);
    IReadOnlyList<CharacterInfo> AllChars();
    int? TryStroke(string ch);
    int StrokeOf(string ch);
    string WuxingOf(string ch);
    CharacterInfo Get(string ch);
    bool TryGet(string ch, out CharacterInfo info);
    bool IsHotGiven(string given);
    bool IsClassicHot(string given, string gender);
    bool IsRecentHot(string given, string gender, int years, int rankLimit, int refYear);
    int HotRankPenalty(string given, string gender, int refYear);
    string SoundZhuyin(string ch);
    string SoundPinyin(string ch);
    int ToneOf(string ch);
    IReadOnlyList<string> CompoundSurnames();
}

public sealed class DaYunEntry
{
    public int Year { get; set; }
    public string LiuNian { get; set; }
    public string DaYun { get; set; }

    public DaYunEntry(int year, string liuNian, string daYun)
    {
        Year = year;
        LiuNian = liuNian;
        DaYun = daYun;
    }
}
}
