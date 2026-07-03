using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PRM.Application.DTOs.Common;
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

        if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
        {
            throw new ApplicationException("Something went wrong. Please try again later.");
        }

        var content = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var msgProp))
            {
                var msg = msgProp.GetString();
                if (root.TryGetProperty("errors", out var errsProp) && errsProp.ValueKind == JsonValueKind.Array)
                {
                    var lines = new List<string>();
                    foreach (var err in errsProp.EnumerateArray())
                    {
                        var field = err.TryGetProperty("field", out var f) ? f.GetString() : null;
                        var message = err.TryGetProperty("message", out var m) ? m.GetString() : null;
                        if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(message))
                        {
                            lines.Add($"{field}: {message}");
                        }
                        else if (!string.IsNullOrEmpty(message))
                        {
                            lines.Add(message);
                        }
                    }
                    if (lines.Count > 0)
                    {
                        throw new ApplicationException(string.Join("\n", lines));
                    }
                }
                if (!string.IsNullOrEmpty(msg))
                {
                    throw new ApplicationException(msg);
                }
            }
        }
        catch (JsonException) { }

        throw new ApplicationException($"HTTP {response.StatusCode}: {content}");
    }

    protected async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(options);
        return wrapper!.Data!;
    }
}
