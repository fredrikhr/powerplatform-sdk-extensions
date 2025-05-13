using System.Collections;
using System.Reflection;

namespace FredrikHr.PowerPlatformSdkExtensions.DataverseTokenAcquisitionPlugins;

public class PowerAutomateOnBehalfOfTokenBundleAcquisitionPlugin : IPlugin
{
    private const string SandboxWorkerAssemblyFqn =
        "Microsoft.CDSRuntime.SandboxWorker, PublicKeyToken=31bf3856ad364e35";
    private const string SandboxGrpcContractsAssemblyFqn =
        "Microsoft.CDSRuntime.SandboxGrpcContracts, PublicKeyToken=31bf3856ad364e35";
    private const string CallbackServiceFqn =
        $"Microsoft.CDSRuntime.SandboxWorker.ISandboxCallbackService, {SandboxWorkerAssemblyFqn}";
    private const string PowerFxConnectorRequFqn =
        $"CDSRunTime.Sandbox.Contract.PowerFxConnectorServiceRequest, {SandboxGrpcContractsAssemblyFqn}";
    private const string PowerFxConnectorRequTypeFqn =
        $"CDSRunTime.Sandbox.Contract.PowerFxConnectorRequestType, {SandboxGrpcContractsAssemblyFqn}";

    private static Type? s_callbackServiceType = Type.GetType(
        CallbackServiceFqn,
        throwOnError: false
        );
    private static Type? s_powerFxConnectorRequType = Type.GetType(
        PowerFxConnectorRequFqn,
        throwOnError: false
        );
    private static Type? s_powerFxConnectorRequTypeEnum = Type.GetType(
        PowerFxConnectorRequTypeFqn,
        throwOnError: false
        );

    private static readonly MethodInfo GetExecuteCallbackMethod =
        typeof(PowerAutomateOnBehalfOfTokenBundleAcquisitionPlugin)
        .GetMethod(
            nameof(GetExecuteCallback),
            BindingFlags.Static | BindingFlags.NonPublic
            )
        .GetGenericMethodDefinition();

    public void Execute(IServiceProvider serviceProvider)
    {
        var trace = serviceProvider.Get<ITracingService>();

        MethodInfo? executeCallbackMethod = null;
        MethodInfo? getCallbackFunctionMethod = null;
        try
        {
            s_callbackServiceType ??= Type.GetType(CallbackServiceFqn, throwOnError: true);
            s_powerFxConnectorRequType ??= Type.GetType(PowerFxConnectorRequFqn, throwOnError: true);
            s_powerFxConnectorRequTypeEnum ??= Type.GetType(PowerFxConnectorRequTypeFqn, throwOnError: true);

            var executeCallbackSignatures = s_callbackServiceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(mi => mi.Name.Equals("ExecuteCallBack", StringComparison.OrdinalIgnoreCase))
                ;
            foreach (var executeCallbackSignature in executeCallbackSignatures)
            {
                var executeCallbackParams = executeCallbackSignature.GetParameters();

                if (executeCallbackParams.Length != 1) continue;

                var executeCallbackParamType = executeCallbackParams[0].ParameterType;
                if (executeCallbackParamType.GetGenericTypeDefinition() != typeof(Func<,>)) continue;

                executeCallbackMethod = executeCallbackSignature;
                var executeCallbackParamGenericTypeArgs = executeCallbackParamType.GenericTypeArguments;
                getCallbackFunctionMethod = GetExecuteCallbackMethod
                    .MakeGenericMethod([.. executeCallbackParamGenericTypeArgs, s_powerFxConnectorRequType]);
            }

            if (executeCallbackMethod is null)
            {
                throw new InvalidPluginExecutionException(
                    message: $"Unable to determine appropriate ExecuteCallBack method on interface type {s_callbackServiceType}",
                    httpStatus: PluginHttpStatusCode.InternalServerError
                    );
            }
            if (getCallbackFunctionMethod is null)
            {
                throw new InvalidPluginExecutionException(
                    message: $"Unable to construct generic method implementation matching argument for signature: {executeCallbackMethod}",
                    httpStatus: PluginHttpStatusCode.InternalServerError
                    );
            }
        }
        catch (Exception typeLoadExcept)
        when (typeLoadExcept is not InvalidPluginExecutionException)
        {
            throw new InvalidPluginExecutionException(
                message: typeLoadExcept.ToString(),
                httpStatus: PluginHttpStatusCode.InternalServerError
                );
        }

        var context = serviceProvider.Get<IPluginExecutionContext6>();
        var outputs = context.OutputParameters;

        var oboSvc = serviceProvider.Get<IOnBehalfOfTokenService>();
        var oboType = oboSvc.GetType();
        var callbackServiceField = oboType.GetFields(
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public
            ).FirstOrDefault(
                field => s_callbackServiceType.IsAssignableFrom(field.FieldType)
            ) ?? throw new InvalidPluginExecutionException(
                $"Unable to find an instance field on {nameof(IOnBehalfOfTokenService)} service instance that is assignable to {s_callbackServiceType}",
                httpStatus: PluginHttpStatusCode.InternalServerError
                );
        object callbackServiceInstance = callbackServiceField.GetValue(oboSvc);
        object pwrSvcRequ = Activator.CreateInstance(s_powerFxConnectorRequType);
        s_powerFxConnectorRequType.InvokeMember(
            "PowerFxConnectorRequestType",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
            Type.DefaultBinder,
            target: pwrSvcRequ,
            args: new object[] { Enum.Parse(s_powerFxConnectorRequTypeEnum, "OboRequest") },
            culture: System.Globalization.CultureInfo.InvariantCulture
            );
        s_powerFxConnectorRequType.InvokeMember(
            "Method",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
            Type.DefaultBinder,
            target: pwrSvcRequ,
            args: ["POST"],
            culture: System.Globalization.CultureInfo.InvariantCulture
            );
        s_powerFxConnectorRequType.InvokeMember(
            "Url",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
            Type.DefaultBinder,
            target: pwrSvcRequ,
            args: [$"https://api.flow.microsoft.com/providers/microsoft.Flow/environments/{Uri.EscapeUriString(context.EnvironmentId)}/users/me/onBehalfOfTokenBundle?api-version={Uri.EscapeDataString("2025-05-01")}"],
            culture: System.Globalization.CultureInfo.InvariantCulture
            );
        dynamic pwrSvcResp;
        try
        {
            dynamic callbackSvcResp = executeCallbackMethod.Invoke(
                callbackServiceInstance,
                [getCallbackFunctionMethod.Invoke(null, [pwrSvcRequ])]
                );
            pwrSvcResp = callbackSvcResp.PowerFxConnectorServiceResponse;
        }
        catch (TargetInvocationException reflectionInvokeExcept)
        {
            Exception oboExcept = reflectionInvokeExcept.InnerException;
            trace.Trace("{0}", oboExcept);
            throw new InvalidPluginExecutionException(
                message: $"Failed to send HTTP request using On-Behalf-Of token. {oboExcept.GetType()}: {oboExcept.Message}",
                httpStatus: PluginHttpStatusCode.InternalServerError,
                exceptionDetails: oboExcept.Data.Cast<DictionaryEntry>().ToDictionary(e => e.Key?.ToString() ?? "", e => e.Value?.ToString())
                );
        }
        outputs["StatusCode"] = pwrSvcResp.StatusCode;
        Entity httpHeaders = new();
        foreach (var headerEntry in (IReadOnlyDictionary<string, string>)pwrSvcResp.Headers)
        {
            httpHeaders[headerEntry.Key] = headerEntry.Value;
        }
        outputs["HttpHeaders"] = httpHeaders;
        Entity bodyHeaders = new();
        foreach (var headerEntry in (IReadOnlyDictionary<string, string>)pwrSvcResp.ContentHeaders)
        {
            bodyHeaders[headerEntry.Key] = headerEntry.Value;
        }
        outputs["ContentHeaders"] = bodyHeaders;
        outputs["ContentBody"] = pwrSvcResp.Body;
    }

    private static Func<TCallback, TOperationType> GetExecuteCallback<TCallback, TOperationType, TRequest>(
        TRequest powerFxServiceRequest
        )
    {
        TOperationType returnValue = (TOperationType)Enum.Parse(typeof(TOperationType), "PowerFxConnectorServiceResponse");
        return CallbackLocalFunction;

        TOperationType CallbackLocalFunction(TCallback request)
        {
            dynamic requObj = request!;
            requObj.PowerFxConnectorServiceRequest = powerFxServiceRequest;
            return returnValue;
        }
    }
}