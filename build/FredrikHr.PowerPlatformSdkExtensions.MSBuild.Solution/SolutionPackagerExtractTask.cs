using System.Diagnostics;

using Microsoft.Crm.Tools.SolutionPackager;

namespace FredrikHr.PowerPlatformSdkExtensions.MSBuild.Solution;

public sealed class SolutionPackagerExtractTask : SolutionPackagerTaskBase
{
    protected override PackagerArguments GetArguments()
    {
        var arguments = base.GetArguments();

        arguments.Action = CommandAction.Extract;

        return arguments;
    }

    protected override void LogErrorFromException(
        Exception exception,
        PackagerArguments arguments,
        SolutionPackager runner
        )
    {
        Log.LogErrorFromException(
            exception,
            showStackTrace: true,
            showDetail: true,
            file: arguments.PathToZipFile
        );
    }
}