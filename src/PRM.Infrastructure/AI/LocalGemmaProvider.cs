using System.Net.Http.Json;
using System.Text.Json;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.AI;

/// <summary>
/// Local Gemma AI provider using Ollama-style API format.
/// Connects to a self-hosted Gemma model endpoint.
/// </summary>
public class LocalGemmaProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _modelName;

    public LocalGemmaProvider(HttpClient http, string apiKey, string baseUrl = "http://164.52.211.238/api/generate", string modelName = "gemma3:12b-it-q8_0")
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl;
        _modelName = modelName;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new DomainException("AI provider API key is not configured. Please set your Local Gemma API key in System Configuration.");

        if (string.IsNullOrWhiteSpace(_baseUrl))
            throw new DomainException("AI provider API URL is not configured. Please set the endpoint URL in System Configuration.");

        // Combine system and user prompts into a single prompt for Ollama format
        var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

        var payload = new
        {
            model = _modelName,
            prompt = combinedPrompt,
            stream = false,
            options = new
            {
                temperature = 0.3,
                num_predict = 1024
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl);
        
        // Add custom API key header
        request.Headers.Add("apikey", _apiKey);
        request.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"AI request failed with status code {response.StatusCode} and response: {content}");

        using var doc = JsonDocument.Parse(content);
        
        // Ollama format returns response in "response" field
        var text = doc.RootElement.GetProperty("response").GetString();

        return text ?? throw new DomainException("The AI provider returned an empty response.");
    }
}
