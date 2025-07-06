namespace Microsoft.CDSRuntime.SandboxWorker;

public readonly record struct IContainerContext
{
    private const string AssemblyQualifiedName =
        "Microsoft.CDSRuntime.SandboxWorker.IContainerContext, Microsoft.CDSRuntime.SandboxWorker, PublicKeyToken=31bf3856ad364e35";
    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public readonly object Target { get; }

    public IContainerContext(object target)
    {
        _ = target ?? throw new ArgumentNullException(nameof(target));
        if (!TypeReference.IsAssignableFrom(target.GetType()))
            throw new InvalidCastException();
        Target = target;
    }

    public readonly string ContainerIP => (string)TypeReference.InvokeMember(
        nameof(ContainerIP),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly int ContainerServicePort => (int)TypeReference.InvokeMember(
        nameof(ContainerServicePort),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string SidecarIP => (string)TypeReference.InvokeMember(
        nameof(SidecarIP),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly int SidecarPort => (int)TypeReference.InvokeMember(
        nameof(SidecarPort),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string SSLCertPath => (string)TypeReference.InvokeMember(
        nameof(SSLCertPath),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly Guid WorkerProcessGuid => (Guid)TypeReference.InvokeMember(
        nameof(WorkerProcessGuid),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string SSLCertName => (string)TypeReference.InvokeMember(
        nameof(SSLCertName),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string Authority => (string)TypeReference.InvokeMember(
        nameof(Authority),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string ValidIssuer => (string)TypeReference.InvokeMember(
        nameof(ValidIssuer),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string ValidAudience => (string)TypeReference.InvokeMember(
        nameof(ValidAudience),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string ContainerReadWriteFolderPath => (string)TypeReference.InvokeMember(
        nameof(ContainerReadWriteFolderPath),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly int ContainerHealthProbePort => (int)TypeReference.InvokeMember(
        nameof(ContainerHealthProbePort),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly bool IsWindowsContainer => (bool)TypeReference.InvokeMember(
        nameof(IsWindowsContainer),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string AzureAuthorityHost => (string)TypeReference.InvokeMember(
        nameof(AzureAuthorityHost),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly bool IsRunningOnDelegatedNetwork => (bool)TypeReference.InvokeMember(
        nameof(IsRunningOnDelegatedNetwork),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly bool ConsoleLoggingEnabled => (bool)TypeReference.InvokeMember(
        nameof(ConsoleLoggingEnabled),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string SandboxFabricClientAppUriFormat => (string)TypeReference.InvokeMember(
        nameof(SandboxFabricClientAppUriFormat),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly string ServiceName => (string)TypeReference.InvokeMember(
        nameof(ServiceName),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.GetProperty,
        binder: default,
        Target,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    /*
    public readonly IslandEnvironmentContext GetIslandEnvironmentContext(ISandboxWorkerShimServiceClient shimServiceClient) => new(TypeReference.InvokeMember(
        nameof(GetIslandEnvironmentContext),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.InvokeMethod,
        binder: default,
        Target,
        args: [shimServiceClient.Target],
        culture: System.Globalization.CultureInfo.InvariantCulture
        ));
    */

    public readonly string GetAllowedAppIds(ISandboxWorkerShimServiceClient shimServiceClient) => (string)TypeReference.InvokeMember(
        nameof(GetAllowedAppIds),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.InvokeMethod,
        binder: default,
        Target,
        args: [shimServiceClient.Target],
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public readonly bool GetAddNonceValidatorInterceptor(ISandboxWorkerShimServiceClient shimServiceClient) => (bool)TypeReference.InvokeMember(
        nameof(GetAddNonceValidatorInterceptor),
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.InvokeMethod,
        binder: default,
        Target,
        args: [shimServiceClient.Target],
        culture: System.Globalization.CultureInfo.InvariantCulture
        );
}
