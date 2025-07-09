namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed record class MsalDelegatingHandlerScopeEntry(
    Uri RequestUri,
    IEnumerable<string> Scopes
    );
