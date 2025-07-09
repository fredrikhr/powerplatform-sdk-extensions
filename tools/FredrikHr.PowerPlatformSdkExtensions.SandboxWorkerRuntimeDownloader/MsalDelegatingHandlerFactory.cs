using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed class MsalDelegatingHandlerFactory(
    IOptionsMonitor<MsalDelegatingHandlerOptions> optionsProvider,
    MsalDelegatingHandlerScopeRegistry scopeRegistry,
    IOptionsMonitor<IPublicClientApplication> publicClientProvider,
    IOptionsMonitor<IConfidentialClientApplication> confidentialClientProvider,
    IEnumerable<IConfigureOptions<MsalDelegatingHandler>> setups,
    IEnumerable<IPostConfigureOptions<MsalDelegatingHandler>> postConfigures,
    IEnumerable<IValidateOptions<MsalDelegatingHandler>> validations,
    ILogger<DeviceCodeResult> deviceCodeLogger
    ) : OptionsFactory<MsalDelegatingHandler>(setups, postConfigures, validations)
{
    protected override MsalDelegatingHandler CreateInstance(string name)
    {
        return new(
            name,
            scopeRegistry,
            optionsProvider,
            publicClientProvider,
            confidentialClientProvider,
            deviceCodeLogger
            );
    }
}