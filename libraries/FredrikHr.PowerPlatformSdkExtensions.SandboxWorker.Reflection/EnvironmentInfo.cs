namespace Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery;

public readonly record struct EnvironmentInfo
{
    private const string AssemblyQualifiedName =
        "Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery.EnvironmentInfo, CoreFramework.PowerPlatform.ResourceDiscovery, PublicKeyToken=31bf3856ad364e35";

    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly object Target { get; }

    public EnvironmentInfo(object target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public readonly string EnvironmentName => (string)TypeReference.InvokeMember(
        nameof(EnvironmentName),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        Type.DefaultBinder,
        Target,
        args: null,
        System.Globalization.CultureInfo.InvariantCulture
        );

	public readonly string RegionShortName => (string)TypeReference.InvokeMember(
        nameof(RegionShortName),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        Type.DefaultBinder,
        Target,
        args: null,
        System.Globalization.CultureInfo.InvariantCulture
        );

	public readonly ISet<string> RegionShortNames => (ISet<string>)TypeReference.InvokeMember(
        nameof(RegionShortNames),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        Type.DefaultBinder,
        Target,
        args: null,
        System.Globalization.CultureInfo.InvariantCulture
        );

	public readonly string ClusterCategory => (string)TypeReference.InvokeMember(
        nameof(ClusterCategory),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        Type.DefaultBinder,
        Target,
        args: null,
        System.Globalization.CultureInfo.InvariantCulture
        );

	public readonly string ClusterNumber => (string)TypeReference.InvokeMember(
        nameof(ClusterNumber),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        Type.DefaultBinder,
        Target,
        args: null,
        System.Globalization.CultureInfo.InvariantCulture
        );

	public readonly string GeoName => (string)TypeReference.InvokeMember(
        nameof(GeoName),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        Type.DefaultBinder,
        Target,
        args: null,
        System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string GetDefaultPowerappsDnsZone()
    {
        const string clusterCategoryTypeName =
            "Microsoft.PowerApps.CoreFramework.CapCoreServices.TopologyModel.ClusterCategory, " +
            "CoreFramework.CapCoreServices.TopologyModel, PublicKeyToken=31bf3856ad364e35";
        Type clusterCategoryTypeRef = Type.GetType(clusterCategoryTypeName, throwOnError: true);
        object clusterCategory;
        try
        {
            clusterCategory = Enum.Parse(clusterCategoryTypeRef, ClusterCategory, ignoreCase: true);
        }
        catch (Exception clusterCategoryExcept)
        {
            throw new ArgumentException(
                message: $"Invalid cluster category value: {ClusterCategory}",
                paramName: nameof(ClusterCategory),
                clusterCategoryExcept
                );
        }

        const string clusterCategoryExtensionTypeName =
            "Microsoft.PowerApps.CoreFramework.CapCoreServices.TopologyModel.ClusterCategoryExtensions, " +
            "CoreFramework.CapCoreServices.TopologyModel, PublicKeyToken=31bf3856ad364e35";
        Type clusterCategoryExtensionTypeRef = Type.GetType(clusterCategoryExtensionTypeName, throwOnError: true);

        return (string)clusterCategoryExtensionTypeRef.InvokeMember(
            nameof(GetDefaultPowerappsDnsZone),
            BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
            Type.DefaultBinder,
            target: null,
            args: [clusterCategory],
            System.Globalization.CultureInfo.InvariantCulture
            );
    }
}