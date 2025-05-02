using System.Diagnostics;

using Microsoft.Crm.Tools.SolutionPackager;

using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace FredrikHr.PowerPlatformSdkExtensions.MSBuild.Solution;

public abstract class SolutionPackagerTaskBase : MSBuildTask
{
    public string? ErrorLevel { get; init; }

    protected virtual PackagerArguments GetArguments()
    {
        return new()
        {
            ErrorLevel = ValidateEnum(ErrorLevel, TraceLevel.Info),
        };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031: Do not catch general exception types",
        Justification = nameof(MSBuildTask)
    )]
    public override sealed bool Execute()
    {
        var arguments = GetArguments();
        SolutionPackager runner = new(arguments);
        try
        {
            runner.Run();
            return true;
        }
        catch (Exception except)
        {
            LogErrorFromException(except, arguments, runner);
            return false;
        }
    }

    protected TEnum ValidateEnum<TEnum>(
        string? value,
        TEnum defaultValue
        )
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        try
        {
            #if NET
            return Enum.Parse<TEnum>(value, ignoreCase: true);
            #else
            return (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase: true);
            #endif
        }
        catch (ArgumentException argExcept)
        {
            Log.LogErrorFromException(argExcept);
            throw;
        }
    }

    protected abstract void LogErrorFromException(
        Exception exception,
        PackagerArguments arguments,
        SolutionPackager runner
        );
}