namespace Mingxu.Core.Liunian
{
    /// <summary>
    /// 流年四大面向標籤。新生兒報告用兒童版用語，成人／流年分析沿用傳統用語。
    /// </summary>
    public static class LiunianDomainLabels
    {
        public static string Career(bool child) => child ? "學習／發展" : "事業";
        public static string Wealth(bool child) => child ? "資源／環境" : "財運";
        /// <summary>完整標籤（區塊標題）。</summary>
        public static string Relationship(bool child) => child ? "人際／互動" : "感情／人際";
        /// <summary>短標籤（圖表／表格欄）。</summary>
        public static string RelationshipShort(bool child) => child ? "人際／互動" : "感情";
        public static string Life(bool child) => child ? "成長／生活" : "生活";

        public static string TriadLead(bool child) =>
            child ? "學習／發展／資源／環境／人際／互動評等走勢" : "事業／財運／感情評等走勢";
    }
}
