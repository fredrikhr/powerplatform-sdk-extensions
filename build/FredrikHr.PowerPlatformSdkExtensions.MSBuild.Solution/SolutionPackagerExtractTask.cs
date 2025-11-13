using Microsoft.Build.Framework;
using Microsoft.Crm.Tools.SolutionPackager;

namespace FredrikHr.PowerPlatformSdkExtensions.MSBuild.Solution;

public sealed class SolutionPackagerExtractTask : SolutionPackagerTaskBase, ITask
{
    public string? SingleComponent { get; init; }

    public bool AllowDeletes { get; init; }

    public bool AllowWrites { get; init; }

    public bool Clobber { get; init; }

    protected override PackagerArguments GetArguments()
    {
        var arguments = base.GetArguments();

        arguments.Action = CommandAction.Extract;

        return arguments;
    }

    protected override string? GetLogSourceFile(
        PackagerArguments arguments,
        SolutionPackager runner
        ) => arguments.PathToZipFile;
}