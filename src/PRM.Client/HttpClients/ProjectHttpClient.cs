using System.Net.Http.Json;
using PRM.Application.DTOs.Project;
using PRM.Client.Session;

namespace PRM.Client.HttpClients;

public class ProjectHttpClient : ApiClient
{
    public ProjectHttpClient(HttpClient http, SessionContext session) : base(http, session) { }

    public async Task<List<ProjectSummaryDto>> GetAllAsync()
    {
        var response = await _http.GetAsync("api/projects");
        return await ReadAsync<List<ProjectSummaryDto>>(response);
    }

    public async Task<List<ProjectSummaryDto>> GetMineAsync()
    {
        var response = await _http.GetAsync("api/projects?scope=mine");
        return await ReadAsync<List<ProjectSummaryDto>>(response);
    }

    public async Task<ProjectDetailDto> GetDetailAsync(int projectId)
    {
        var response = await _http.GetAsync($"api/projects/{projectId}");
        return await ReadAsync<ProjectDetailDto>(response);
    }

    public async Task CreateAsync(CreateProjectDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/projects", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task UpdateAsync(int projectId, UpdateProjectDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/projects/{projectId}", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task AddMilestoneAsync(int projectId, AddMilestoneDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/projects/{projectId}/milestones", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task UpdateMilestoneStatusAsync(int projectId, int milestoneId, UpdateMilestoneStatusDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/projects/{projectId}/milestones/{milestoneId}", dto);
        await EnsureSuccessAsync(response);
    }
}
