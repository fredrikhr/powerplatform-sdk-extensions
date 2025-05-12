using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Organization;

namespace FredrikHr.PowerPlatformSdkExtensions.DataverseUtilityPlugins;

public class RetrieveInstanceInformationPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var trace = serviceProvider.Get<ITracingService>();
        trace.Trace("Starting execution of {0}.", GetType().Name);

        var context = serviceProvider.Get<IPluginExecutionContext2>();
        var outputs = context.OutputParameters;

        outputs[nameof(context.BusinessUnitId)] = context.BusinessUnitId;
        outputs[nameof(context.UserId)] = context.UserId;
        outputs["UserEntraObjectId"] = context.UserAzureActiveDirectoryObjectId;
        outputs["UserApplicationId"] = context.InitiatingUserApplicationId;
        outputs["PluginExecutionEntryPoint"] = context.OwningExtension;

        var dataverseSvc = serviceProvider.Get<IOrganizationServiceFactory>()?
            .CreateOrganizationService(null) ??
            serviceProvider.GetOrganizationService(context.UserId);
        RetrieveCurrentOrganizationRequest request = new()
        { AccessType = EndpointAccessType.Default };
        trace.Trace("Exceuting OrganizationService {0}.", request.GetType().Name);
        var response = (RetrieveCurrentOrganizationResponse)dataverseSvc.Execute(request);

        if (response.Detail is OrganizationDetail orgDetails)
        {
            trace.Trace("Extracting Organization Details into response property entity.");
            Entity orgDetailsEntity = new();

            orgDetailsEntity[nameof(orgDetails.OrganizationId)] = orgDetails.OrganizationId;
            orgDetailsEntity[nameof(orgDetails.FriendlyName)] = orgDetails.FriendlyName;
            orgDetailsEntity[nameof(orgDetails.OrganizationVersion)] = orgDetails.OrganizationVersion;
            orgDetailsEntity[nameof(orgDetails.EnvironmentId)] = orgDetails.EnvironmentId;
            orgDetailsEntity[nameof(orgDetails.DatacenterId)] = orgDetails.DatacenterId;
            orgDetailsEntity[nameof(orgDetails.Geo)] = orgDetails.Geo;
            orgDetailsEntity[nameof(orgDetails.TenantId)] = orgDetails.TenantId;
            orgDetailsEntity[nameof(orgDetails.UrlName)] = orgDetails.UrlName;
            orgDetailsEntity[nameof(orgDetails.UniqueName)] = orgDetails.UniqueName;
            orgDetailsEntity[nameof(orgDetails.State)] = orgDetails.State.ToString();
            orgDetailsEntity[nameof(orgDetails.SchemaType)] = orgDetails.SchemaType;
            orgDetailsEntity[nameof(orgDetails.OrganizationType)] = orgDetails.OrganizationType.ToString();

            outputs[$"{nameof(OrganizationDetail)}s"] = orgDetailsEntity;

            trace.Trace("Extracting Organization Endpoints information in separate response property.");
            Entity endpointsEntity = new();
            Uri? apiSvcEndpointUri = null;
            Uri? webAppEndpointUri = null;
            foreach (var endpointEntry in orgDetails.Endpoints ?? [])
            {
                endpointsEntity[endpointEntry.Key.ToString()] = endpointEntry.Value;
                switch (endpointEntry.Key)
                {
                    case EndpointType.WebApplication:
                        webAppEndpointUri ??= new(endpointEntry.Value);
                        break;
                    case EndpointType.OrganizationService:
                    case EndpointType.OrganizationDataService:
                        apiSvcEndpointUri ??= new(endpointEntry.Value);
                        break;
                }
            }
            if (apiSvcEndpointUri is not null)
            {
                trace.Trace("Determining Organization OData Endpoint URLs.");
                string odataBaseUrl = new Uri(
                    baseUri: apiSvcEndpointUri,
                    relativeUri: $"/api/data/v{Uri.EscapeUriString(orgDetails.OrganizationVersion)}"
                    ).ToString();
                endpointsEntity["ODataServiceBaseUrl"] = odataBaseUrl.ToString();
                endpointsEntity["ODataMetadataUrl"] = odataBaseUrl + "/$metadata";
            }
            if (webAppEndpointUri is not null)
            {
                endpointsEntity["TokenAudience"] = webAppEndpointUri.GetLeftPart(UriPartial.Authority);
            }
            outputs[nameof(orgDetails.Endpoints)] = endpointsEntity;
        }

        trace.Trace("Evaluating Environment information.");
        var envDetails = serviceProvider.Get<IEnvironmentService>();
        Entity envDetailsEntity = new();
        envDetailsEntity[nameof(envDetails.AzureAuthorityHost)] = envDetails.AzureAuthorityHost.ToString();
        envDetailsEntity[nameof(envDetails.Geo)] = envDetails.Geo;
        envDetailsEntity[nameof(envDetails.AzureRegionName)] = envDetails.AzureRegionName;
        outputs["EnvironmentDetails"] = envDetailsEntity;

        trace.Trace("Execution of {0} completed.", GetType().Name);
    }
}
