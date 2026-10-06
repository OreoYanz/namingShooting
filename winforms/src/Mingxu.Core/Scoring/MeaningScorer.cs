using System;
using System.Collections.Generic;
using System.Linq;
using Mingxu.Core.Models;

namespace Mingxu.Core.Scoring
{
public sealed class MeaningScoreResult
{
    public double Score { get; set; }
    public List<string> Notes { get; set; }
    public int CategoryCount { get; set; }
    public int ImageryCount { get; set; }
    public int AllusionCount { get; set; }
    public bool KnownCombo { get; set; }
    public string ComboGloss { get; set; }
}

public static class MeaningScorer
{
    private static readonly string[] Positive =
    {
        "清雅","榮貴","荣贵","成功","隆昌","福壽","福寿","吉祥","幸福","溫和","温和",
        "賢淑","贤淑","英俊","多才","巧智","聰穎","聪颖","智勇","環境良好","环境良好",
        "貴人","贵人","興旺","兴旺","昌盛","秀麗","秀丽","秀氣","秀气","伶俐","安穩",
        "安稳","享福","榮華","荣华","蓬勃","茂盛","正直","誠信","仁厚","高雅","優雅",
        "优雅","光明","和諧"
    };
    private static readonly string[] Negative =
    {
        "孤獨","孤独","刑克","刑偶","傷子","伤子","欠子","克父","克母","克妻","多災",
        "多灾","災厄","灾厄","勞苦","劳苦","奔波","潦倒","困苦","短壽","短寿","病弱",
        "牢獄","牢狱","不祥","破敗","破败","事勞無功","事劳无功","憂心","忧心","勞神",
        "劳神","再嫁","雙妻","双妻","忌車","忌车","怕水","家破","人亡","殺人","杀人",
        "被殺","被杀","失常","無光","无光","暗淡"
    };
    private static readonly HashSet<char> Imagery = new HashSet<char>(
        "林清明華玉蘭竹梅菊荷蓮松柏雲海山川風月星陽昊翔鵬飛安平寧康文詩書德仁義智信恩澤軒宇宸彤萱芷芃婷雅涵睿哲豪傑剛毅家國永遠博宏錦繡辰晨".ToCharArray());
    private static readonly HashSet<char> Allusion = new HashSet<char>(
        "芃萱芷蘭竹梅菊荷蓮鵬鯤玉德寧明新文信仁澤海山川風月辰軒宸彤涵睿哲恩安平康翔宇家國".ToCharArray());
    private static readonly HashSet<string> KnownCombos = new HashSet<string>
    {
        "家豪","承恩","宇恩","淑芬","美玲","志明","建宏","雅婷","怡君","冠宇","子軒",
        "詩涵","雨萱","宥廷","品睿","芃宇","明哲","厚德","致遠","清和","文傑","俊傑",
        "安然","宇航","芯語","思遠","思齊","思源","明德","明遠","明軒","明睿","文軒",
        "文博","文彥","俊宇","俊宏","俊豪","俊彥","志遠","志宏","志豪","承澤","承宇",
        "承翰","浩然","浩宇","浩軒","博文","博宇","博雅","睿哲","睿恩","睿涵","雅涵",
        "雅萱","雅雯","怡安","怡萱","欣怡","欣妍","語涵","語晴","雨晴","雨涵","子涵",
        "子晴","宥安","宥辰","宇辰","宇軒","宇翔","晨宇","晨曦","晨希","嘉恩","嘉慧",
        "嘉寧","嘉祐","品妍","品涵","清雅","清寧","清遠","若水","若涵","若蘭","芷涵"
    };
    private static readonly Dictionary<string, string[]> Categories = new Dictionary<string, string[]>
    {
        {"德行", new[] {"仁","義","禮","智","信","德","忠","孝","廉","正","誠","善","恕"}},
        {"才學", new[] {"才","智","慧","敏","學","文","書","詩","雅","聰","巧","睿","哲"}},
        {"志向", new[] {"志","遠","雄","偉","博","宏","鴻","鵬","翔","昇","進","達","成"}},
        {"剛健", new[] {"剛","勇","武","強","毅","豪","英","傑","威","壯"}},
        {"柔美", new[] {"美","麗","柔","婉","秀","婷","妍","嫣","娜","姿"}},
        {"自然", new[] {"山","水","林","木","花","雲","雨","風","月","日","星","海","川"}},
        {"草木", new[] {"蘭","竹","梅","菊","荷","蓮","松","柏","芷","萱","芃","茂"}},
        {"光采", new[] {"光","明","輝","曜","昊","陽","昱","晶","燦","彩","華","榮"}},
        {"安泰", new[] {"安","平","寧","定","泰","康","吉","祥","福","壽","和","順","樂"}},
        {"富貴", new[] {"富","貴","財","祿","金","玉","寶","珍","錦","盛","隆","昌"}}
    };
    private static readonly Dictionary<char, string> ImageryText = new Dictionary<char, string>
    {
        {'林',"林木成蔭"},{'清',"清水明志"},{'明',"日月光明"},{'華',"繁花似錦"},
        {'玉',"溫潤如玉"},{'蘭',"空谷幽蘭"},{'竹',"虛心有節"},{'梅',"凌寒獨開"},
        {'菊',"東籬清芳"},{'荷',"出淤泥不染"},{'蓮',"清雅脫俗"},{'松',"蒼松長青"},
        {'柏',"堅貞不移"},{'雲',"高遠自在"},{'海',"海納百川"},{'山',"高山仰止"},
        {'川',"川流不息"},{'風',"清風徐來"},{'月',"明月清輝"},{'星',"星河璀璨"},
        {'陽',"陽光普照"},{'昊',"昊天廣大"},{'翔',"展翅高翔"},{'鵬',"志在千里"},
        {'安',"安然若素"},{'寧',"寧靜致遠"},{'文',"文質彬彬"},{'德',"厚德載物"},
        {'仁',"仁者愛人"},{'信',"一諾千金"},{'澤',"惠澤廣被"},{'軒',"氣宇軒昂"},
        {'宇',"氣宇不凡"},{'萱',"萱草忘憂"},{'芷',"芳芷香草"},{'芃',"草木茂盛"},
        {'雅',"溫文爾雅"},{'涵',"涵泳義理"},{'睿',"睿智明達"},{'哲',"知人則哲"},
        {'遠',"志存高遠"},{'博',"博學篤志"},{'宏',"宏圖大展"},{'錦',"錦繡前程"},
        {'辰',"良辰美景"},{'晨',"朝氣清新"}
    };
    private static readonly Dictionary<char, string> AllusionText = new Dictionary<char, string>
    {
        {'芃',"《詩·鄘風·載馳》「芃芃其麥」"},{'萱',"《詩·衛風·伯兮》萱草忘憂"},
        {'芷',"屈原《離騷》香草意象"},{'蘭',"孔子芝蘭、屈賦蘭芷"},
        {'竹',"王徽之「何可一日無此君」"},{'梅',"林逋梅妻鶴子"},
        {'菊',"陶淵明「採菊東籬下」"},{'荷',"周敦頤《愛蓮說》"},
        {'蓮',"周敦頤《愛蓮說》"},{'鵬',"《莊子·逍遙遊》鯤鵬"},
        {'鯤',"《莊子·逍遙遊》北冥有魚"},{'玉',"《禮記》君子比德於玉"},
        {'德',"《易》厚德載物"},{'寧',"諸葛亮「寧靜致遠」"},
        {'明',"《大學》明明德"},{'新',"《大學》日日新"},
        {'文',"《論語》文質彬彬"},{'信',"《論語》人而無信"},
        {'仁',"《論語》仁者愛人"},{'澤',"《孟子》惠澤傳統"},
        {'山',"《詩·小雅》高山仰止"},{'川',"《論語》子在川上"},
        {'彤',"《詩·邶風·靜女》貽我彤管"},{'涵',"朱熹論讀書涵泳"},
        {'睿',"《書》睿作聖"},{'哲',"《書》知人則哲"},
        {'安',"《大學》定靜而後能安"},{'平',"《書》地平天成"},
        {'翔',"《論語》翔而後集"},{'家',"《大學》齊家治國"}
    };

    public static MeaningScoreResult Score(string given, IList<CharacterInfo> chars)
    {
        var raw = 0;
        var notes = new List<string>();
        var positiveChars = 0;
        var negativeChars = 0;
        var imageryCount = 0;
        var allusionCount = 0;
        var categoryHits = new HashSet<string>();
        foreach (var info in chars)
        {
            var meaning = info == null ? "" : info.Meaning ?? "";
            if (meaning.Length == 0 && (info == null || !Imagery.Contains(info.Char[0]))) raw -= 4;
            var pos = Positive.Count(meaning.Contains);
            var neg = Negative.Count(meaning.Contains);
            if (pos > 0) positiveChars++;
            if (neg > 0) negativeChars++;
            raw += Math.Min(24, pos * 8);
            raw -= neg * 12;
            if (info != null && Imagery.Contains(info.Char[0])) imageryCount++;
            if (info != null && Allusion.Contains(info.Char[0])) allusionCount++;
            if (info != null)
                foreach (var category in Categories)
                    if (category.Value.Any(k => info.Char.Contains(k) || meaning.Contains(k)))
                        categoryHits.Add(category.Key);
        }
        raw += Math.Min(18, imageryCount * 6);
        raw += Math.Min(20, allusionCount * 10);
        if ((given ?? "").Length >= 2)
        {
            raw += KnownCombos.Contains(given.Substring(0, 2)) ? 15 : 6;
            if (positiveChars > 0 && negativeChars > 0) raw -= 8;
            if (positiveChars >= 2 && negativeChars == 0) raw += 5;
        }
        var known = (given ?? "").Length >= 2 && KnownCombos.Contains(given.Substring(0, 2));
        notes.Add("字義分類：" + (categoryHits.Count == 0 ? "中性" : string.Join("、", categoryHits)));
        notes.Add("正面字義 " + positiveChars + "；負面字義 " + negativeChars);
        notes.Add("文學意象 " + imageryCount + "；典故字 " + allusionCount);
        var imageryNotes = (given ?? "").Where(ImageryText.ContainsKey)
            .Select(ch => "「" + ch + "」" + ImageryText[ch]).Take(2).ToList();
        notes.AddRange(imageryNotes);
        notes.AddRange((given ?? "").Where(AllusionText.ContainsKey)
            .Select(ch => "典故「" + ch + "」：" + AllusionText[ch]).Take(2));
        if ((given ?? "").Length >= 2)
            notes.Add(known ? "命中常見兩字語意，組合自然" : "採兩字意象與分類合成語意");
        return new MeaningScoreResult
        {
            Score = Math.Max(0, Math.Min(100, Math.Round(50.0 + raw, 1))),
            Notes = notes,
            CategoryCount = categoryHits.Count,
            ImageryCount = imageryCount,
            AllusionCount = allusionCount,
            KnownCombo = known,
            ComboGloss = known ? "常見姓名組合，語意連貫" :
                (categoryHits.Count > 0 ? "會合「" + string.Join("、", categoryHits.Take(2)) + "」意象" : "依個別字義合參")
        };
    }
}
}
