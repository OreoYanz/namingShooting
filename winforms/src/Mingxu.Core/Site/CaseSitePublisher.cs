using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mingxu.Core.Site
{
    public sealed class CasePublishResult
    {
        public bool Ok { get; set; }
        public bool Merged { get; set; }
        public bool Pushed { get; set; }
        public bool SkippedPush { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public string SitePath { get; set; }
        public string DocsPath { get; set; }
        public string CommitSha { get; set; }
        public string CaseId { get; set; }
    }

    /// <summary>
    /// 將 PDF 匯出產生的案例字卡併入 site/data/cases.json（並同步 docs/），可選 git commit／push。
    /// </summary>
    public static class CaseSitePublisher
    {
        public static CasePublishResult Publish(JObject card, bool commitAndPush = true, string repoRootOverride = null)
        {
            var result = new CasePublishResult();
            if (card == null)
            {
                result.Error = "案例資料為空";
                return result;
            }

            var id = (string)card["id"] ?? "";
            result.CaseId = id;

            string repoRoot;
            try
            {
                repoRoot = string.IsNullOrWhiteSpace(repoRootOverride)
                    ? FindRepoRoot()
                    : repoRootOverride.Trim();
            }
            catch (Exception ex)
            {
                result.Error = "找不到專案根目錄：" + ex.Message;
                return result;
            }

            var sitePath = Path.Combine(repoRoot, "site", "data", "cases.json");
            var docsPath = Path.Combine(repoRoot, "docs", "data", "cases.json");
            result.SitePath = sitePath;
            result.DocsPath = docsPath;

            try
            {
                MergeInto(sitePath, card);
                // GitHub Pages 讀 docs/：保持與 site 同步
                Directory.CreateDirectory(Path.GetDirectoryName(docsPath) ?? repoRoot);
                File.Copy(sitePath, docsPath, true);
                result.Merged = true;
            }
            catch (Exception ex)
            {
                result.Error = "寫入 cases.json 失敗：" + ex.Message;
                return result;
            }

            if (!commitAndPush)
            {
                result.Ok = true;
                result.SkippedPush = true;
                result.Message = "已更新 site／docs 的 cases.json（未 commit／push）";
                return result;
            }

            try
            {
                return CommitAndPush(repoRoot, sitePath, docsPath, card, result);
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.Error = "git 操作失敗：" + ex.Message;
                result.Message = "案例已寫入網站檔案，但 commit／push 失敗";
                return result;
            }
        }

        public static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (var i = 0; i < 12 && dir != null; i++)
            {
                var siteCases = Path.Combine(dir.FullName, "site", "data", "cases.json");
                var git = Path.Combine(dir.FullName, ".git");
                if (File.Exists(siteCases) && (Directory.Exists(git) || File.Exists(git)))
                    return dir.FullName;
                dir = dir.Parent;
            }

            dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (var i = 0; i < 12 && dir != null; i++)
            {
                var siteCases = Path.Combine(dir.FullName, "site", "data", "cases.json");
                if (File.Exists(siteCases))
                    return dir.FullName;
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("請確認執行環境位於 NamingMethod 倉庫內（含 site/data/cases.json）");
        }

        private static void MergeInto(string casesPath, JObject card)
        {
            JObject root;
            if (File.Exists(casesPath))
            {
                var text = File.ReadAllText(casesPath, Encoding.UTF8);
                root = JObject.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            }
            else
            {
                root = new JObject
                {
                    ["version"] = 1,
                    ["note"] = "由 WinForms 匯出 PDF 時產生的 .case.json 併入此檔 cases 陣列。",
                };
            }

            var cases = root["cases"] as JArray;
            if (cases == null)
            {
                cases = new JArray();
                root["cases"] = cases;
            }

            var id = (string)card["id"] ?? "";
            var service = (string)card["service"] ?? "";
            var birth = (string)card["birthDate"] ?? "";
            var display = (string)card["displayName"] ?? "";
            var region = (string)card["region"] ?? "";

            // 同 id，或同服務＋生日＋顯示名＋地區 → 取代（避免重匯出重複）
            for (var i = cases.Count - 1; i >= 0; i--)
            {
                var existing = cases[i] as JObject;
                if (existing == null) continue;
                var eid = (string)existing["id"] ?? "";
                if (!string.IsNullOrEmpty(id) && eid == id)
                {
                    cases.RemoveAt(i);
                    continue;
                }
                if (string.Equals((string)existing["service"] ?? "", service, StringComparison.OrdinalIgnoreCase)
                    && (string)existing["birthDate"] == birth
                    && (string)existing["displayName"] == display
                    && (string)existing["region"] == region)
                {
                    cases.RemoveAt(i);
                }
            }

            cases.Insert(0, card.DeepClone());

            if (root["version"] == null) root["version"] = 1;
            if (root["note"] == null)
                root["note"] = "由 WinForms 匯出 PDF 時產生的 .case.json 併入此檔 cases 陣列。";

            var json = root.ToString(Formatting.Indented) + Environment.NewLine;
            Directory.CreateDirectory(Path.GetDirectoryName(casesPath) ?? ".");
            File.WriteAllText(casesPath, json, new UTF8Encoding(false));
        }

        private static CasePublishResult CommitAndPush(
            string repoRoot,
            string sitePath,
            string docsPath,
            JObject card,
            CasePublishResult result)
        {
            var relSite = ToRepoRelative(repoRoot, sitePath);
            var relDocs = ToRepoRelative(repoRoot, docsPath);

            var add = RunGit(repoRoot, "add -- \"" + relSite + "\" \"" + relDocs + "\"");
            if (add.ExitCode != 0)
            {
                result.Error = "git add 失敗：" + TrimOut(add);
                result.Message = "案例已寫入，但無法 stage";
                return result;
            }

            var status = RunGit(repoRoot, "status --porcelain -- \"" + relSite + "\" \"" + relDocs + "\"");
            if (string.IsNullOrWhiteSpace(status.StdOut))
            {
                result.Ok = true;
                result.SkippedPush = true;
                result.Message = "cases.json 無變更，略過 commit／push";
                return result;
            }

            var display = (string)card["displayName"] ?? "OO";
            var label = (string)card["serviceLabel"] ?? ((string)card["service"] ?? "案例");
            var msg = "Add real case card: " + label + " " + display + ".";
            var msgFile = Path.Combine(Path.GetTempPath(), "mingxu-case-commit-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                File.WriteAllText(msgFile, msg + "\n", new UTF8Encoding(false));
                var commit = RunGit(repoRoot, "commit -F \"" + msgFile + "\"");
                if (commit.ExitCode != 0)
                {
                    var outText = TrimOut(commit);
                    if (outText.IndexOf("nothing to commit", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Ok = true;
                        result.SkippedPush = true;
                        result.Message = "cases.json 無變更，略過 commit／push";
                        return result;
                    }
                    result.Error = "git commit 失敗：" + outText;
                    result.Message = "案例已寫入，但 commit 失敗";
                    return result;
                }
            }
            finally
            {
                try { if (File.Exists(msgFile)) File.Delete(msgFile); } catch { /* ignore */ }
            }

            var push = RunGit(repoRoot, "push origin HEAD", 300000);
            if (push.ExitCode != 0)
            {
                result.Error = "git push 失敗：" + TrimOut(push);
                result.Message = "已 commit，但 push 失敗（請稍後手動 git push）";
                result.Ok = false;
                return result;
            }

            var rev = RunGit(repoRoot, "rev-parse --short HEAD");
            result.CommitSha = (rev.StdOut ?? "").Trim();
            result.Pushed = true;
            result.Ok = true;
            result.Message = "已更新官網真實案例並 push"
                + (string.IsNullOrEmpty(result.CommitSha) ? "" : "（" + result.CommitSha + "）");
            return result;
        }

        private static string ToRepoRelative(string repoRoot, string fullPath)
        {
            var root = Path.GetFullPath(repoRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(fullPath);
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return full.Substring(root.Length).Replace('\\', '/');
            return full.Replace('\\', '/');
        }

        private sealed class GitRun
        {
            public int ExitCode;
            public string StdOut;
            public string StdErr;
        }

        private static GitRun RunGit(string repoRoot, string arguments, int timeoutMs = 120000)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            // 避免 git 依 console code page 亂碼
            psi.EnvironmentVariables["LANG"] = "C.UTF-8";
            psi.EnvironmentVariables["LC_ALL"] = "C.UTF-8";

            using (var p = Process.Start(psi))
            {
                if (p == null)
                    throw new InvalidOperationException("無法啟動 git");

                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { /* ignore */ }
                    throw new TimeoutException("git 逾時：" + arguments);
                }
                return new GitRun
                {
                    ExitCode = p.ExitCode,
                    StdOut = stdout,
                    StdErr = stderr,
                };
            }
        }

        private static string TrimOut(GitRun run)
        {
            var s = ((run.StdErr ?? "") + "\n" + (run.StdOut ?? "")).Trim();
            if (s.Length > 800) s = s.Substring(0, 800) + "…";
            return s;
        }
    }
}
