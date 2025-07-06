using static FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerPlugins.SandboxWorkerPluginHelpers;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerPlugins;

public class SandboxWorkerFileProviderPlugin : IPlugin
{
    private static class InputParameterNames
    {
        public const string FilePath = nameof(FilePath);
    }

    private static class OutputParameterNames
    {
        public const string ContentBase64 = nameof(ContentBase64);
        public const string FileInfo = nameof(FileInfo);
        public const string Uri = nameof(Uri);
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null) return;
        var execCtx = serviceProvider.Get<IExecutionContext>() ??
            throw new InvalidPluginExecutionException(
                OperationStatus.Failed,
                $"${nameof(IExecutionContext)} is null."
                );

        var trace = serviceProvider.Get<ITracingService>();
        var logger = serviceProvider.Get<ILogger>();
        var paramsIn = execCtx.InputParameters;
        var paramsOut = execCtx.OutputParameters;
        void ExceptionLogger(Exception exception, LogLevel logLevel)
        {
            logger?.Log(logLevel, exception, LogExceptionFormat, exception);
            trace?.Trace(TraceExceptionFormat, exception);
        }

        if (!paramsIn.TryGetValue(InputParameterNames.FilePath, out string filePath))
            throw new InvalidPluginExecutionException(
                $"Missing input parameter: {InputParameterNames.FilePath}",
                PluginHttpStatusCode.BadRequest
                );

        byte[] fileBytes = ReadFileBytes(filePath, ExceptionLogger);
        paramsOut[OutputParameterNames.ContentBase64] = Convert.ToBase64String(fileBytes);
        FileInfo fileInfo = new(filePath);
        Entity fileInfoEntity = new();
        fileInfoEntity[nameof(fileInfo.Exists)] = fileInfo.Exists;
        fileInfoEntity[nameof(fileInfo.Name)] = fileInfo.Name;
        fileInfoEntity[nameof(fileInfo.Extension)] = fileInfo.Extension;
        fileInfoEntity[nameof(fileInfo.Length)] = fileInfo.Length;
        fileInfoEntity[nameof(fileInfo.FullName)] = fileInfo.FullName;
        fileInfoEntity[nameof(fileInfo.CreationTime)] = fileInfo.CreationTimeUtc;
        fileInfoEntity[nameof(fileInfo.Attributes)] = fileInfo.Attributes.ToString();
        fileInfoEntity[nameof(fileInfo.IsReadOnly)] = fileInfo.IsReadOnly;
        fileInfoEntity[nameof(fileInfo.LastAccessTime)] = fileInfo.LastAccessTimeUtc;
        fileInfoEntity[nameof(fileInfo.LastWriteTime)] = fileInfo.LastWriteTimeUtc;
        paramsOut[OutputParameterNames.FileInfo] = fileInfoEntity;

        if (Uri.TryCreate(fileInfo.FullName, UriKind.Absolute, out Uri fileUri))
        {
            Entity fileUriEntity = new();
            fileUriEntity[nameof(fileUri.AbsolutePath)] = fileUri.AbsolutePath;
            fileUriEntity[nameof(fileUri.Segments)] = fileUri.Segments;
            paramsOut[OutputParameterNames.Uri] = fileUriEntity;
        }
    }

    private static byte[] ReadFileBytes(string filePath, Action<Exception, LogLevel> exceptionLogger)
    {
        try { return File.ReadAllBytes(filePath); }
        catch (Exception exception)
        {
            exceptionLogger(exception, LogLevel.Critical);
            PluginHttpStatusCode statusCode = exception switch
            {
                UnauthorizedAccessException => PluginHttpStatusCode.Unauthorized,
                System.Security.SecurityException => PluginHttpStatusCode.Forbidden,
                FileNotFoundException => PluginHttpStatusCode.NotFound,
                DirectoryNotFoundException => PluginHttpStatusCode.NotFound,
                IOException => PluginHttpStatusCode.InternalServerError,
                InvalidOperationException => PluginHttpStatusCode.InternalServerError,
                _ => PluginHttpStatusCode.BadRequest,
            };
            throw new InvalidPluginExecutionException(
                message: exception.ToString(),
                status: OperationStatus.Failed,
                httpStatus: statusCode,
                errorCode: exception.HResult
                );
        }
    }
}