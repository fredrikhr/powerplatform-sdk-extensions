using Autofac;

using Microsoft.Extensions.Logging;

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

    public static IContainerContext ContainerContext => new(TypeReference.InvokeMember(
        "_containerContext",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField,
        binder: default,
        target: null,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        ));

    public static ISandboxWorkerShimServiceClient ShimClient => new(TypeReference.InvokeMember(
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
}
