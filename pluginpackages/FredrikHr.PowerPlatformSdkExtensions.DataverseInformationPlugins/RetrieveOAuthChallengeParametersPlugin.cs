namespace FredrikHr.PowerPlatformSdkExtensions.DataverseInformationPlugins;

public class RetrieveOAuthChallengeParametersPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var trace = serviceProvider.Get<ITracingService>();
        trace.Trace("Starting execution of {0}.", GetType().Name);

        var context = serviceProvider.Get<IPluginExecutionContext>();
        var outputs = context.OutputParameters;

        const string challengeUrlName = "ChallengeUrl";
        string? challengeUrl = context.InputParameterOrDefault<string>(challengeUrlName);
        Uri challengeUri;
        try
        {
            challengeUri = new(challengeUrl);
        }
        catch (ArgumentNullException)
        {
            throw new InvalidPluginExecutionException(
                message: $"Input parameter {challengeUrlName} must not be null.",
                httpStatus: PluginHttpStatusCode.BadRequest
                );
        }
        catch (ArgumentException argExcept)
        {
            throw new InvalidPluginExecutionException(
                message: argExcept.Message,
                httpStatus: PluginHttpStatusCode.BadRequest
                );
        }

        var challengeAnalyzer = serviceProvider.Get<IAssemblyAuthenticationContext2>();
        bool success;
        string? authority, resourceId;
        try
        {
            success = challengeAnalyzer.ResolveAuthorityAndResourceFromChallengeUri(
                challengeUri,
                out authority,
                out resourceId
                );
        }
        catch (Exception except)
        {
            throw new InvalidPluginExecutionException(
                message: except.Message,
                exception: except
                );
        }

        outputs["Success"] = success;
        outputs["Authority"] = authority;
        outputs["ResourceId"] = resourceId;

        trace.Trace("Execution of {0} completed.", GetType().Name);
    }
}