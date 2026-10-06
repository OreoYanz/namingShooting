# YouTube Shorts 自動上傳設定

產生當日包完成後，可自動把 `short/daily_YYYYMMDD.mp4` 上傳成 YouTube Shorts。

## 1. Google Cloud

1. 開啟 [Google Cloud Console](https://console.cloud.google.com/)
2. 建立（或選取）專案
3. **API 和服務 → 資料庫** → 啟用 **YouTube Data API v3**
4. **API 和服務 → 憑證 → 建立憑證 → OAuth 用戶端 ID**
   - 應用程式類型選 **桌面應用程式**
5. 下載 JSON，存成：

```
DailyPaperCut/config/youtube_client_secret.json
```

6. 若第一次用 OAuth：到 **OAuth 同意畫面**
   - 使用者類型可選「外部」
   - 測試期間把你的 Google 帳號加進「測試使用者」
   - 範圍會在授權時要求 `youtube.upload`

## 2. 本機授權（只需一次）

> **重要**：OAuth 用戶端類型請選 **桌面應用程式**，不要選「網頁應用程式」。  
> 若下載的 JSON 最外層是 `"web": { ...}`，就會出現 `redirect_uri_mismatch`。  
> 正確應為 `"installed": { ...}`。

```bash
cd DailyPaperCut
pip install google-api-python-client google-auth-oauthlib google-auth-httplib2
python youtube_auth.py
```

瀏覽器登入**要上傳 Shorts 的那個 YouTube 頻道帳號**，允許上傳。  
成功後會產生：

```
DailyPaperCut/config/youtube_token.json
```

（已在 `.gitignore`，勿提交 git）

### 若出現 `redirect_uri_mismatch`

代表憑證類型或重新導向 URI 不對，請擇一：

**A. 建議：改建成桌面應用程式**

1. Google Cloud → **API 和服務 → 憑證**
2. **建立憑證 → OAuth 用戶端 ID**
3. 應用程式類型選 **桌面應用程式**（不是網頁）
4. 下載 JSON，覆蓋 `config/youtube_client_secret.json`
5. 再執行 `python youtube_auth.py`

**B. 堅持用現有「網頁」憑證**

在該 OAuth 用戶端的「已授權的重新導向 URI」加入：

```
http://localhost:8765/
http://127.0.0.1:8765/
```

儲存後再執行 `python youtube_auth.py`。

## 3. `.env`（可選）

```env
# 預設 true：有 token 就自動上傳；設 false 可關閉
AUTO_PUBLISH_YOUTUBE=true

# public / unlisted / private
# public / unlisted / private（預設 private＝不公開）
YOUTUBE_PRIVACY=private

# YOUTUBE_CLIENT_SECRETS=config/youtube_client_secret.json
# YOUTUBE_TOKEN_FILE=config/youtube_token.json
# YOUTUBE_TAGS=名序,每日吉祥,剪紙,Shorts,運勢
```

未授權／沒 token 時會**略過**上傳，不中斷產生與官網發布。

## 4. 行為說明

- 影片：當日 `short/daily_*.mp4`（約 20 秒、直式 1080×1920）
- 標題：`名序｜每日吉祥 {日期}｜{主題} #Shorts`
- 說明：`social/youtube.txt` ＋ `#Shorts`
- 產生流程：產生 → 寄信 → 上官網 → **上傳 YouTube Shorts**

## 5. 常見問題

| 狀況 | 處理 |
|------|------|
| `Access Not Configured` | Cloud 專案未啟用 YouTube Data API v3 |
| `access_denied` / 測試使用者 | OAuth 同意畫面加上你的帳號為測試者 |
| token 過期 | 再跑一次 `python youtube_auth.py` |
| 上傳成功但不是 Shorts 分頁 | 確認為直式、≤60 秒，標題／說明含 `#Shorts` |
