namespace Microsoft.CDSRuntime.SandboxCommon;

public static class SandboxUtility
{
    private const string AssemblyQualifiedName =
        "Microsoft.CDSRuntime.SandboxCommon.SandboxUtility, Microsoft.CDSRuntime.SandboxCommon, PublicKeyToken=31bf3856ad364e35";
    public static Type TypeReference { get; } =
        Type.GetType(AssemblyQualifiedName, throwOnError: true);

    public static Lazy<string> SandboxRootFilesPath => (Lazy<string>)TypeReference.InvokeMember(
        nameof(SandboxRootFilesPath),
        BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField,
        binder: default,
        target: null,
        args: null,
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public static string SandboxFilesPath() => (string)TypeReference.InvokeMember(
        nameof(SandboxFilesPath),
        BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
        binder: default,
        target: null,
        args: [],
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    private static readonly Func<string, Guid> fnParseOrgIdFromWorkerMetadata = (Func<string, Guid>)TypeReference
        .GetMethod(
            nameof(ParseOrgIdFromWorkerMetadata),
            BindingFlags.Static | BindingFlags.Public,
            binder: default,
            [typeof(string)],
            modifiers: null
        )
        .CreateDelegate(typeof(Func<string, Guid>));

    public static Guid ParseOrgIdFromWorkerMetadata(string workerMetadata) =>
        fnParseOrgIdFromWorkerMetadata(workerMetadata);

    public static string AssemblyCachePath(string? sandboxFilesPath = null) => (string)TypeReference.InvokeMember(
        nameof(AssemblyCachePath),
        BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
        binder: default,
        target: null,
        args: [sandboxFilesPath],
        culture: System.Globalization.CultureInfo.InvariantCulture
        );

    public static string PackageExtractionPath(string? sandboxFilesPath = null) => (string)TypeReference.InvokeMember(
        nameof(PackageExtractionPath),
        BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
        binder: default,
        target: null,
        args: [sandboxFilesPath],
        culture: System.Globalization.CultureInfo.InvariantCulture
        );
}