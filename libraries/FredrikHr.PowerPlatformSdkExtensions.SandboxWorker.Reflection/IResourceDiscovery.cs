namespace Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery;

public readonly record struct IResourceDiscovery
{
    private const string AssemblyQualifiedName =
        "Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery.IResourceDiscovery, CoreFramework.PowerPlatform.ResourceDiscovery, PublicKeyToken=31bf3856ad364e35";

    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly object Target { get; }

    public IResourceDiscovery(object target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public static IResourceDiscovery CreateUsingNamingConvention(
        IEnvironmentInfoSource environmentInfoSource
        )
    {
        const string typeName = "Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery.NamingConventionResourceDiscovery";
        Type typeRef = TypeReference.Assembly.GetType(typeName, throwOnError: true);
        object target = Activator.CreateInstance(typeRef, environmentInfoSource);
        return new(target);
    }

    public readonly string GetResourceName(
        string serviceTag,
        string? serviceResourceName = null
        ) => (string)TypeReference.InvokeMember(
            nameof(GetResourceName),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
            Type.DefaultBinder,
            Target,
            args: [serviceTag, serviceResourceName],
            System.Globalization.CultureInfo.InvariantCulture
            );

    public readonly IEnumerable<string> GetResourceNames(
        string serviceTag,
        string? serviceResourceName = null
        ) => (IEnumerable<string>)TypeReference.InvokeMember(
            nameof(GetResourceNames),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
            Type.DefaultBinder,
            Target,
            args: [serviceTag, serviceResourceName],
            System.Globalization.CultureInfo.InvariantCulture
            );

    public readonly string GetEndpointName(
        string serviceTag,
        string? serviceEndpointName = null,
        string? dnsZone = null
        ) => (string)TypeReference.InvokeMember(
            nameof(GetEndpointName),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
            Type.DefaultBinder,
            Target,
            args: [serviceTag, serviceEndpointName, dnsZone],
            System.Globalization.CultureInfo.InvariantCulture
            );

    public readonly IEnumerable<string> GetEndpointNames(
        string serviceTag,
        string? serviceEndpointName = null,
        string? dnsZone = null
        ) => (IEnumerable<string>)TypeReference.InvokeMember(
            nameof(GetEndpointNames),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
            Type.DefaultBinder,
            Target,
            args: [serviceTag, serviceEndpointName, dnsZone],
            System.Globalization.CultureInfo.InvariantCulture
            );

    public readonly string GetGeoEndpointName(
        string serviceTag,
        string? serviceEndpointName = null,
        string? dnsZone = null
        ) => (string)TypeReference.InvokeMember(
            nameof(GetGeoEndpointName),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
            Type.DefaultBinder,
            Target,
            args: [serviceTag, serviceEndpointName, dnsZone],
            System.Globalization.CultureInfo.InvariantCulture
            );
}