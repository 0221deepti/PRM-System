using System.Net.Http.Json;
using PRM.Application.DTOs.User;
using PRM.Client.Session;

namespace PRM.Client.HttpClients;

public class UserHttpClient : ApiClient
{
    public UserHttpClient(HttpClient http, SessionContext session) : base(http, session) { }

    public async Task<List<UserSummaryDto>> GetAllUsersAsync()
    {
        var response = await _http.GetAsync("api/users");
        return await ReadAsync<List<UserSummaryDto>>(response);
    }

    public async Task CreateUserAsync(CreateUserDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/users", dto);
        await EnsureSuccessAsync(response);
    }

    public async Task DeactivateUserAsync(int userId)
    {
        var response = await _http.PutAsync($"api/users/{userId}/deactivate", null);
        await EnsureSuccessAsync(response);
    }

    public async Task ReactivateUserAsync(int userId)
    {
        var response = await _http.PutAsync($"api/users/{userId}/reactivate", null);
        await EnsureSuccessAsync(response);
    }
}
