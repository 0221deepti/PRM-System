using System.Net.Http.Json;
using PRM.Application.DTOs.Employee;
using PRM.Client.Session;

namespace PRM.Client.HttpClients;

public class EmployeeHttpClient : ApiClient
{
    public EmployeeHttpClient(HttpClient http, SessionContext session) : base(http, session) { }

    public async Task<List<EmployeeSummaryDto>> GetAllAsync()
    {
        var response = await _http.GetAsync("api/employees");
        return await ReadAsync<List<EmployeeSummaryDto>>(response);
    }

    public async Task<List<EmployeeSummaryDto>> GetMyTeamAsync()
    {
        var response = await _http.GetAsync("api/employees?scope=my-team");
        return await ReadAsync<List<EmployeeSummaryDto>>(response);
    }

    public async Task<EmployeeDetailDto> GetDetailAsync(int employeeId)
    {
        var response = await _http.GetAsync($"api/employees/{employeeId}");
        return await ReadAsync<EmployeeDetailDto>(response);
    }

    public async Task<EmployeeDetailDto> GetMyProfileAsync()
    {
        var response = await _http.GetAsync("api/employees/me");
        return await ReadAsync<EmployeeDetailDto>(response);
    }

    public async Task UpdateAsync(int employeeId, UpdateEmployeeDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/employees/{employeeId}", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task AssignManagerAsync(int employeeId, AssignManagerDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/employees/{employeeId}/assign-manager", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task DeactivateAsync(int employeeId)
    {
        var response = await _http.PutAsync($"api/employees/{employeeId}/deactivate", null);
        await EnsureSuccessAsync(response);
    }

    public async Task AddSkillAsync(int employeeId, AddSkillDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/employees/{employeeId}/skills", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task UpdateSkillAsync(int employeeId, int skillId, UpdateSkillDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/employees/{employeeId}/skills/{skillId}", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task RemoveSkillAsync(int employeeId, int skillId)
    {
        var response = await _http.DeleteAsync($"api/employees/{employeeId}/skills/{skillId}");
        await EnsureSuccessAsync(response);
    }
}
