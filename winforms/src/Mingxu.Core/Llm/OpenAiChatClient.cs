using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mingxu.Core.Llm
{

internal static class OpenAiChatClient
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
        catch { }
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(120);
        return client;
    }

    public static string PostJson(string baseUrl, string apiKey, string model, string system, string user, double temperature, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            error = "未設定 ChatGPT API Key（App.config 的 OpenAI:ApiKey）。";
            return null;
        }

        var modelName = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model.Trim();
        var rootUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? "https://api.openai.com/v1"
            : baseUrl.Trim().TrimEnd('/');

        try
        {
            var payload = new JObject
            {
                ["model"] = modelName,
                ["temperature"] = temperature,
                ["response_format"] = new JObject { ["type"] = "json_object" },
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = system },
                    new JObject { ["role"] = "user", ["content"] = user },
                }
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, rootUrl + "/chat/completions"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var response = Http.SendAsync(request).GetAwaiter().GetResult())
                {
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        error = FormatApiError((int)response.StatusCode, body);
                        return null;
                    }
                    var root = JObject.Parse(body);
                    return root["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
                }
            }
        }
        catch (Exception ex)
        {
            error = "ChatGPT 呼叫例外：" + ex.Message;
            return null;
        }
    }

    public static string FormatApiError(int status, string body)
    {
        try
        {
            var root = JObject.Parse(body ?? "{}");
            var err = root["error"] as JObject;
            var code = err != null ? (err["code"]?.ToString() ?? "") : "";
            var type = err != null ? (err["type"]?.ToString() ?? "") : "";
            var msg = err != null ? (err["message"]?.ToString() ?? "") : "";

            if (code == "credit_balance_exhausted" || type == "insufficient_quota" ||
                msg.IndexOf("no credits", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "OpenAI 帳戶額度不足（沒有可用 credits）。\n\n" +
                       "請到帳單頁面儲值後再試：\n" +
                       "https://platform.openai.com/settings/organization/billing/\n\n" +
                       "儲值完成後通常可立刻使用；若仍失敗，確認此 API Key 所屬的 Organization 已有餘額。";
            }
            if (status == 401 || code == "invalid_api_key")
                return "OpenAI API Key 無效或已撤銷，請檢查 App.config 的 OpenAI:ApiKey。";
            if (status == 429)
                return "請求過於頻繁或暫時限流（429）。請稍後再試。\n" + Truncate(msg, 200);
            if (!string.IsNullOrWhiteSpace(msg))
                return "ChatGPT API 失敗 " + status + "：" + Truncate(msg, 300);
        }
        catch
        {
        }
        return "ChatGPT API 失敗 " + status + "：" + Truncate(body, 300);
    }

    public static string Truncate(string s, int n)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= n) return s ?? "";
        return s.Substring(0, n) + "…";
    }
}
}
