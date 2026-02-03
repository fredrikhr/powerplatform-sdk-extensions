namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed record class GlobalDiscoveryServiceInstance(
    string ApiUrl,
    string DatacenterId,
    string DatacenterName,
    string EnvironmentId,
    string FriendlyName,
    Guid Id,
    bool IsUserSysAdmin,
    DateTimeOffset LastUpdated,
    int OrganizationType,
    string Purpose,
    string Region,
    string SchemaType,
    int State,
    int StatusMessage,
    Guid TenantId,
    DateTimeOffset TrialExpirationDate,
    string UniqueName,
    string UrlName,
    string Version,
    string Url
    );