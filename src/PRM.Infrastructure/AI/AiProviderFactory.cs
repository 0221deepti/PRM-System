using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.AI;

/// <summary>
/// Factory that creates the appropriate LLM provider based on the configured provider name.
/// Implements the Factory Pattern — all provider creation logic is centralized here.
/// Adding a new provider requires only adding a new case here (Open/Closed Principle).
/// </summary>
public interface IAiProviderFactory
{
    ILlmProvider Create(string providerName, string apiKey);
}

public class AiProviderFactory : IAiProviderFactory
{
    private readonly IHttpClientFactory _httpFactory;

    public AiProviderFactory(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    public ILlmProvider Create(string providerName, string apiKey) => providerName switch
    {
        "Gemini" => new GeminiProvider(_httpFactory.CreateClient(), apiKey),
        "Groq" => new GroqProvider(_httpFactory.CreateClient(), apiKey),
        _ => throw new DomainException($"Unknown LLM provider: {providerName}")
    };
}
