using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

using FredrikHr.PowerPlatformSdkExtensions.PluginRuntime.Entities;

namespace FredrikHr.PowerPlatformSdkExtensions.PluginRuntime;

public class PluginExecutionInformation
{
    public const string PrivilegeNameImpersonation = "prvActOnBehalfOfAnotherUser";
    private static readonly string[] PrivilegeNamesImpersonation = [PrivilegeNameImpersonation];

    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<SystemUser?> _applicationSystemUser;
    private readonly Lazy<ManagedIdentity?> _pluginManagedIdentity;
    private readonly Lazy<SystemUser?> _pluginManagedIdentitySystemUser;
    private readonly Lazy<bool> _isUserPluginManagedIdentitySystemUser;
    private readonly Lazy<bool> _userHasImpersonationPrivilege;

    public PluginExecutionInformation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _pluginManagedIdentity = new(RetrievePluginManagedIdentity);
        _pluginManagedIdentitySystemUser = new(RetrievePluginManagedIdentitySystemUser);
        _isUserPluginManagedIdentitySystemUser = new(EvaluateIsUserSameAsPlugin);
        _applicationSystemUser = new(RetrieveApplicationSystemUser);
        _userHasImpersonationPrivilege = new(EvaluateUserHasImpersonationPrivilege);
    }

    public ITracingService TracingService
        => field ??= _serviceProvider.Get<ITracingService>();

    public IOrganizationServiceFactory DataverseClientFactory
        => field ??= _serviceProvider.Get<IOrganizationServiceFactory>();

    public IOrganizationService SystemDataverseClient
        => field ??= DataverseClientFactory.CreateOrganizationService(
            userId: null
            );

    public IOrganizationService DataverseClient
        => field ??= DataverseClientFactory.CreateOrganizationService(
            _serviceProvider.Get<IPluginExecutionContext>()?.UserId
            );

    public SystemUser? ApplicationSystemUser
        => _applicationSystemUser.Value;

    public ManagedIdentity? PluginManagedIdentity
        => _pluginManagedIdentity.Value;

    public SystemUser? PluginManagedIdentitySystemUser
        => _pluginManagedIdentitySystemUser.Value;

    public bool IsUserPluginManagedIdentitySystemUser
        => _isUserPluginManagedIdentitySystemUser.Value;

    public bool UserHasImpersonationPrivilege
        => _userHasImpersonationPrivilege.Value;

    private ManagedIdentity? RetrievePluginManagedIdentity()
    {
        var context = _serviceProvider.Get<IPluginExecutionContext>();
        IOrganizationService client = SystemDataverseClient;
        if (context.OwningExtension?.Id is not Guid sdkStepId) return null;
        SdkMessageProcessingStep? sdkStepEntity = client.Retrieve(
            context.OwningExtension?.LogicalName ??
            SdkMessageProcessingStep.EntityLogicalName,
            sdkStepId,
            SdkMessageProcessingStep.ColumnSet
            ) switch
        {
            SdkMessageProcessingStep s => s,
            Entity e => e.ToEntity<SdkMessageProcessingStep>(),
            _ => null,
        };
        EntityReference? pluginTypeRef = sdkStepEntity?.EventHandler
#pragma warning disable CS0612 // Type or member is obsolete
            ?? sdkStepEntity?.PluginTypeId
#pragma warning restore CS0612 // Type or member is obsolete
            ;
        if (pluginTypeRef is null ||
            !PluginType.EntityLogicalName.Equals(pluginTypeRef.LogicalName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        PluginType? pluginTypeEntity = client.Retrieve(
            PluginType.EntityLogicalName,
            pluginTypeRef.Id,
            PluginType.ColumnSet
            ) switch
        {
            PluginType pt => pt,
            Entity e => e.ToEntity<PluginType>(),
            _ => null,
        };
        if (pluginTypeEntity?.PluginAssemblyId is not EntityReference pluginAssemblyRef)
        { return null; }
        PluginAssembly? pluginAssemblyEntity = client.Retrieve(
            pluginAssemblyRef.LogicalName ?? PluginAssembly.EntityLogicalName,
            pluginAssemblyRef.Id,
            PluginAssembly.ColumnSet
            ) switch
        {
            PluginAssembly pa => pa,
            Entity e => e.ToEntity<PluginAssembly>(),
            _ => null,
        };
        if (pluginAssemblyEntity is null) return null;
        if (pluginAssemblyEntity.ManagedIdentityId is null &&
            pluginAssemblyEntity.PackageId is EntityReference packageRef)
        {
            PluginPackage? pluginPackageEntity = client.Retrieve(
                packageRef.LogicalName ?? PluginPackage.EntityLogicalName,
                packageRef.Id,
                PluginPackage.ColumnSet
                ) switch
            {
                PluginPackage pp => pp,
                Entity e => e.ToEntity<PluginPackage>(),
                _ => null,
            };
            pluginAssemblyEntity.ManagedIdentityId =
                pluginPackageEntity?.managedidentityid;
        }
        if (pluginAssemblyEntity.ManagedIdentityId is not EntityReference managedIdentityRef)
        {
            return null;
        }
        ManagedIdentity? managedIdentityEntity = client.Retrieve(
            managedIdentityRef.LogicalName ?? ManagedIdentity.EntityLogicalName,
            managedIdentityRef.Id,
            ManagedIdentity.ColumnSet
            ) switch
        {
            ManagedIdentity mi => mi,
            Entity e => e.ToEntity<ManagedIdentity>(),
            _ => null,
        };
        return managedIdentityEntity;
    }

    private SystemUser? RetrievePluginManagedIdentitySystemUser()
    {
        if (PluginManagedIdentity is not { ApplicationId: Guid appId }) return null;
        IOrganizationService client = SystemDataverseClient;
        QueryExpression appUserQuery = new(SystemUser.EntityLogicalName)
        {
            TopCount = 2,
            ColumnSet = SystemUser.ApplicationUserColumnSet,
            Criteria =
            {
                Conditions =
                {
                    new(SystemUser.Fields.ApplicationId, ConditionOperator.Equal, appId),
                },
            },
        };
        EntityCollection appUserResults = client.RetrieveMultiple(appUserQuery);
        return appUserResults.TotalRecordCount != 0
            ? appUserResults.Entities.Single() switch
            {
                SystemUser su => su,
                Entity e => e.ToEntity<SystemUser>(),
                _ => null,
            }
            : null;
    }

    private SystemUser? RetrieveApplicationSystemUser()
    {
        var context = _serviceProvider.Get<IPluginExecutionContext7>();
        return context.IsApplicationUser
            ? SystemDataverseClient.Retrieve(
                SystemUser.EntityLogicalName,
                context.UserId,
                SystemUser.ApplicationUserColumnSet
                ) switch
            {
                SystemUser su => su,
                Entity e => e.ToEntity<SystemUser>(),
                _ => null,
            }
            : null;
    }

    private bool EvaluateIsUserSameAsPlugin()
    {
        var context = _serviceProvider.Get<IPluginExecutionContext7>();
        return context.IsApplicationUser &&
            context.UserId != Guid.Empty &&
            context.UserId == PluginManagedIdentitySystemUser?.SystemUserId;
    }

    private bool EvaluateUserHasImpersonationPrivilege()
    {
        var context = _serviceProvider.Get<IPluginExecutionContext>();
        RetrieveUserSetOfPrivilegesByNamesRequest privRequ = new()
        {
            UserId = context.UserId,
            PrivilegeNames = PrivilegeNamesImpersonation,
        };
        return DataverseClient.Execute(privRequ)
            is RetrieveUserSetOfPrivilegesByNamesResponse
        { RolePrivileges.Length: > 0 };
    }
}
