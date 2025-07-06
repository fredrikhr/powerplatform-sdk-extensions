namespace Microsoft.CDSRuntime.SandboxWorker;

public readonly record struct ISandboxWorkerShimServiceClient
{
    private const string AssemblyQualifiedName =
        "Microsoft.CDSRuntime.SandboxWorker.ISandboxWorkerShimServiceClient, Microsoft.CDSRuntime.SandboxWorker, PublicKeyToken=31bf3856ad364e35";

    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly object Target { get; }

    public ISandboxWorkerShimServiceClient(object target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public readonly WorkerMetadataResponse GetWorkerAssignedMetadata(Guid workerProcessGuid) => new(TypeReference.InvokeMember(
        nameof(GetWorkerAssignedMetadata),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
        Type.DefaultBinder,
        Target,
        [workerProcessGuid],
        culture: System.Globalization.CultureInfo.InvariantCulture
        ));
}
