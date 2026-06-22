using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.AI;

/// <summary>
/// Factory that creates the appropriate LLM provider based on the configured provider name.
/// Currently supports LocalGemma for self-hosted Gemma models via Ollama-style API.
/// </summary>
public interface IAiProviderFactory
{
    ILlmProvider Create(SystemConfig config);
}

public class AiProviderFactory : IAiProviderFactory
{
    private readonly IHttpClientFactory _httpFactory;

    public AiProviderFactory(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    public ILlmProvider Create(SystemConfig config) => config.LlmProvider switch
    {
        "LocalGemma" => new LocalGemmaProvider(
            _httpFactory.CreateClient(), 
            config.LlmApiKey, 
            config.LlmApiUrl, 
            config.LlmModelName),
        _ => throw new DomainException($"Unknown LLM provider: {config.LlmProvider}")
    };
}
