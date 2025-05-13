namespace FredrikHr.PowerPlatformSdkExtensions.DataverseUtilityPlugins;

internal static class PowerPlatformApiUrls
{
    private const StringComparison Ord = StringComparison.OrdinalIgnoreCase;

    private static (string endpointSuffix, int idSuffixLength) GetSuffixInfo(string clusterCategory)
    {
        return clusterCategory switch
        {
            string _ when clusterCategory.Equals("Local", Ord) => ("api.powerplatform.localhost", 1),
            string _ when clusterCategory.Equals("Exp", Ord) => ("api.exp.powerplatform.com", 2),
            string _ when clusterCategory.Equals("Dev", Ord) => ("api.dev.powerplatform.com", 2),
            string _ when clusterCategory.Equals("Prv", Ord) => ("api.prv.powerplatform.com", 1),
            string _ when clusterCategory.Equals("Test", Ord) => ("api.test.powerplatform.com", 2),
            string _ when clusterCategory.Equals("Preprod", Ord) => ("api.preprod.powerplatform.com", 2),
            string _ when clusterCategory.Equals("FirstRelease", Ord) => ("api.powerplatform.com", 2),
            string _ when clusterCategory.Equals("Prod", Ord) => ("api.powerplatform.com", 2),
            string _ when clusterCategory.Equals("Gov", Ord) => ("api.gov.powerplatform.microsoft.us", 1),
            string _ when clusterCategory.Equals("High", Ord) => ("api.high.powerplatform.microsoft.us", 1),
            string _ when clusterCategory.Equals("DoD", Ord) => ("api.appsplatform.us", 1),
            string _ when clusterCategory.Equals("Mooncake", Ord) => ("api.powerplatform.partner.microsoftonline.cn", 1),
            string _ when clusterCategory.Equals("Ex", Ord) => ("api.powerplatform.eaglex.ic.gov", 1),
            string _ when clusterCategory.Equals("Rx", Ord) => ("api.powerplatform.microsoft.scloud", 1),
            _ => throw new ArgumentException($"Invalid cluster category value: {clusterCategory}", nameof(clusterCategory)),
        };
    }

    private static string BuildEndpoint(string infix, string resourceId, string endpointSuffix, int idSuffixLength)
    {
        resourceId = resourceId.Replace("-", "");
        var resourcePrefix = resourceId[..^idSuffixLength];
        var resourceSuffix = resourceId.Substring(resourceId.Length - idSuffixLength, idSuffixLength);
        return $"{resourcePrefix}.{resourceSuffix}.{infix}.{endpointSuffix}";
    }

    internal static
        (string tenantEndpoint, string environmentEndpoint, string tokenAudience)
        GetApiInformation(string tenantId, string environmentId, string clusterCategory)
    {
        (string endpointSuffix, int idSuffixLength) = GetSuffixInfo(clusterCategory);
        string tenantEndpoint = BuildEndpoint("tenant", tenantId, endpointSuffix, idSuffixLength);
        string environmentEndpoint = BuildEndpoint("environment", environmentId, endpointSuffix, idSuffixLength);
        return (
            $"https://{tenantEndpoint}",
            $"https://{environmentEndpoint}",
            $"https://{endpointSuffix}"
            );
    }
}
