using Microsoft.Identity.Client;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed class MsalDelegatingHandlerOptions
{
    public MsalDelegatingHandlerAuthenticationMode AuthenticationMode { get; set; }
    public IAccount? UserAccount { get; set; }
}
