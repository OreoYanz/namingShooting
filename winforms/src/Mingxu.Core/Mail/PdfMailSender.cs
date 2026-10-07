using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace Mingxu.Core.Mail
{
    public sealed class MailSendResult
    {
        public bool Ok { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public string To { get; set; }
    }

    /// <summary>
    /// 匯出 PDF 後以 SMTP（Gmail 應用程式密碼）寄出附件。
    /// 使用 STARTTLS（.NET SmtpClient）；若預設埠被擋會自動改試其他埠。
    /// </summary>
    public static class PdfMailSender
    {
        public static MailSendResult SendPdf(
            string pdfPath,
            string fullName,
            string serviceName,
            string smtpHost,
            int smtpPort,
            string user,
            string appPassword,
            string to,
            string from = null,
            bool enableSsl = true)
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
            catch { /* ignore */ }

            user = (user ?? "").Trim();
            appPassword = (appPassword ?? "").Replace(" ", "").Replace("\u3000", "").Trim();
            to = string.IsNullOrWhiteSpace(to) ? user : to.Trim();
            from = string.IsNullOrWhiteSpace(from) ? user : from.Trim();
            smtpHost = string.IsNullOrWhiteSpace(smtpHost) ? "smtp.gmail.com" : smtpHost.Trim();
            if (smtpPort <= 0) smtpPort = 587;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(appPassword))
            {
                return new MailSendResult
                {
                    Ok = false,
                    Skipped = true,
                    Message = "未設定 Mail:User／Mail:AppPassword，略過寄信",
                };
            }

            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            {
                return new MailSendResult
                {
                    Ok = false,
                    Error = "PDF 檔案不存在：" + pdfPath,
                    Message = "寄信失敗",
                    To = to,
                };
            }

            var name = string.IsNullOrWhiteSpace(fullName) ? "命名報告" : fullName.Trim();
            var service = string.IsNullOrWhiteSpace(serviceName) ? "命名剖象" : serviceName.Trim();
            var subject = "名序｜" + service + "｜" + name;
            var body =
                "名序 PDF 報告已匯出。\n\n"
                + "服務：" + service + "\n"
                + "姓名：" + name + "\n"
                + "檔名：" + Path.GetFileName(pdfPath) + "\n"
                + "時間：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n\n"
                + "PDF 見附件。\n";

            // SmtpClient 只支援 STARTTLS（587／25），不支援 465 隱式 SSL。
            // 部分網路會擋 587，改試 25。
            var ports = BuildPortCandidates(smtpPort);
            Exception lastError = null;

            foreach (var port in ports)
            {
                try
                {
                    SendOnce(pdfPath, subject, body, smtpHost, port, user, appPassword, to, from, enableSsl);
                    return new MailSendResult
                    {
                        Ok = true,
                        Message = "已寄送 PDF 至 " + to + "（SMTP " + smtpHost + ":" + port + "）",
                        To = to,
                    };
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (!IsConnectionFailure(ex))
                        break; // 帳密／認證錯誤不必換埠
                }
            }

            var detail = FormatError(lastError);
            return new MailSendResult
            {
                Ok = false,
                Error = detail,
                Message = "寄信失敗",
                To = to,
            };
        }

        private static IEnumerable<int> BuildPortCandidates(int preferred)
        {
            // 465 = 隱式 SSL，SmtpClient 不支援，略過
            var list = new List<int>();
            if (preferred > 0 && preferred != 465) list.Add(preferred);
            foreach (var p in new[] { 25, 587 })
            {
                if (!list.Contains(p)) list.Add(p);
            }
            return list;
        }

        private static void SendOnce(
            string pdfPath,
            string subject,
            string body,
            string smtpHost,
            int smtpPort,
            string user,
            string appPassword,
            string to,
            string from,
            bool enableSsl)
        {
            using (var msg = new MailMessage())
            {
                msg.From = new MailAddress(from);
                msg.To.Add(to);
                msg.Subject = subject;
                msg.Body = body;
                msg.IsBodyHtml = false;
                msg.Attachments.Add(new Attachment(pdfPath));

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(user, appPassword);
                    client.Timeout = 45000;
                    client.Send(msg);
                }
            }
        }

        private static bool IsConnectionFailure(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is SocketException) return true;
                if (e is TimeoutException) return true;
                var m = e.Message ?? "";
                if (m.IndexOf("無法連接", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (m.IndexOf("unable to connect", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (m.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (m.IndexOf("連線嘗試失敗", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static string FormatError(Exception ex)
        {
            if (ex == null) return "未知錯誤";
            var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            if (IsConnectionFailure(ex))
            {
                return detail
                    + "\n\n可能原因：網路／防火牆阻擋 SMTP（常見為 587）。"
                    + "\n已自動改試 25／587。"
                    + "\n若仍失敗，可在 App.config 設 Mail:Enabled=false 先略過寄信，或檢查 Gmail 應用程式密碼。";
            }
            return detail;
        }
    }
}
