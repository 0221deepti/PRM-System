namespace PRM.Infrastructure.AI;

public interface ILlmProvider
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct);
}
