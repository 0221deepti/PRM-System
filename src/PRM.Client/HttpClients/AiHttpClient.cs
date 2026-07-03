using System.Net.Http.Json;
using PRM.Application.DTOs.Ai;
using PRM.Client.Session;

namespace PRM.Client.HttpClients;

public class AiHttpClient : ApiClient
{
    public AiHttpClient(HttpClient http, SessionContext session) : base(http, session) { }

    public async Task<SkillMatchResultDto> MatchSkillsAsync(SkillMatchRequestDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/ai/skill-match", dto);
        return await ReadAsync<SkillMatchResultDto>(response);
    }

    public async Task<RiskSummaryDto> GenerateRiskSummaryAsync(RiskSummaryRequestDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/ai/risk-summary", dto);
        return await ReadAsync<RiskSummaryDto>(response);
    }

    public async Task<TeamBuilderResultDto> BuildTeamAsync(TeamBuilderRequestDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/ai/team-builder", dto);
        return await ReadAsync<TeamBuilderResultDto>(response);
    }
}
