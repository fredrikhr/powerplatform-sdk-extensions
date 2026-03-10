namespace Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery;

public readonly record struct IEnvironmentInfoSource
{
    private const string AssemblyQualifiedName =
        "Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery.IEnvironmentInfoSource, CoreFramework.PowerPlatform.ResourceDiscovery, PublicKeyToken=31bf3856ad364e35";

    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly object Target { get; }

    public IEnvironmentInfoSource(object target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public readonly EnvironmentInfo GetEnvironmentInfo() => new(TypeReference.InvokeMember(
        nameof(GetEnvironmentInfo),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
        Type.DefaultBinder,
        Target,
        args: [],
        System.Globalization.CultureInfo.InvariantCulture
        ));
}