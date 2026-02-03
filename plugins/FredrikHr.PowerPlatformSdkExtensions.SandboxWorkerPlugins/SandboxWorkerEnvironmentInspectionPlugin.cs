using System.Collections;
using System.Runtime.InteropServices;

using Microsoft.CDSRuntime.SandboxCommon;
using Microsoft.CDSRuntime.SandboxWorker;

using static FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerPlugins.SandboxWorkerPluginHelpers;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerPlugins;

public class SandboxWorkerEnvironmentInspectionPlugin : IPlugin
{
    private static class OutputParameterNames
    {
        public const string CurrentDirectory = nameof(Environment.CurrentDirectory);
        public const string CommandLineArguments = nameof(CommandLineArguments);
        public const string EnvironmentVariables = nameof(EnvironmentVariables);
        public const string Is64BitOperatingSystem = nameof(Environment.Is64BitOperatingSystem);
        public const string Is64BitProcess = nameof(Environment.Is64BitProcess);
        public const string MachineName = nameof(Environment.MachineName);
        public const string OSVersion = nameof(Environment.OSVersion);
        public const string StackTrace = nameof(Environment.StackTrace);
        public const string SystemDirectory = nameof(Environment.SystemDirectory);
        public const string UserDomainName = nameof(Environment.UserDomainName);
        public const string UserName = nameof(Environment.UserName);
        public const string ClrVersion = nameof(ClrVersion);
        public const string ClrDescription = nameof(ClrDescription);
        public const string OSArchitecture = nameof(RuntimeInformation.OSArchitecture);
        public const string OSDescription = nameof(RuntimeInformation.OSDescription);
        public const string ProcessArchitecture = nameof(RuntimeInformation.ProcessArchitecture);
        public const string SharedVariables = nameof(IExecutionContext.SharedVariables);
        public const string ContainerContext = nameof(ContainerContext);
        public const string WorkerMetadata = nameof(WorkerMetadata);
        public const string ApplicationInsightsKey = nameof(ApplicationInsightsKey);
        public const string OrgIdFromWorkerMetadata = nameof(OrgIdFromWorkerMetadata);
        public const string SandboxWorkerMainFiles = nameof(SandboxWorkerMainFiles);
        public const string SandboxFiles = nameof(SandboxFiles);
        public const string ContainerFiles = nameof(ContainerFiles);
        public const string SSLCertFiles = nameof(SSLCertFiles);
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

        try { ExecuteCore(execCtx, trace); }
        catch (TypeInitializationException typeInitExcept)
        {
            logger.LogCritical(LogExceptionFormat, typeInitExcept);
            trace.Trace(TraceExceptionFormat, typeInitExcept);
            throw new InvalidPluginExecutionException(
                status: OperationStatus.Failed,
                message: typeInitExcept.Message,
                errorCode: typeInitExcept.HResult,
                httpStatus: PluginHttpStatusCode.InternalServerError
                );
        }
    }

    private static void ExecuteCore(IExecutionContext execCtx, ITracingService trace)
    {
        var paramsOut = execCtx.OutputParameters;
        var sharedVars = execCtx.SharedVariables;

        paramsOut[OutputParameterNames.CurrentDirectory] = Environment.CurrentDirectory;
        string[] allCommandLineArgs = Environment.GetCommandLineArgs();
        string[] appCommandLineArgs = [.. allCommandLineArgs.Skip(1)];
        paramsOut[OutputParameterNames.CommandLineArguments] = allCommandLineArgs;
        paramsOut[OutputParameterNames.Is64BitOperatingSystem] = Environment.Is64BitOperatingSystem;
        paramsOut[OutputParameterNames.Is64BitProcess] = Environment.Is64BitProcess;
        paramsOut[OutputParameterNames.MachineName] = Environment.MachineName;
        paramsOut[OutputParameterNames.OSVersion] = Environment.OSVersion.ToString();
        paramsOut[OutputParameterNames.StackTrace] = Environment.StackTrace;
        paramsOut[OutputParameterNames.SystemDirectory] = Environment.SystemDirectory;
        paramsOut[OutputParameterNames.UserDomainName] = Environment.UserDomainName;
        paramsOut[OutputParameterNames.UserName] = Environment.UserName;
        paramsOut[OutputParameterNames.ClrVersion] = Environment.Version.ToString();
        paramsOut[OutputParameterNames.ClrDescription] = RuntimeInformation.FrameworkDescription;
        paramsOut[OutputParameterNames.OSArchitecture] = RuntimeInformation.OSArchitecture.ToString();
        paramsOut[OutputParameterNames.OSDescription] = RuntimeInformation.OSDescription;
        paramsOut[OutputParameterNames.ProcessArchitecture] = RuntimeInformation.ProcessArchitecture.ToString();

        var environmentVariables = Environment.GetEnvironmentVariables();
        paramsOut[OutputParameterNames.EnvironmentVariables] =
            StringDictionaryToEntityCollection(environmentVariables);

        EntityCollection sharedVarsEntities = new([.. sharedVars
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp =>
            {
                Entity entity = new();
                entity[nameof(kvp.Key)] = kvp.Key;
                entity[nameof(kvp.Value)] = kvp.Value;
                return entity;
            })]);
        paramsOut[OutputParameterNames.SharedVariables] = sharedVarsEntities;

        AddContainerContextOutputParameter(paramsOut);
        AddWorkerMetadataOutputParameter(paramsOut);

        AddWorkerDirectoriesToOutputParameter(paramsOut, trace);
    }

    private static void AddContainerContextOutputParameter(ParameterCollection paramsOut)
    {
        var containerContext = SandboxWorkerMain.ContainerContext;
        Entity containerContextEntity = new();
        foreach (var containerContextProperty in IContainerContext.TypeReference.GetProperties())
        {
            containerContextEntity[containerContextProperty.Name] =
                containerContextProperty.GetValue(containerContext.Target);
        }
        /*
        var islandEnvCtx = containerContext.GetIslandEnvironmentContext(
            SandboxWorkerMain.ShimClient
            );
        Entity islandEnvCtxEntity = new();
        foreach (var islandEnvCtxProp in IslandEnvironmentContext.TypeReference.GetProperties())
        {
            islandEnvCtxEntity[islandEnvCtxProp.Name] =
                islandEnvCtxProp.GetValue(islandEnvCtx.Target);
        }
        containerContextEntity[IslandEnvironmentContext.TypeReference.Name] = islandEnvCtxEntity;
        */
        var allowedAppIds = containerContext.GetAllowedAppIds(
            SandboxWorkerMain.ShimClient
            );
        containerContextEntity["AllowedAppIds"] = allowedAppIds;
        paramsOut[OutputParameterNames.ContainerContext] = containerContextEntity;
    }

    private static void AddWorkerMetadataOutputParameter(ParameterCollection paramsOut)
    {
        var shimClient = SandboxWorkerMain.ShimClient;
        var metadatResp = shimClient.GetWorkerAssignedMetadata(
            SandboxWorkerMain.WorkerProcessGuid
        );
        string workerMetadata = metadatResp.WorkerMetadata;
        paramsOut[OutputParameterNames.WorkerMetadata] = workerMetadata;
        paramsOut[OutputParameterNames.ApplicationInsightsKey] = metadatResp.ApplicationInsightsKey;
        paramsOut[OutputParameterNames.OrgIdFromWorkerMetadata] = SandboxUtility.ParseOrgIdFromWorkerMetadata(workerMetadata);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = nameof(ITracingService))]
    private static void AddWorkerDirectoriesToOutputParameter(ParameterCollection paramsOut, ITracingService trace)
    {
        var contCtx = SandboxWorkerMain.ContainerContext;
        var containerReadOnlyPath = contCtx.ContainerReadWriteFolderPath;
        var sslCertPath = contCtx.SSLCertPath;

        try
        {
            FileInfo mainLocationInfo = new(SandboxWorkerMain.TypeReference.Assembly.Location);
            if (mainLocationInfo.Directory?.FullName is string mainLocationDir)
            {
                AddDirectoryMetadataToOutputParameter(
                    OutputParameterNames.SandboxWorkerMainFiles,
                    mainLocationDir,
                    paramsOut
                    );
            }
        }
        catch (Exception except)
        {
            trace.Trace(TraceExceptionFormat, except);
        }

        AddDirectoryMetadataToOutputParameter(OutputParameterNames.SandboxFiles, SandboxUtility.SandboxFilesPath(), paramsOut);
        AddDirectoryMetadataToOutputParameter(OutputParameterNames.ContainerFiles, containerReadOnlyPath, paramsOut);
        AddDirectoryMetadataToOutputParameter(OutputParameterNames.SSLCertFiles, sslCertPath, paramsOut);
    }

    private static void AddDirectoryMetadataToOutputParameter(string name, string directoryPath, ParameterCollection paramsOut)
    {
        Entity entity = new();
        bool exists = Directory.Exists(directoryPath);
        entity[nameof(Directory.Exists)] = exists;
        if (exists)
        {
            entity[nameof(Path)] = Path.GetFullPath(directoryPath);
            string[] files = Directory.GetFiles(directoryPath);
            string[] directories = Directory.GetDirectories(directoryPath);
            List<string> additionalFiles = [];
            foreach (var filePath in files)
            {
                string addFileName = $"{Path.GetFileNameWithoutExtension(filePath)}.resources.dll";
                foreach (var subDirPath in directories)
                {
                    var addFilePath = Path.Combine(subDirPath, addFileName);
                    if (File.Exists(addFilePath))
                        additionalFiles.Add(addFilePath);
                }
            }
            entity["Files"] = files.Concat(additionalFiles).ToArray();
        }
        else
            entity[nameof(Path)] = directoryPath;
        paramsOut[name] = entity;
    }

    private static EntityCollection StringDictionaryToEntityCollection(IDictionary dictionary)
    {
        var entityList = dictionary.Cast<DictionaryEntry>()
            .OrderBy(e => (string)e.Key, StringComparer.OrdinalIgnoreCase)
            .Select(e =>
            {
                Entity entity = new();
                entity["Name"] = e.Key;
                entity[nameof(e.Value)] = e.Value;
                return entity;
            })
            .ToList();
        return new(entityList);
    }
}