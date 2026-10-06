# Meta 發文設定（Facebook／Instagram／Threads）

工作台發布會讀取 `DailyPaperCut/.env`。建議用 **Facebook Login for Business + 長期 Page Token**（自己粉專／自己 IG 商業帳最穩）。

## 0. 帳號先備妥

1. Facebook **粉絲專頁**（Page）
2. Instagram **專業帳號**（商業或創作者），並在粉專設定裡 **綁定同一粉專**
3. Threads 帳號（與 IG 同 Meta 帳號）
4. 到 [Meta for Developers](https://developers.facebook.com/) 建立一個 App（類型選 **Business**）

## 1. 在 App 加入產品

App → 新增產品：

| 產品 | 用途 |
|------|------|
| **Facebook Login for Business**（或 Facebook Login） | 取得使用者／粉專權限 |
| **Instagram**（Instagram API with Facebook Login） | IG 發圖／Reels |
| **Threads API** | Threads 發文 |

開發階段可先把 App 設為 **Development**，並把你的 Meta 帳號加為 App 的 **管理員／開發者／測試者**。自己粉專通常即可發文；若要給其他人用，之後再送 App Review。

## 2. 需要的權限（Permissions）

申請／勾選（名稱隨 Meta 介面略有出入）：

- `pages_show_list`
- `pages_read_engagement`
- `pages_manage_posts`
- `pages_manage_engagement`（建議）
- `instagram_basic`
- `instagram_content_publish`
- `business_management`（若帳號掛在 Business Manager）
- Threads：`threads_basic`、`threads_content_publish`

## 3. 取得短期 User Token（Graph API Explorer）

1. 開啟 [Graph API Explorer](https://developers.facebook.com/tools/explorer/)
2. 右上角選你的 **App**
3. User or Page → 選 **Get User Access Token**
4. 勾選上面權限 → Generate Access Token → 登入授權
5. 先記下這個 **短效 User Token**（約 1～2 小時）

驗證粉專列表：

```http
GET https://graph.facebook.com/v21.0/me/accounts?access_token=USER_TOKEN
```

回傳裡每個粉專會有 `id`、`access_token`（這是 **Page Token**）、`name`。

## 4. 換成「長期」Token（重要）

### 4a. 短效 User → 長效 User（約 60 天）

需要 App 的 **App ID**、**App Secret**（App → 設定 → 基本）：

```http
GET https://graph.facebook.com/v21.0/oauth/access_token
  ?grant_type=fb_exchange_token
  &client_id=APP_ID
  &client_secret=APP_SECRET
  &fb_exchange_token=SHORT_LIVED_USER_TOKEN
```

回傳的 `access_token` = **長效 User Token**。

### 4b. 長效 User → 長效 Page Token（通常不過期）

```http
GET https://graph.facebook.com/v21.0/me/accounts
  ?access_token=LONG_LIVED_USER_TOKEN
```

對應粉專的 `access_token` → 填入 `.env` 的 `META_PAGE_ACCESS_TOKEN`。  
粉專的 `id` → `META_PAGE_ID`。

> 實務上：**長期 Page Token** 最適合工作台伺服器使用。Token 外洩等同可代發文，請勿提交到 git。

## 5. 取得 Instagram Business Account ID

粉專綁定 IG 後：

```http
GET https://graph.facebook.com/v21.0/{PAGE_ID}
  ?fields=instagram_business_account
  &access_token=PAGE_ACCESS_TOKEN
```

回傳例如：

```json
{ "instagram_business_account": { "id": "1789xxxxxxxxxx" }, "id": "..." }
```

把 `instagram_business_account.id` 填入 `INSTAGRAM_BUSINESS_ACCOUNT_ID`。

也可再確認帳號：

```http
GET https://graph.facebook.com/v21.0/{IG_USER_ID}
  ?fields=id,username
  &access_token=PAGE_ACCESS_TOKEN
```

## 6. Threads

1. App 啟用 **Threads API**，完成 Threads 測試用戶／授權（依開發者後台指示）
2. 用 Threads 相關授權取得 **Threads User Access Token**（可與 Meta 登入流程一併處理；開發期可用 Explorer／授權工具）
3. 查自己的 Threads 使用者 ID：

```http
GET https://graph.threads.net/v1.0/me
  ?fields=id,username
  &access_token=THREADS_ACCESS_TOKEN
```

填入：

- `THREADS_USER_ID`
- `THREADS_ACCESS_TOKEN`

官方文件：[Threads API – Posts](https://developers.facebook.com/docs/threads/posts)

## 7. 填入 `.env`

複製 `.env.example` → `.env`：

```env
META_PAGE_ID=你的粉專ID
META_PAGE_ACCESS_TOKEN=長期PageToken
INSTAGRAM_BUSINESS_ACCOUNT_ID=IG商業帳ID
# IG 發文通常可沿用 Page Token（Facebook Login 路線）
INSTAGRAM_ACCESS_TOKEN=
THREADS_USER_ID=Threads使用者ID
THREADS_ACCESS_TOKEN=ThreadsToken

# 公開媒體根網址（Reels／IG 需要可公開下載的 URL）
PUBLIC_MEDIA_BASE_URL=https://mingxu.mingxu.workers.dev
```

`INSTAGRAM_ACCESS_TOKEN` 若留空，程式會改用 `META_PAGE_ACCESS_TOKEN`。

## 8. 工作台怎麼發

1. 產生當日包（含 `last.jpg`、`short/*.mp4`、social 文案）
2. 勾選 **Facebook／Instagram／Threads**（可連同官網）
3. 按發布  
   - IG／Threads／Reels 需要 **公開 HTTPS 圖／影網址** → 會先確保官網 `assets/daily/` 已更新並 push  
   - 每個平台會嘗試：**靜態圖貼文 + Reels（MP4）**

## 9. 常見錯誤

| 訊息／狀況 | 處理 |
|------------|------|
| `(#10) Application does not have permission` | App 權限未開或未授權上述 scopes |
| IG `instagram_business_account` 為空 | IG 未轉專業帳或未綁粉專 |
| media container `IN_PROGRESS` 很久／失敗 | 影音 URL 無法被 Meta 抓取（未 push、404、非 https） |
| PPA / 兩步驟驗證 | 到粉專完成 Page Publishing Authorization，帳號開 2FA |
| Development 模式別人看不到 | 正常；對外開放需 App Review + Live |

## 10. 快速自測（填好 .env 後）

```bash
cd DailyPaperCut
python -c "from engine.publishers.meta_config import load_meta_config; print(load_meta_config().summary())"
```

再於工作台對某一天只勾 Facebook 試發一則靜態圖。
