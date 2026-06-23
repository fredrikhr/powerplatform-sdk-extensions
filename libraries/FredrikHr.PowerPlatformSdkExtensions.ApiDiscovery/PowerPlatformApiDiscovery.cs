using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.PowerApps.CoreFramework.PowerPlatform.Api;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class PowerPlatformApiDiscovery
{
    private const StringComparison Cmp = StringComparison.OrdinalIgnoreCase;

    private static readonly (string GlobalEndpoint, string GlobalUserContentEndpoint) DefaultEndpointEntry =
        ("api.powerplatform.com", "api.powerplatformusercontent.com");
    private static readonly Dictionary<string, (string GlobalEndpoint, string GlobalUserContentEndpoint)> ClusterCatorgyEndpointMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Prod", DefaultEndpointEntry },
        { "FirstRelease", DefaultEndpointEntry },
        { "Local", ("api.powerplatform.localhost", "api.powerplatformusercontent.localhost") },
        { "Exp", ("api.exp.powerplatform.com", "api.exp.powerplatformusercontent.com") },
        { "Dev", ("api.dev.powerplatform.com", "api.dev.powerplatformusercontent.com") },
        { "Prv", ("api.prv.powerplatform.com", "api.prv.powerplatformusercontent.com") },
        { "Test", ("api.test.powerplatform.com", "api.test.powerplatformusercontent.com") },
        { "Preprod", ("api.preprod.powerplatform.com", "api.preprod.powerplatformusercontent.com") },
        { "GovFR", ("api.gov.powerplatform.microsoft.us", "api.gov.powerplatformusercontent.microsoft.us") },
        { "Gov", ("api.gov.powerplatform.microsoft.us", "api.gov.powerplatformusercontent.microsoft.us") },
        { "High", ("api.high.powerplatform.microsoft.us", "api.high.powerplatformusercontent.microsoft.us") },
        { "DoD", ("api.appsplatform.us", "api.appsplatformusercontent.us") },
        { "Mooncake", ("api.powerplatform.partner.microsoftonline.cn", "api.powerplatformusercontent.partner.microsoftonline.cn") },
        { "Ex", ("api.powerplatform.eaglex.ic.gov", "api.powerplatformusercontent.eaglex.ic.gov") },
        { "Rx", ("api.powerplatform.microsoft.scloud", "api.powerplatformusercontent.microsoft.scloud") },
    };

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
            string c when string.Equals("Prod", c, Cmp) => 2,
            string c when string.Equals("FirstRelease", c, Cmp) => 2,
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
        if (!ClusterCatorgyEndpointMap.TryGetValue(clusterCategory, out var endpointEntry))
            endpointEntry = DefaultEndpointEntry;
        (GlobalEndpoint, GlobalUserContentEndpoint) = endpointEntry;
    }

    private static readonly Type? InternalEnvironmentServiceType = Type.GetType(
        "Microsoft.Xrm.Sdk.IInternalEnvironmentService" + ", " +
        "Microsoft.Xrm.Kernel.Contracts.Internal, PublicKeyToken=31bf3856ad364e35",
        throwOnError: false
        );

    private static string? GetClusterCategoryName(IEnvironmentService envService)
    {
        return InternalEnvironmentServiceType?.InvokeMember(
            "ClusterCategory",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.GetProperty,
            binder: default,
            target: envService,
            args: null,
            System.Globalization.CultureInfo.InvariantCulture
            ) as string;
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