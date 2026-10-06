using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MingxuDesktop
{
    /// <summary>
    /// 以 WebView2 PrintToPdf 將 HTML 轉成 A4 PDF（需 UI 執行緒／訊息迴圈）。
    /// </summary>
    public static class WebView2PdfRenderer
    {
        public static async Task PrintHtmlToPdfAsync(
            string html,
            string pdfPath,
            string browserExecutableFolder = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new ArgumentException("HTML 內容為空", "html");
            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException("PDF 路徑為空", "pdfPath");

            var fullPdf = Path.GetFullPath(pdfPath);
            var pdfDir = Path.GetDirectoryName(fullPdf);
            if (!string.IsNullOrEmpty(pdfDir))
                Directory.CreateDirectory(pdfDir);

            var workDir = Path.Combine(Path.GetTempPath(), "MingxuFlowPdf_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDir);
            var htmlPath = Path.Combine(workDir, "report.html");
            File.WriteAllText(htmlPath, html, System.Text.Encoding.UTF8);

            var userData = Path.Combine(Path.GetTempPath(), "MingxuWebView2");
            Directory.CreateDirectory(userData);

            Exception error = null;
            using (var form = new Form())
            {
                form.ShowInTaskbar = false;
                form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-20000, -20000);
                form.Size = new Size(900, 1200);
                form.Opacity = 0;
                form.Show();

                var webView = new WebView2();
                webView.Dock = DockStyle.Fill;
                form.Controls.Add(webView);

                try
                {
                    CoreWebView2Environment env;
                    if (!string.IsNullOrWhiteSpace(browserExecutableFolder) && Directory.Exists(browserExecutableFolder))
                        env = await CoreWebView2Environment.CreateAsync(browserExecutableFolder, userData).ConfigureAwait(true);
                    else
                        env = await CoreWebView2Environment.CreateAsync(null, userData).ConfigureAwait(true);

                    await webView.EnsureCoreWebView2Async(env).ConfigureAwait(true);

                    var navTcs = new TaskCompletionSource<bool>();
                    webView.CoreWebView2.NavigationCompleted += (s, e) =>
                    {
                        if (e.IsSuccess) navTcs.TrySetResult(true);
                        else navTcs.TrySetException(new InvalidOperationException(
                            "WebView2 載入 HTML 失敗：" + (e.WebErrorStatus.ToString())));
                    };

                    webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);

                    using (cancellationToken.Register(() => navTcs.TrySetCanceled()))
                    {
                        var finished = await Task.WhenAny(navTcs.Task, Task.Delay(60000, cancellationToken)).ConfigureAwait(true);
                        if (finished != navTcs.Task)
                            throw new TimeoutException("WebView2 載入報告逾時");
                        await navTcs.Task.ConfigureAwait(true);
                    }

                    // 稍候版面／字型
                    await Task.Delay(400).ConfigureAwait(true);

                    var settings = webView.CoreWebView2.Environment.CreatePrintSettings();
                    settings.Orientation = CoreWebView2PrintOrientation.Portrait;
                    settings.ShouldPrintBackgrounds = true;
                    settings.MediaSize = CoreWebView2PrintMediaSize.Custom;
                    // A4 inches
                    settings.PageWidth = 8.27;
                    settings.PageHeight = 11.69;
                    settings.MarginTop = 0.4;
                    settings.MarginBottom = 0.45;
                    settings.MarginLeft = 0.45;
                    settings.MarginRight = 0.45;
                    settings.ScaleFactor = 1.0;

                    if (File.Exists(fullPdf))
                        File.Delete(fullPdf);

                    var ok = await webView.CoreWebView2.PrintToPdfAsync(fullPdf, settings).ConfigureAwait(true);
                    if (!ok)
                        throw new InvalidOperationException("WebView2 PrintToPdf 回傳失敗");
                    if (!File.Exists(fullPdf) || new FileInfo(fullPdf).Length < 64)
                        throw new InvalidOperationException("PDF 檔案未正確產生");
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    try { form.Close(); } catch { /* ignore */ }
                }
            }

            try
            {
                if (Directory.Exists(workDir))
                    Directory.Delete(workDir, true);
            }
            catch { /* ignore temp cleanup */ }

            if (error != null)
                throw error;
        }
    }
}
