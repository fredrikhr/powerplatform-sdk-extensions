using System.Diagnostics;

using Microsoft.Build.Framework;
using Microsoft.Crm.Tools.SolutionPackager;

using MSBuildTask = Microsoft.Build.Utilities.Task;
using SolutionPackagerLogger = Microsoft.Crm.Tools.Logger;

namespace FredrikHr.PowerPlatformSdkExtensions.MSBuild.Solution;

public abstract class SolutionPackagerTaskBase : MSBuildTask
{
    [Required]
    public string? PackageType { get; init; }

    [Required]
    public required ITaskItem PathToZipFile { get; init; }

    [Required]
    public required ITaskItem SolutionRootDirectory { get; init; }

    public ITaskItem? MappingFile { get; init; }

    public bool Localize { get; init; }

    public bool UseLcid { get; init; }

    public string? LocaleTemplate { get; init; }

    public ITaskItem? LogFile { get; init; }

    public string? ErrorLevel { get; init; }

    public bool NoLogo { get; init; }

    public bool DisableTelemetry { get; init; }

    protected virtual PackagerArguments GetArguments()
    {
        return new()
        {
            PackageType = ValidateEnum(PackageType, default(SolutionPackageType)),

            PathToZipFile = PathToZipFile?.GetMetadata("FullPath"),
            Folder = SolutionRootDirectory?.GetMetadata("FullPath"),
            MappingFile = MappingFile?.GetMetadata("FullPath"),

            Localize = Localize,
            UseLcid = UseLcid,

            LogFile = LogFile?.GetMetadata("FullPath"),
            ErrorLevel = ValidateEnum(ErrorLevel, TraceLevel.Info),
            NoLogo = NoLogo,
            DisableTelemetry = DisableTelemetry,
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
            if (!string.IsNullOrWhiteSpace(arguments.LogFile))
                RemoveTraceFileListener(arguments);
            var logSourceFile = GetLogSourceFile(arguments, runner);
            foreach (var warning in SolutionPackagerLogger.AllWarnings)
            {
                Log.LogWarning(
                    subcategory: nameof(SolutionPackager),
                    warningCode: null,
                    helpKeyword: null,
                    message: warning,
                    file: logSourceFile,
                    lineNumber: 0, endLineNumber: 0,
                    columnNumber: 0, endColumnNumber: 0
                );
            }
            foreach (var error in SolutionPackagerLogger.AllErrors)
            {
                Log.LogError(
                    subcategory: nameof(SolutionPackager),
                    errorCode: null,
                    helpKeyword: null,
                    message: error,
                    file: logSourceFile,
                    lineNumber: 0, endLineNumber: 0,
                    columnNumber: 0, endColumnNumber: 0
                );
            }
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

    private void LogErrorFromException(
        Exception exception,
        PackagerArguments arguments,
        SolutionPackager runner
        )
    {
        var logSourceFile = GetLogSourceFile(arguments, runner);
        Log.LogErrorFromException(
            exception,
            showStackTrace: true,
            showDetail: true,
            file: logSourceFile
        );
    }

    protected abstract string? GetLogSourceFile(
        PackagerArguments arguments,
        SolutionPackager runner
    );

    private static void RemoveTraceFileListener(
        PackagerArguments arguments
        )
    {
        string logFilePath = Path.GetFullPath(arguments.LogFile);
        foreach (var fileTracer in Trace.Listeners.OfType<TextWriterTraceListener>().ToList())
        {
            if (fileTracer.Writer is not StreamWriter { BaseStream: FileStream traceFileStream })
                continue;
            if (!logFilePath.Equals(traceFileStream.Name, StringComparison.OrdinalIgnoreCase))
                continue;
            Trace.Listeners.Remove(fileTracer);
            fileTracer.Dispose();
        }
    }
}