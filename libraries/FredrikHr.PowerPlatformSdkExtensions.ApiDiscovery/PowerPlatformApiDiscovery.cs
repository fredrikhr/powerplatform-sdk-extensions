using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.PowerApps.CoreFramework.PowerPlatform.Api;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class PowerPlatformApiDiscovery
{
    private const StringComparison Cmp = StringComparison.OrdinalIgnoreCase;

    private readonly IPluginExecutionContext6 _context;
    private const string TenantInfix = "tenant";

    private const string EnvironmentInfix = "environment";

    private const string OrganizationInfix = "organization";

    private const string TenantIslandPrefix = "il-";
    private readonly int _idSuffixLength;

    public string TokenAudience => "https://" + GlobalEndpoint;

    public string GlobalEndpoint { get; }

    public string GlobalUserContentEndpoint { get; }

    public string TenantEndpoint
        => field ??= GetTenantEndpoint(_context.TenantId);
    public string TenantIslandClusterEndpoint
        => field ??= GetTenantIslandClusterEndpoint(_context.TenantId);
    public string EnvironmentEndpoint
        => field ??= GetEnvironmentEndpoint(_context.EnvironmentId);
    public string EnvironmentUserContentEndpoint
        => field ??= GetEnvironmentUserContentEndpoint(_context.EnvironmentId);
    public string OrganizationEndpoint
        => field ??= GetOrganizationEndpoint(_context.OrganizationId);

    private string GetTenantEndpoint(Guid tenantId)
    {
        return BuildEndpoint(TenantInfix, tenantId.ToString("N"));
    }

    private string GetTenantIslandClusterEndpoint(Guid tenantId)
    {
        return BuildEndpoint(TenantInfix, tenantId.ToString("N"), TenantIslandPrefix);
    }

    private string GetEnvironmentEndpoint(string environmentId)
    {
        ThrowIfStringIsNullOrEmpty(environmentId);
        return BuildEndpoint(EnvironmentInfix, environmentId);
    }

    private string GetEnvironmentUserContentEndpoint(string environmentId)
    {
        ThrowIfStringIsNullOrEmpty(environmentId);
        return BuildEndpoint(EnvironmentInfix, environmentId, "", userContentEndpoint: true);
    }

    private string GetOrganizationEndpoint(Guid organizationId)
    {
        return BuildEndpoint(OrganizationInfix, organizationId.ToString("N"));
    }

    [SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "URL")]
    private string BuildEndpoint(string infix, string resourceId, string prefix = "", bool userContentEndpoint = false)
    {
        string urlSafeResourceId = resourceId.ToLowerInvariant().Replace("-", "");
        string resourceIdPrefix = urlSafeResourceId[..^_idSuffixLength];
        string resourceIdSuffix = urlSafeResourceId[^_idSuffixLength..];
        // string text3 = text.Substring(text.Length - idSuffixLength, idSuffixLength);
        string suffix = userContentEndpoint ? GlobalUserContentEndpoint : GlobalEndpoint;
        return prefix + resourceIdPrefix + "." + resourceIdSuffix + "." + infix + "." + suffix;
    }

    private static int GetIdSuffixLength(string category)
    {
        return category switch
        {
            string c when string.Equals("FirstRelease", c, Cmp) => 2,
            string c when string.Equals("Prod", c, Cmp) => 2,
            _ => 1,
        };
    }

    public PowerPlatformApiDiscovery(
        IPluginExecutionContext6 context,
        IEnvironmentService envService
        )
    {
        _context = context;
        string clusterCategory = GetClusterCategoryName(envService) ?? "Prod";
        _idSuffixLength = GetIdSuffixLength(clusterCategory);
        (GlobalEndpoint, GlobalUserContentEndpoint) = clusterCategory switch
        {
            string c when string.Equals("Local", c, Cmp)
                => ("api.powerplatform.localhost", "api.powerplatformusercontent.localhost"),
            string c when string.Equals("Exp", c, Cmp)
                => ("api.exp.powerplatform.com", "api.exp.powerplatformusercontent.com"),
            string c when string.Equals("Dev", c, Cmp)
                => ("api.dev.powerplatform.com", "api.dev.powerplatformusercontent.com"),
            string c when string.Equals("Prv", c, Cmp)
                => ("api.prv.powerplatform.com", "api.prv.powerplatformusercontent.com"),
            string c when string.Equals("Test", c, Cmp)
                => ("api.test.powerplatform.com", "api.test.powerplatformusercontent.com"),
            string c when string.Equals("Preprod", c, Cmp)
                => ("api.preprod.powerplatform.com", "api.preprod.powerplatformusercontent.com"),
            string c when string.Equals("GovFR", c, Cmp)
                => ("api.gov.powerplatform.microsoft.us", "api.gov.powerplatformusercontent.microsoft.us"),
            string c when string.Equals("Gov", c, Cmp)
                => ("api.gov.powerplatform.microsoft.us", "api.gov.powerplatformusercontent.microsoft.us"),
            string c when string.Equals("High", c, Cmp)
                => ("api.high.powerplatform.microsoft.us", "api.high.powerplatformusercontent.microsoft.us"),
            string c when string.Equals("DoD", c, Cmp)
                => ("api.appsplatform.us", "api.appsplatformusercontent.us"),
            string c when string.Equals("Mooncake", c, Cmp)
                => ("api.powerplatform.partner.microsoftonline.cn", "api.powerplatformusercontent.partner.microsoftonline.cn"),
            string c when string.Equals("Ex", c, Cmp)
                => ("api.powerplatform.eaglex.ic.gov", "api.powerplatformusercontent.eaglex.ic.gov"),
            string c when string.Equals("Rx", c, Cmp)
                => ("api.powerplatform.microsoft.scloud", "api.powerplatformusercontent.microsoft.scloud"),
            _ => ("api.powerplatform.com", "api.powerplatformusercontent.com"),
        };
    }

    private static string? GetClusterCategoryName(IEnvironmentService envService)
    {
        dynamic envInternalService = envService;
        return envInternalService.ClusterCategory as string;
    }

    private static void ThrowIfStringIsNullOrEmpty(
        [NotNull] string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null
        )
    {
        switch (argument)
        {
            case null:
                throw new ArgumentNullException(paramName);
            case "":
                throw new ArgumentException(message: default, paramName);
        }
    }
}