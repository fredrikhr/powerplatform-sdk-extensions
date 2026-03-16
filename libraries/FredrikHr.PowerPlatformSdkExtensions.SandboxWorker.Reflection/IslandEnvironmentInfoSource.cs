using Microsoft.PowerApps.CoreFramework.PowerPlatform.ResourceDiscovery;

namespace Microsoft.CDSRuntime.SandboxWorker;

public readonly record struct IslandEnvironmentInfoSource
{
    private const string AssemblyQualifiedName =
        "Microsoft.CDSRuntime.SandboxWorker.IslandEnvironmentInfoSource, Microsoft.CDSRuntime.SandboxWorker, PublicKeyToken=31bf3856ad364e35";

    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly object Target { get; }

    public static IslandEnvironmentInfoSource Wrap(object target) => new(target);

    private IslandEnvironmentInfoSource(object target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public IslandEnvironmentInfoSource(IslandEnvironmentContext islandEnvironmentContext)
        : this(Activator.CreateInstance(TypeReference, args: [islandEnvironmentContext.Target]))
    { }

    public readonly IEnvironmentInfoSource AsIEnvironmentInfoSource() =>
        (IEnvironmentInfoSource)Target;

    public readonly EnvironmentInfo GetEnvironmentInfo() =>
        AsIEnvironmentInfoSource().GetEnvironmentInfo();
}