namespace FredrikHr.PowerPlatformSdkExtensions.ApiDiscovery;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1056: URI-like properties should not be strings",
    Justification = nameof(PowerPlexPluginUtility.GetPowerPlatformBaseUri)
    )]
public class PowerPlatformApiDiscovery
{
    public PowerPlatformApiDiscovery(
        IPluginExecutionContext6 context,
        IEnvironmentService envInfo
        )
        : this(envInfo?.Geo ?? "any", context) { }

    public PowerPlatformApiDiscovery(
        string geo,
        IPluginExecutionContext6 context
        )
    {
        geo ??= "any";
        _ = context ?? throw new ArgumentNullException(nameof(context));

        string tenantId = context.TenantId.ToString();
        string environmentId = context.EnvironmentId;

        string tokenAudience = PowerPlexPluginUtility
            .GetPowerPlatformTokenAudienceUrl(geo);
        string tenantApiUrl = PowerPlexPluginUtility
            .GetPowerPlatformBaseUri(geo, tenantId)
            .Replace("environment", "tenant");
        string environmentApiUrl = PowerPlexPluginUtility
            .GetPowerPlatformBaseUri(geo, environmentId);

        TenantApiUrl = tenantApiUrl;
        EnvironmentApiUrl = environmentApiUrl;
        TokenAudience = tokenAudience;
    }

    public string TenantApiUrl { get; }

    public string EnvironmentApiUrl { get; }

    public string TokenAudience { get; }
}