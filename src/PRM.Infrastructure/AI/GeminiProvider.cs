using System.Net.Http.Json;
using System.Text.Json;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.AI;

/// <summary>
/// Google Gemini API provider.
/// Connects to Gemini model generation endpoint.
/// </summary>
public class GeminiProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _modelName;

    public GeminiProvider(HttpClient http, string apiKey, string baseUrl, string modelName)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
        _modelName = modelName;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new DomainException("AI provider API key is not configured. Please set your Gemini API key in appsettings.json or System Configuration.");

        if (string.IsNullOrWhiteSpace(_baseUrl))
            throw new DomainException("AI provider API URL is not configured. Please set the Gemini endpoint URL in appsettings.json or System Configuration.");

        // Construct Gemini URL
        // https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}
        var url = $"{_baseUrl}{_modelName}:generateContent?key={_apiKey}";

        // Combine system and user prompt.
        var combinedPrompt = string.IsNullOrWhiteSpace(systemPrompt) 
            ? userPrompt 
            : $"{systemPrompt}\n\n{userPrompt}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = combinedPrompt
                        }
                    }
                }
            }
        };

        var response = await _http.PostAsJsonAsync(url, payload, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Gemini API request failed with status code {response.StatusCode} and response: {content}");

        using var doc = JsonDocument.Parse(content);
        try
        {
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? throw new DomainException("The Gemini provider returned an empty response.");
        }
        catch (Exception ex)
        {
            throw new DomainException($"Failed to parse Gemini response: {ex.Message}");
        }
    }
}
