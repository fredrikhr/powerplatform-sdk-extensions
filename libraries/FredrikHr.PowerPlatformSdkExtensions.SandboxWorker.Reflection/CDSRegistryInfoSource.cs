namespace Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery;

public readonly record struct CDSRegistryInfoSource
{
    private const string AssemblyQualifiedName =
        "Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery.CDSRegistryInfoSource, CoreFramework.PowerPlatform.ResourceDiscovery, PublicKeyToken=31bf3856ad364e35";

    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly object Target { get; }

    public CDSRegistryInfoSource()
        : this(Activator.CreateInstance(TypeReference))
    { }

    public CDSRegistryInfoSource(object target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public readonly IEnvironmentInfoSource AsIEnvironmentInfoSource() =>
        new(Target);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2225: Operator overloads have named alternates",
        Justification = nameof(AsIEnvironmentInfoSource))]
    public static implicit operator IEnvironmentInfoSource(CDSRegistryInfoSource source) =>
        source.AsIEnvironmentInfoSource();

    public readonly EnvironmentInfo GetEnvironmentInfo() => new(TypeReference.InvokeMember(
        nameof(GetEnvironmentInfo),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
        Type.DefaultBinder,
        Target,
        args: [],
        System.Globalization.CultureInfo.InvariantCulture
        ));
}