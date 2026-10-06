# 名序 WinForms（Visual Studio 2015 / .NET Framework 4.8）

舊式 csproj（含 `xmlns`），可用 **Visual Studio 2015** 開啟。執行時不需要 Python、也不需要 .NET 8。

## 環境需求

1. Visual Studio 2015（含「.NET 桌面開發」相關工作負載）
2. [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48)
3. NuGet 套件（本目錄已含 `packages/`，或執行 `nuget.exe restore`）

## 開啟與編譯

1. 開啟 `MingxuDesktop.sln`
2. 設 `MingxuDesktop` 為啟動專案 → F5

命令列：

```bat
"%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe" MingxuDesktop.sln /p:Configuration=Debug
```

輸出：`MingxuDesktop\bin\Debug\MingxuDesktop.exe`

## 字庫資料

執行期資料在 `_gen\`（由專案複製到輸出目錄）。若需重新產生，於專案根目錄執行：

```bat
py -3 ..\scripts\generate_csharp_data.py
```

（需根目錄 `data\`；完整字庫亦可搭配本機 `%APPDATA%\TaiwanNamingHelper\naming.sqlite3`。）

## 專案結構

| 專案 | 說明 |
|------|------|
| `Mingxu.Core` | 八字、五格、評分、候選管線、流年、LLM |
| `Mingxu.Data` | 字庫 CSV／熱門名 |
| `Mingxu.Export` | 命名剖象 PDF |
| `MingxuDesktop` | WinForms 介面 |

目標框架：**.NET Framework 4.8**，語言：**C# 6**（VS2015 相容）。
