/**
 * 名序官網 · 廣告追蹤 ID 設定
 * 填好後重新部署 docs/（或同步 site → docs）即可生效。
 *
 * Meta Pixel：Events Manager → 資料來源 → Pixel ID（純數字）
 * Google 轉換：Google Ads → 目標 → 轉換 → 標記設定
 *   - googleAdsId：形如 AW-1234567890
 *   - googleAdsConversionLabel：轉換動作標籤（send_to 斜線後段）
 */
window.MX_TRACKING = {
  /** Facebook / Meta Pixel ID，例：123456789012345 */
  metaPixelId: "",

  /** Google Ads 帳戶 ID，例：AW-1234567890 */
  googleAdsId: "",

  /**
   * 「加入 LINE」轉換標籤（勿含 AW- 前綴）。
   * 完整 send_to 會組成：googleAdsId + "/" + googleAdsConversionLabel
   * 例：AbC-D_eFgHiJkLmNoP
   */
  googleAdsConversionLabel: "",

  /** 可選：GA4 評估 ID（G-XXXXXXXX），有填才載入 */
  ga4MeasurementId: "",
};
