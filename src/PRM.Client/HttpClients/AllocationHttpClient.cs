using System.Net.Http.Json;
using PRM.Application.DTOs.Allocation;
using PRM.Application.DTOs.Project;
using PRM.Client.Session;

namespace PRM.Client.HttpClients;

public class AllocationHttpClient : ApiClient
{
    public AllocationHttpClient(HttpClient http, SessionContext session) : base(http, session) { }

    public async Task<List<AllocationSummaryDto>> GetAllAsync()
    {
        var response = await _http.GetAsync("api/allocations");
        return await ReadAsync<List<AllocationSummaryDto>>(response);
    }

    public async Task<List<AllocationSummaryDto>> GetMineAsync()
    {
        var response = await _http.GetAsync("api/allocations/mine");
        return await ReadAsync<List<AllocationSummaryDto>>(response);
    }

    public async Task<List<AllocationSummaryDto>> GetByProjectAsync(int projectId)
    {
        var response = await _http.GetAsync($"api/allocations/project/{projectId}");
        return await ReadAsync<List<AllocationSummaryDto>>(response);
    }

    public async Task CreateAsync(CreateAllocationDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/allocations", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task EndAllocationAsync(int allocationId)
    {
        var response = await _http.PutAsync($"api/allocations/{allocationId}/end", null);
        await EnsureSuccessAsync(response);
    }
}
