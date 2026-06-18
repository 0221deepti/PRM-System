using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PRM.Client.Session;

namespace PRM.Client.HttpClients;

/// <summary>
/// Base class for all API clients. Handles token attachment and error parsing.
/// </summary>
public abstract class ApiClient
{
    protected readonly HttpClient _http;
    protected readonly SessionContext _session;

    protected ApiClient(HttpClient http, SessionContext session)
    {
        _http = http;
        _session = session;
        _http.BaseAddress = new Uri("http://localhost:5016/");

        if (_session.Token != null)
        {
            _http.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _session.Token);
        }
    }

    protected async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var content = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("error", out var errProp))
            {
                throw new ApplicationException(errProp.GetString());
            }
        }
        catch (JsonException) { }

        throw new ApplicationException($"HTTP {response.StatusCode}: {content}");
    }

    protected async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return (await response.Content.ReadFromJsonAsync<T>(options))!;
    }
}
