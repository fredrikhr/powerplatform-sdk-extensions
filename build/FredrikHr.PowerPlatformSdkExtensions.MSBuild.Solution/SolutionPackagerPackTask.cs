using Microsoft.Build.Framework;
using Microsoft.Crm.Tools.SolutionPackager;

namespace FredrikHr.PowerPlatformSdkExtensions.MSBuild.Solution;

public sealed class SolutionPackagerPackTask : SolutionPackagerTaskBase, ITask
{
    public bool UseUnmanagedFileForManaged { get; init; }

    public bool DisablePluginAssemblyTypeNameRemapping { get; init; }

    public string? SolutionName { get; init; }

    protected override PackagerArguments GetArguments()
    {
        PackagerArguments arguments = base.GetArguments();

        arguments.Action = CommandAction.Pack;
        arguments.UseUnmanagedFileForManaged = UseUnmanagedFileForManaged;
        arguments.RemapPluginTypeNames = !DisablePluginAssemblyTypeNameRemapping;
        arguments.SolutionName = SolutionName;

        return arguments;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031: Do not catch general exception types",
        Justification = nameof(Log))]
    protected override string? GetLogSourceFile(
        PackagerArguments arguments,
        SolutionPackager runner)
    {
        Context context = runner.Context;
        try
        {
            return Path.Combine(
                context.RootFolder,
                context.ComponentConfigurationManager.ConfigurationSection.SolutionFile
            );
        }
        catch (Exception pathExcept)
        {
            Log.LogErrorFromException(pathExcept);
            return null;
        }
    }
}