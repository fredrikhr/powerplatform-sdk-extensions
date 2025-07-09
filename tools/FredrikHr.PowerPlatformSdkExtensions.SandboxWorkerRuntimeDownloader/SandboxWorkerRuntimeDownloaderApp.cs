using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed partial class SandboxWorkerRuntimeDownloaderApp(
    IOptionsMonitor<MsalDelegatingHandlerOptions> msalAuthOptionsProvider,
    MsalDelegatingHandlerScopeRegistry scopeRegistry,
    IHttpClientFactory httpClientFactory,
    IConfiguration appConfig,
    ILogger<SandboxWorkerRuntimeDownloaderApp> logger
    )
{
    public async Task RunAsync(CancellationToken cancelToken)
    {
        using var httpClient = httpClientFactory.CreateClient();
        var environmentId = appConfig["EnvironmentId"];
        if (environmentId is null)
        {
            LogMissingEnvironmentId(logger);
            return;
        }

        if (IsUserAuthenticated())
        {
            Uri globalDiscoveryUri = new("https://globaldisco.crm.dynamics.com");
            scopeRegistry.AddEntry(
                globalDiscoveryUri,
                [$"{globalDiscoveryUri}/.default", "offline_access"]
                );

            using var globalDiscoveryRequMsg = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri(globalDiscoveryUri, "/api/discovery/v2.0/Instances" +
                $"?$filter={Uri.EscapeDataString($"EnvironmentId eq '{environmentId}'")}")
                );
            globalDiscoveryRequMsg.Headers.Add("OData-Version", "4.0");
            globalDiscoveryRequMsg.Headers.Add("OData-MaxVersion", "4.01");
            using var globalDiscoveryRespMsg = await httpClient.SendAsync(
                globalDiscoveryRequMsg,
                HttpCompletionOption.ResponseContentRead,
                cancelToken
                ).ConfigureAwait(continueOnCapturedContext: false);
            globalDiscoveryRespMsg.EnsureSuccessStatusCode();
            var globalDiscoveryRespRoot = await globalDiscoveryRespMsg.Content
                .ReadFromJsonAsync<JsonElement>(cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            var globalDiscoveryInstance = globalDiscoveryRespRoot
                .GetProperty("value").Deserialize<GlobalDiscoveryServiceInstance[]>()?
                .FirstOrDefault();
        }
        else
        {
            Uri powerAutomateApiUri = new("https://api.flow.microsoft.com");
            scopeRegistry.AddEntry(
                powerAutomateApiUri,
                ["https://service.flow.microsoft.com//.default"]
                );

            var environmentResource = await httpClient.GetFromJsonAsync<JsonElement>(
                new Uri(powerAutomateApiUri, $"/providers/microsoft.Flow/environments/{Uri.EscapeDataString(environmentId)}?api-version=2025-07-01"),
                cancelToken
                )
                .ConfigureAwait(continueOnCapturedContext: false);
            var environmentProps = environmentResource.GetProperty("properties");
            var environmentInstance = environmentProps
                .GetProperty("linkedEnvironmentMetadata")
                .Deserialize<PowerAutomateLinkedEnvironmentMetadata>();
        }
    }

    private bool IsUserAuthenticated()
    {
        var options = msalAuthOptionsProvider.CurrentValue;
        return options.AuthenticationMode switch
        {
            MsalDelegatingHandlerAuthenticationMode.UserInteractive or
            MsalDelegatingHandlerAuthenticationMode.UserDeviceCode =>
                true,
            _ => false,
        };
    }

    [LoggerMessage(LogLevel.Critical, EventName = "NoEnvironmentId",
        Message = "Missing configuration item: EnvironmentId"
    )]
    private static partial void LogMissingEnvironmentId(ILogger logger);
}
