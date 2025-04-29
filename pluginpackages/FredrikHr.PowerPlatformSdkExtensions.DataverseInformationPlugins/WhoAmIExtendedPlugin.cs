using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Organization;

namespace FredrikHr.PowerPlatformSdkExtensions.WhoAmIExtended.Plugins;

public class WhoAmIExtendedPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext2>();
        var outputs = context.OutputParameters;

        outputs[nameof(context.BusinessUnitId)] = context.BusinessUnitId;
        outputs[nameof(context.UserId)] = context.UserId;
        outputs["UserEntraObjectId"] = context.UserAzureActiveDirectoryObjectId;
        outputs["UserApplicationId"] = context.InitiatingUserApplicationId;

        var dataverseSvc = serviceProvider.Get<IOrganizationService>();
        RetrieveCurrentOrganizationRequest request = new()
        { AccessType = EndpointAccessType.Default };
        var response = (RetrieveCurrentOrganizationResponse)dataverseSvc.Execute(request);

        OrganizationDetail orgDetails = response.Detail;
        outputs[$"{nameof(OrganizationDetail)}s"] = orgDetails;

        Entity endpointsEntity = new();
        endpointsEntity[nameof(EndpointType.WebApplication)] = orgDetails.Endpoints[EndpointType.WebApplication];
        endpointsEntity[nameof(EndpointType.OrganizationService)] = orgDetails.Endpoints[EndpointType.OrganizationService];
        endpointsEntity[nameof(EndpointType.OrganizationDataService)] = orgDetails.Endpoints[EndpointType.OrganizationDataService];
        string odataBaseUrl = new Uri(
            baseUri: new Uri(orgDetails.Endpoints[EndpointType.WebApplication]),
            relativeUri: $"/api/data/v${Uri.EscapeUriString(orgDetails.OrganizationVersion)}"
            ).ToString();
        endpointsEntity["ODataServiceUrl"] = odataBaseUrl;
        endpointsEntity["ODataMetadataUrl"] = odataBaseUrl + "/$metadata";
        outputs[nameof(orgDetails.Endpoints)] = endpointsEntity;

        var envDetails = serviceProvider.Get<IEnvironmentService>();
        Entity envDetailsEntity = new();
        envDetailsEntity[nameof(envDetails.AzureAuthorityHost)] = envDetails.AzureAuthorityHost;
        envDetailsEntity[nameof(envDetails.Geo)] = envDetails.Geo;
        envDetailsEntity[nameof(envDetails.AzureRegionName)] = envDetails.AzureRegionName;
        outputs["EnvironmentDetails"] = endpointsEntity;
    }
}