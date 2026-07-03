using Microsoft.Extensions.Configuration;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.AI;

/// <summary>
/// Factory that creates the appropriate LLM provider based on the configured provider name.
/// Supports automatic reachability checks and fallback between Gemma and Gemini.
/// </summary>
public interface IAiProviderFactory
{
    ILlmProvider Create(SystemConfig config);
}

public class AiProviderFactory : IAiProviderFactory
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;
    
    // Cache the reachability check to prevent repeated latency on subsequent requests
    private static bool? _isGemmaAvailable;
    private static DateTime _lastCheckTime = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
    private static readonly object _lock = new();

    public AiProviderFactory(IHttpClientFactory httpFactory, IConfiguration configuration)
    {
        _httpFactory = httpFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Resets the cached Gemma availability status (primarily used for unit testing).
    /// </summary>
    public static void ResetCache()
    {
        lock (_lock)
        {
            _isGemmaAvailable = null;
            _lastCheckTime = DateTime.MinValue;
        }
    }

    public ILlmProvider Create(SystemConfig config)
    {
        // 1. Resolve Gemma configuration (DB has priority if configured provider is Gemma, fallback to appsettings)
        string gemmaUrl, gemmaModel, gemmaKey;
        if (config.LlmProvider != null && config.LlmProvider.Equals("LocalGemma", StringComparison.OrdinalIgnoreCase))
        {
            gemmaUrl = !string.IsNullOrWhiteSpace(config.LlmApiUrl) 
                ? config.LlmApiUrl 
                : (_configuration["AiSettings:GemmaBaseUrl"] ?? "http://164.52.211.238/api/generate");
            gemmaModel = !string.IsNullOrWhiteSpace(config.LlmModelName) 
                ? config.LlmModelName 
                : (_configuration["AiSettings:GemmaModel"] ?? "gemma3:12b-it-q8_0");
            gemmaKey = !string.IsNullOrWhiteSpace(config.LlmApiKey) 
                ? config.LlmApiKey 
                : (_configuration["AiSettings:GemmaApiKey"] ?? "8e6aNK83g9YFBk1fNSW60eukXKSvOoZJ");
        }
        else
        {
            gemmaUrl = _configuration["AiSettings:GemmaBaseUrl"] ?? "http://164.52.211.238/api/generate";
            gemmaModel = _configuration["AiSettings:GemmaModel"] ?? "gemma3:12b-it-q8_0";
            gemmaKey = _configuration["AiSettings:GemmaApiKey"] ?? "8e6aNK83g9YFBk1fNSW60eukXKSvOoZJ";
        }

        // 2. Resolve Gemini configuration (DB has priority if configured provider is Gemini, fallback to appsettings)
        string geminiUrl, geminiModel, geminiKey;
        if (config.LlmProvider != null && config.LlmProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            geminiUrl = !string.IsNullOrWhiteSpace(config.LlmApiUrl) 
                ? config.LlmApiUrl 
                : (_configuration["AiSettings:GeminiBaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/");
            geminiModel = !string.IsNullOrWhiteSpace(config.LlmModelName) 
                ? config.LlmModelName 
                : (_configuration["AiSettings:GeminiModel"] ?? "gemini-1.5-flash");
            geminiKey = !string.IsNullOrWhiteSpace(config.LlmApiKey) 
                ? config.LlmApiKey 
                : (_configuration["AiSettings:GeminiApiKey"] ?? "");
        }
        else
        {
            geminiUrl = _configuration["AiSettings:GeminiBaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/";
            geminiModel = _configuration["AiSettings:GeminiModel"] ?? "gemini-1.5-flash";
            geminiKey = _configuration["AiSettings:GeminiApiKey"] ?? "";
        }

        // 3. Select provider based on configuration and reachability
        if (config.LlmProvider != null && config.LlmProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            // If Gemini is explicitly configured as the provider, use Gemini directly (e.g. for testing both-fail / direct access)
            return new GeminiProvider(
                _httpFactory.CreateClient(),
                geminiKey,
                geminiUrl,
                geminiModel);
        }

        // Default or LocalGemma configured -> check Gemma availability first
        if (IsGemmaReachable(gemmaUrl))
        {
            return new LocalGemmaProvider(
                _httpFactory.CreateClient(),
                gemmaKey,
                gemmaUrl,
                gemmaModel);
        }

        // Gemma is unavailable, automatically fall back to Gemini
        return new GeminiProvider(
            _httpFactory.CreateClient(),
            geminiKey,
            geminiUrl,
            geminiModel);
    }

    private bool IsGemmaReachable(string url)
    {
        var now = DateTime.UtcNow;
        if (_isGemmaAvailable.HasValue && (now - _lastCheckTime) < CacheTtl)
            return _isGemmaAvailable.Value;

        lock (_lock)
        {
            now = DateTime.UtcNow;
            if (_isGemmaAvailable.HasValue && (now - _lastCheckTime) < CacheTtl)
                return _isGemmaAvailable.Value;

            try
            {
                using var client = _httpFactory.CreateClient();
                // Perform quick connection ping with a short timeout to prevent application hanging
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
                
                var response = client.GetAsync(url, cts.Token).GetAwaiter().GetResult();
                _isGemmaAvailable = true;
            }
            catch
            {
                _isGemmaAvailable = false;
            }

            _lastCheckTime = DateTime.UtcNow;
            return _isGemmaAvailable.Value;
        }
    }
}
