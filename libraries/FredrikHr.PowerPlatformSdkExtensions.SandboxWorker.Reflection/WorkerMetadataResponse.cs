using Google.Protobuf.WellKnownTypes;

namespace Microsoft.CDSRuntime.SandboxWorker;

public readonly record struct WorkerMetadataResponse
{
    private const string AssemblyQualifiedName =
        "Microsoft.PowerPlatform.Plex.SidecarContract.WorkerMetadataResponse, Microsoft.PowerPlatform.Plex.SidecarContract, PublicKeyToken=31bf3856ad364e35";
    public static System.Type TypeReference { get; } =
        System.Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly object Target { get; }

    public WorkerMetadataResponse(object target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public readonly string WorkerMetadata => (string)TypeReference.InvokeMember(
        nameof(WorkerMetadata),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string ApplicationInsightsKey => (string)TypeReference.InvokeMember(
        nameof(ApplicationInsightsKey),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly Timestamp WorkerThrottleEndtime => (Timestamp)TypeReference.InvokeMember(
        nameof(WorkerThrottleEndtime),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );
}