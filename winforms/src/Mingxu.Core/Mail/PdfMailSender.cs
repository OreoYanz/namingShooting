using System;
using System.IO;
using System.Net;
using System.Net.Mail;

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

            try
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
                        client.Timeout = 120000;
                        client.Send(msg);
                    }
                }

                return new MailSendResult
                {
                    Ok = true,
                    Message = "已寄送 PDF 至 " + to,
                    To = to,
                };
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new MailSendResult
                {
                    Ok = false,
                    Error = detail,
                    Message = "寄信失敗",
                    To = to,
                };
            }
        }
    }
}
