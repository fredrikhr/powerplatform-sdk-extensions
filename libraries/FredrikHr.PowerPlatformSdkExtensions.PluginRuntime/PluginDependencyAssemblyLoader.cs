using System.Reflection;

namespace FredrikHr.PowerPlatformSdkExtensions.PluginRuntime;

internal sealed class PluginDependencyAssemblyLoader : IDisposable
{
    private readonly IPlugin _plugin;
    private readonly ITracingService _trace;
    private readonly ResolveEventHandler _resolveEventHandler;

    public PluginDependencyAssemblyLoader(
        IPlugin plugin,
        ITracingService trace
        )
    {
        _plugin = plugin;
        _trace = trace;
        _resolveEventHandler = PluginExecutionRuntimeAssemblyResolve;

        AppDomain.CurrentDomain.AssemblyResolve +=
            _resolveEventHandler;
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.AssemblyResolve -=
            _resolveEventHandler;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031: Do not catch general exception types",
        Justification = nameof(ResolveEventHandler)
        )]
    private Assembly PluginExecutionRuntimeAssemblyResolve(
        object sender,
        ResolveEventArgs args
        )
    {
        ITracingService trace = _trace;
        if (string.IsNullOrEmpty(args.Name)) return null!;
        Assembly? loadedAssembly;
        try
        {
            AssemblyName name = new(args.Name);
            string filename = $"{name.Name}.dll";

            foreach (string filepath in GetPossibleFilepaths(filename, _plugin, trace))
            {
                if (File.Exists(filepath))
                {
                    loadedAssembly = Assembly.LoadFile(filepath);
                    try
                    {
                        trace?.Trace(
                            "Requested assembly '{0}' -> loaded assembly '{1}' from path '{2}'.",
                            name,
                            loadedAssembly.GetName(),
                            filepath
                            );
                    }
                    catch (Exception)
                    {
                        // Ignore exception from trace on purpose
                    }
                    return loadedAssembly;
                }
            }
        }
        catch (Exception) { return null!; }

        return null!;

        static IEnumerable<string> GetPossibleFilepaths(
            string filename,
            IPlugin plugin,
            ITracingService trace
            )
        {
            string filepath;

            GetFilePathsFromPluginAssembly(
                plugin,
                trace,
                out string? locationDirectoryPath,
                out string? codeBaseDirectoryPath
                );
            if (locationDirectoryPath is not null)
            {
                filepath = Path.Combine(locationDirectoryPath, filename);
                yield return filepath;
            }
            if (codeBaseDirectoryPath is not null)
            {
                filepath = Path.Combine(codeBaseDirectoryPath, filename);
                yield return filepath;
            }

            filepath = Path.Combine(Environment.CurrentDirectory, filename);
            yield return filepath;

            string cultureDirectory = System.Globalization.CultureInfo.CurrentCulture.Name;
            filepath = Path.Combine(Environment.CurrentDirectory, cultureDirectory, filename);
            yield return filepath;

            cultureDirectory = "en-US";
            filepath = Path.Combine(Environment.CurrentDirectory, cultureDirectory, filename);
            yield return filepath;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031: Do not catch general exception types",
        Justification = nameof(ResolveEventHandler)
        )]
    private static void GetFilePathsFromPluginAssembly(
        IPlugin plugin,
        ITracingService trace,
        out string? locationDirectoryPath,
        out string? codeBaseDirectoryPath
        )
    {
        locationDirectoryPath = null;
        codeBaseDirectoryPath = null;
        Assembly thisAssembly = plugin?.GetType().Assembly ??
            typeof(PluginDependencyAssemblyLoader).Assembly;
        try
        {
            if (!string.IsNullOrEmpty(thisAssembly.Location) &&
                File.Exists(thisAssembly.Location))
            {
                locationDirectoryPath = Path.GetDirectoryName(thisAssembly.Location);
            }
        }
        catch (Exception pathExcept)
        {
            trace.Trace("While determining directory path for location of assembly: {0}", pathExcept);
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(thisAssembly.CodeBase) &&
                File.Exists(thisAssembly.CodeBase))
            {
                codeBaseDirectoryPath = Path.GetDirectoryName(thisAssembly.CodeBase);
            }
        }
        catch (Exception pathExcept)
        {
            trace.Trace("While determining directory path for code base of assembly: {0}", pathExcept);
            return;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031: Do not catch general exception types",
        Justification = nameof(ITracingService)
        )]
    internal void PreloadAssemblies()
    {
        GetFilePathsFromPluginAssembly(
            _plugin,
            _trace,
            out string? path1,
            out string? path2);
        List<string> paths = new(capacity: 2);
        if (!string.IsNullOrEmpty(path1))
            paths.Add(path1!);
        if (!string.IsNullOrEmpty(path2) && !path2!.Equals(path1, StringComparison.OrdinalIgnoreCase))
            paths.Add(path2);
        foreach (string path in paths)
        {
            foreach (string dllPath in Directory.EnumerateFiles(path, "*.dll"))
            {
                try
                {
                    _trace.Trace("Preloading assembly: {0}", dllPath);
                    Assembly.LoadFile(dllPath);
                }
                catch (Exception assemblyLoadExcept)
                {
                    PluginBase.TraceException(_trace, assemblyLoadExcept);
                }
            }
        }
    }
}