namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed class FederatedCredentialsOptions
{
    public const string ConfigurationSectionName = "FederatedCredentials";
    public const string DefaultAudience = "api://AzureADTokenExchange";

    internal enum ProviderType
    {
        Unknown = 0,
        GitHubActions
    }

    public ProviderType Provider { get; set; }
    public string? Audience { get; set; } = DefaultAudience;
}