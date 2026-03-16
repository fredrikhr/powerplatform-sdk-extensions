using Autofac;

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Plex.SidecarContract;

namespace Microsoft.CDSRuntime.SandboxWorker;

public static class SandboxWorkerMain
{
    private const string AssemblyQualifiedName =
        "Microsoft.CDSRuntime.SandboxWorker.SandboxWorkerMain, Microsoft.CDSRuntime.SandboxWorker, PublicKeyToken=31bf3856ad364e35";
    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public static IContainer Container => (IContainer)TypeReference.InvokeMember(
        "_container",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField,
        binder: default,
        target: null,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public static CancellationTokenSource CancellationTokenSource => (CancellationTokenSource)TypeReference.InvokeMember(
        "_cancellationTokenSource",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField,
        binder: default,
        target: null,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public static ILogger Logger => (ILogger)TypeReference.InvokeMember(
        "_logger",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField,
        binder: default,
        target: null,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public static IContainerContext ContainerContext =>
        IContainerContext.Wrap(TypeReference.InvokeMember(
        "_containerContext",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField,
        binder: default,
        target: null,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        ));

    public static ISandboxWorkerShimServiceClient ShimClient =>
        ISandboxWorkerShimServiceClient.Wrap(TypeReference.InvokeMember(
        "_shimClient",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField,
        binder: default,
        target: null,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        ));

    public static Dictionary<string, Version> AssemblyRedirects => (Dictionary<string, Version>)TypeReference.InvokeMember(
        "_assemblyRedirects",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField,
        binder: default,
        target: null,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    private static readonly Func<Guid> WorkerProcessGuidGetter = (Func<Guid>)TypeReference.GetProperty(
        nameof(WorkerProcessGuid),
        BindingFlags.Static | BindingFlags.Public
        ).GetMethod.CreateDelegate(typeof(Func<Guid>));

    public static Guid WorkerProcessGuid => WorkerProcessGuidGetter();

    public static SidecarService.SidecarServiceClient SidecarServiceClient
    {
        get
        {
            object shimClient = ShimClient.Target;
            var sidecarServiceClient = (SidecarService.SidecarServiceClient)
                shimClient.GetType().InvokeMember(
                    "_client",
                    BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.GetField,
                    Type.DefaultBinder,
                    target: shimClient,
                    args: null,
                    System.Globalization.CultureInfo.InvariantCulture
                    );
            return sidecarServiceClient;
        }
    }
}