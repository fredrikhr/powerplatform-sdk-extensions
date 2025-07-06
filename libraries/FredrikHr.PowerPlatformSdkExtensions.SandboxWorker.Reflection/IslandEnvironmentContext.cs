namespace Microsoft.CDSRuntime.SandboxWorker;

/*
public readonly record struct IslandEnvironmentContext
{
    private const string AssemblyQualifiedName =
        "Microsoft.CDSRuntime.SandboxCommon.IslandEnvironmentContext, Microsoft.CDSRuntime.SandboxCommon, PublicKeyToken=31bf3856ad364e35";
    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly object Target { get; }

    public IslandEnvironmentContext(object target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public string EnvironmentName => (string)TypeReference.InvokeMember(
        nameof(EnvironmentName),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public string ClusterCategory => (string)TypeReference.InvokeMember(
        nameof(ClusterCategory),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public string GeoName => (string)TypeReference.InvokeMember(
        nameof(GeoName),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public string ClusterName => (string)TypeReference.InvokeMember(
        nameof(ClusterName),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public string RegionShortName => (string)TypeReference.InvokeMember(
        nameof(RegionShortName),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public string RegionName => (string)TypeReference.InvokeMember(
        nameof(RegionName),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public string ClusterNumber => (string)TypeReference.InvokeMember(
        nameof(ClusterNumber),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );
}
*/