using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace FredrikHr.PowerPlatformSdkExtensions.MSBuild.Solution;

public sealed class SolutionPackagerPackTask : MSBuildTask
{
    public override bool Execute()
    {
        Microsoft.Crm.Tools.SolutionPackager.PackagerArguments a = new();

        return false;
    }
}