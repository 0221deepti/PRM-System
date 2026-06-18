using System.Net.Http.Json;
using System.Text.Json;

namespace PRM.Infrastructure.AI;

/// <summary>
/// Gemini AI provider using the Google Generative AI REST API.
/// </summary>
public class GeminiProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public GeminiProvider(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return "[AI not configured — set your Gemini API key in System Configuration.]";

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = $"{systemPrompt}\n\n{userPrompt}" } }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 1024
            }
        };

        var response = await _http.PostAsJsonAsync(url, payload, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return $"[AI request failed: {response.StatusCode}]";

        using var doc = JsonDocument.Parse(content);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "[No response from AI]";
    }
}
