using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed class GitHubActionsIdTokenClient(
    HttpClient httpClient
    ) : IDisposable
{
    private const string BearerAuthScheme = "Bearer";
    private const string TokenEnvVarName = "ACTIONS_ID_TOKEN_REQUEST_TOKEN";
    private const string UrlEnvVarName = "ACTIONS_ID_TOKEN_REQUEST_URL";

    public static bool IsAvailable =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(TokenEnvVarName)) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(UrlEnvVarName));

    private readonly Lazy<AuthenticationHeaderValue> _idTokenRequestToken = new(
        () => new(
        BearerAuthScheme,
        Environment.GetEnvironmentVariable(TokenEnvVarName)
        ));

    private readonly Lazy<Uri> _idTokenRequestUri = new(() => new(
        Environment.GetEnvironmentVariable(UrlEnvVarName)!
        ));

    public async Task<string?> GetIdTokenAsync(
        string? audience = default,
        CancellationToken cancelToken = default
        )
    {
        Uri idTokenUri = _idTokenRequestUri.Value;
        if (audience is not null)
        {
            string query = idTokenUri.Query ?? "";
            char querySep = query is { Length: > 0 } ? '&' : '?';
            query += $"{querySep}audience={Uri.EscapeDataString(audience)}";
            idTokenUri = new(idTokenUri, query);
        }
        using HttpRequestMessage idTokenRequ = new(HttpMethod.Get, idTokenUri)
        {
            Headers =
            {
                Authorization = _idTokenRequestToken.Value,
            }
        };
        var idTokenResp = await httpClient
            .SendAsync(idTokenRequ, HttpCompletionOption.ResponseContentRead, cancelToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        idTokenResp.EnsureSuccessStatusCode();
        var idTokenElem = await idTokenResp.Content.ReadFromJsonAsync<JsonElement>(
            cancelToken).ConfigureAwait(continueOnCapturedContext: false);
        return idTokenElem.GetProperty("value").GetString();
    }

    public void Dispose()
    {
        httpClient?.Dispose();
    }
}