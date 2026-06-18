using System.Net.Http.Json;
using PRM.Application.DTOs.Auth;
using PRM.Client.Session;
using PRM.Domain.Enums;

namespace PRM.Client.HttpClients;

public class AuthHttpClient : ApiClient
{
    public AuthHttpClient(HttpClient http, SessionContext session) : base(http, session) { }

    public async Task<LoginResponseDto> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequestDto(username, password));
        var result = await ReadAsync<LoginResponseDto>(response);

        // Update session state
        _session.Token = result.Token;
        _session.UserFullName = result.FullName;
        _session.Role = Enum.Parse<UserRole>(result.RoleName);
        _session.UserId = result.UserId;
        _session.EmployeeId = result.EmployeeId;

        // Re-attach token to current HttpClient instance
        _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _session.Token);

        return result;
    }

    public async Task ChangePasswordAsync(string newPassword, string confirmPassword)
    {
        var response = await _http.PutAsJsonAsync("api/auth/change-password", new ChangePasswordDto(newPassword, confirmPassword));
        await EnsureSuccessAsync(response);
    }
}
