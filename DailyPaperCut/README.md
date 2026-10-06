# 名序｜每日吉祥剪紙引擎（DailyPaperCut）

第一階段：產生 → 預覽 → 人工確認 → 發布（**不自動發布**）。

## 快速開始

```bash
cd DailyPaperCut
python -m venv .venv
.\.venv\Scripts\activate
pip install -r requirements.txt
copy .env.example .env
# 編輯 .env，填入 OPENAI_API_KEY（文案與素材工作室圖像生成用）

# 第一次：先把素材庫補成可取用的 PNG（或到素材工作室生成）
# 開啟網頁工作台後按「為現有庫目補 placeholder 檔」
# 或 API：POST /api/pool/seed-placeholders

# CLI 產生當日包
python run_generate.py 20261002

# 網頁介面（推薦；底層仍是 Python）
uvicorn ui.web_app:app --reload --port 8765
# 瀏覽 http://127.0.0.1:8765
```

## 兩個工作面

| 頁面 | 用途 |
|------|------|
| **每日產生** | 取公共吉凶＋宜忌 → 從素材池取圖 → 場景／GIF／MP4／文案 → **自動寄信＋上官網＋YouTube Shorts** |
| **素材工作室** | 依 `materials/assets/` **資料夾**類別生成圖像；資料夾增減後選單自動更新。每日產生只取用池內檔案 |

## 產出位置

`DailyPaperCut/YYYYMMDD/` 下含：

- `data/` daily.json、fortune.json、generation.json
- `source/`、`cutout/`、`scene/`
- `gif/`、`short/`（含 **MP4**）、`social/`、`publish/`
- `manifest.json`

## 模組

| 模組 | 職責 |
|------|------|
| `engine/fortune.py` | 公共日曆吉凶＋農曆通書宜忌（lunar_python） |
| `engine/material_pool.py` | 素材池生成／就緒清單 |
| `engine/materials.py` | 取用規則（7／2／14 天） |
| `engine/cutout_from_pool.py` | 每日只從池複製，不 call 圖像 API |
| `engine/ai_copy.py` | 社群文案 |
| `engine/scene.py` | 紙雕場景合成 |
| `engine/gif_builder.py` | GIF / WebP |
| `engine/short_builder.py` | Shorts MP4（靜態字結尾＋CTA） |
| `engine/pipeline.py` | Generate Daily |
| `ui/web_app.py` | FastAPI 工作台 |
| `engine/publishers/` | 官網、Facebook／IG／Threads 發布 |

## Meta 社群發文

工作台可勾選 **Facebook／Instagram／Threads**，各發 **靜態圖 + Reels（MP4）**。  
Token 取得步驟與 `.env` 欄位見 [`docs/META_SETUP.md`](docs/META_SETUP.md)。

## YouTube Shorts

產生完成後可自動上傳 Shorts。設定見 [`docs/YOUTUBE_SETUP.md`](docs/YOUTUBE_SETUP.md)（需先 `python youtube_auth.py`）。

## 設定重點

- `config/settings.json`
  - `cta.url` → `https://mingxu.mingxu.workers.dev/`
  - `openai.enableImageGeneration` → `true`（僅素材工作室會 call）
- `config/StyleBible.json`：視覺風格
- `materials/library.json`：素材 ID 庫
- `materials/usage_history.json`：使用歷史

## 注意

- 消費者可見內容禁止出現「AI」字樣。
- Shorts 結尾為靜態字「名序／知名・知運・知人生」，並導流官網。
- 宜忌為公共通書資料，非個人命盤。
- 產生完成後若已設定 `GMAIL_USER` + `GMAIL_APP_PASSWORD`，會把社群文案與 MP4 寄到指定信箱（見 `.env.example`）。
