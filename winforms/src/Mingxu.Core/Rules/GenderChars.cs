using System.Collections.Generic;

namespace Mingxu.Core.Rules
{
public static class GenderChars
{
    public static readonly HashSet<char> Female = new HashSet<char>(
        ("女母媽婦妻姐妹妹姨姑婆奶妾妃嬪嬤妞姆姊姍姬妮妲姝姸姣娘娜娟娥娩娑娓娴娸婭婷媚媛嫦嬋嬌嬈妤妏嫣媜媗媄媞" +
         "嫻嬅嬡姵妘妡姳娪婌婥嫆嬿媃坤婷玲芬芳淑惠美雅怡雯欣柔萱婕琳瑤瑩瑄琪珊慧慈寧靜婉清薇蓉荷蓮蘭梅桃櫻" +
         "霏妍晴詠苡菲語涵蓁詩珮甄嵐芯芮茜茹茵苑芙黛釵裙霜月花香粉紫翠胭心妙巧繡绣綉綺絹絲紗縷纓鳳凰鶯燕蝶菊芙莉菱芝菁蕙珮瑤璇璃瓊" +
         "雪霞露雲夢思悅吟琴笛笙簫").ToCharArray());

    public static readonly HashSet<char> Male = new HashSet<char>(
        ("男父夫兄弟公翁婿郎漢爺伯叔舅甥哥爸爹士乾偉傑豪強國德智俊彥毅謙朗晨陽昇昊佑祥瑞霖霆赫奕皓晟昀昕昭景曜燦煥煜煒" +
         "柏澤浩然嘉宏翔承碩睿軒宸哲建志明杰騰鋒銘凱昱桓祺禧剛勇猛雄武軍威震彪斌釗鈞銓錦銳鋼鎧勛勳宗忠義烈健壯盛鼎泰康祿虎豹龍楠").ToCharArray());

    private static readonly string[] FemaleMeaning = { "女子", "女性", "女德", "女也", "婦也", "母也", "妻也" };
    private static readonly string[] MaleMeaning = { "男子", "男性", "男兒", "男也", "父也", "夫也", "郎也", "雄也" };

    static GenderChars()
    {
        var overlap = new HashSet<char>(Female);
        overlap.IntersectWith(Male);
        Female.ExceptWith(overlap);
        Male.ExceptWith(overlap);
    }

    public static bool IsFeminine(string ch, string meaning)
    {
        return ch != null && ch.Length == 1 &&
            (Female.Contains(ch[0]) || ContainsAny(meaning, FemaleMeaning));
    }

    public static bool IsMasculine(string ch, string meaning)
    {
        return ch != null && ch.Length == 1 &&
            (Male.Contains(ch[0]) || ContainsAny(meaning, MaleMeaning));
    }

    public static bool GenderMismatch(string ch, string gender, string zibei, string meaning)
    {
        if (string.IsNullOrEmpty(ch) || (!string.IsNullOrEmpty(zibei) && ch == zibei)) return false;
        if (gender == "M") return IsFeminine(ch, meaning);
        if (gender == "F") return IsMasculine(ch, meaning);
        return false;
    }

    private static bool ContainsAny(string text, IEnumerable<string> keys)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (var key in keys) if (text.Contains(key)) return true;
        return false;
    }
}
}
