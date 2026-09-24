# 名序（NamingMethod）

本專案保留兩條主線：

1. **形象官網**（GitHub Pages）：編輯 `site/`，公開內容在 `docs/`
2. **WinForms 桌面程式**（.NET Framework 4.8）：`winforms/MingxuDesktop.sln`

線上網站：https://oreoyanz.github.io/namingShooting/

---

## 形象網站

| 路徑 | 用途 |
|------|------|
| `site/` | 編輯用來源 |
| `docs/` | GitHub Pages 發布內容（與 `site/` 同步） |
| `scripts/apply_seo.py` | 套用 SEO meta／JSON-LD，並同步更新 `site/` 與 `docs/` |

修改文案後建議：

```bat
python scripts\apply_seo.py
```

再將 `docs/` 推上 GitHub `main`。

廣告追蹤 ID 填在：`site/js/tracking-config.js`（並同步至 `docs/js/`）。

---

## WinForms 桌面版

| 路徑 | 用途 |
|------|------|
| `winforms/MingxuDesktop.sln` | VS2015 / MSBuild 方案 |
| `winforms/_gen/` | 執行期字庫與熱門名資料（勿刪） |
| `winforms/packages/` | NuGet 套件 |
| `data/` | 規則／字庫來源（重新產生 `_gen` 時使用） |
| `scripts/generate_csharp_data.py` | 從 `data/`（與本機 SQLite，若有）產生 `winforms/_gen/` |

### 編譯與執行

1. 安裝 [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48)
2. 於 `winforms` 還原 NuGet：`nuget.exe restore MingxuDesktop.sln -PackagesDirectory packages`
3. 開啟 `winforms\MingxuDesktop.sln`，將 `MingxuDesktop` 設為啟動專案 → F5  
   或：

```bat
"%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe" winforms\MingxuDesktop.sln /p:Configuration=Debug
```

輸出：`winforms\MingxuDesktop\bin\Debug\MingxuDesktop.exe`

API Key 等設定見：`winforms\MingxuDesktop\App.config`（勿提交真實金鑰至公開遠端）。

### 重新產生字庫（進階）

```bat
py -3 scripts\generate_csharp_data.py
```

會寫入 `winforms\_gen\`。日常開發與執行只需既有 `_gen`，不必依賴 Python 命名引擎。

---

## 專案結構（精簡後）

```
NamingMethod/
  docs/                 # GitHub Pages
  site/                 # 官網來源
  scripts/              # apply_seo / generate_csharp_data
  data/                 # 字庫與規則來源資料
  winforms/             # 桌面程式方案
  README.md
```
