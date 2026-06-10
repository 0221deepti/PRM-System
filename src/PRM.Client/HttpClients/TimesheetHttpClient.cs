using System.Net.Http.Json;
using PRM.Application.DTOs.Timesheet;
using PRM.Client.Session;

namespace PRM.Client.HttpClients;

public class TimesheetHttpClient : ApiClient
{
    public TimesheetHttpClient(HttpClient http, SessionContext session) : base(http, session) { }

    public async Task SubmitAsync(SubmitTimesheetDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/timesheets", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task<List<TimesheetSummaryDto>> GetMineAsync()
    {
        var response = await _http.GetAsync("api/timesheets/mine");
        return await ReadAsync<List<TimesheetSummaryDto>>(response);
    }

    public async Task<List<TeamTimesheetEntryDto>> GetTeamAsync(DateOnly weekStart)
    {
        var dateStr = weekStart.ToString("dd-MM-yyyy");
        var response = await _http.GetAsync($"api/timesheets/team?week={dateStr}");
        return await ReadAsync<List<TeamTimesheetEntryDto>>(response);
    }

    public async Task<bool> CheckMissedAsync()
    {
        var response = await _http.GetAsync("api/timesheets/missed-check");
        await EnsureSuccessAsync(response);
        var result = await ReadAsync<System.Text.Json.JsonElement>(response);
        return result.GetProperty("hasMissed").GetBoolean();
    }

    public async Task<List<PRM.Application.DTOs.Employee.EmployeeAllocationDto>> GetWeekAllocationsAsync(DateOnly weekStart)
    {
        var dateStr = weekStart.ToString("dd-MM-yyyy");
        var response = await _http.GetAsync($"api/timesheets/week-allocations?week={dateStr}");
        return await ReadAsync<List<PRM.Application.DTOs.Employee.EmployeeAllocationDto>>(response);
    }
}
