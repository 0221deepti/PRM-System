using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PRM.Infrastructure.AI;

/// <summary>
/// Groq AI provider using the Groq REST API (OpenAI-compatible).
/// </summary>
public class GroqProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public GroqProvider(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return "[AI not configured — set your Groq API key in System Configuration.]";

        var url = "https://api.groq.com/openai/v1/chat/completions";

        var payload = new
        {
            model = "llama3-8b-8192",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            max_tokens = 1024
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return $"[AI request failed: {response.StatusCode}]";

        using var doc = JsonDocument.Parse(content);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return text ?? "[No response from AI]";
    }
}
