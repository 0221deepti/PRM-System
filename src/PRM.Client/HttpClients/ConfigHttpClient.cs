using System.Net.Http.Json;
using PRM.Application.DTOs.Config;
using PRM.Client.Session;

namespace PRM.Client.HttpClients;

public class ConfigHttpClient : ApiClient
{
    public ConfigHttpClient(HttpClient http, SessionContext session) : base(http, session) { }

    public async Task<SystemConfigDto> GetConfigAsync()
    {
        var response = await _http.GetAsync("api/config");
        return await ReadAsync<SystemConfigDto>(response);
    }

    public async Task UpdateConfigAsync(UpdateConfigDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/config", dto);
        await EnsureSuccessAsync(response);
    }
}
