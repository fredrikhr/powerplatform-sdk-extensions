using Microsoft.Xrm.Sdk.Query;

namespace FredrikHr.PowerPlatformSdkExtensions.PluginRuntime.Entities;

partial class SystemUser
{
    public static ColumnSet ApplicationUserColumnSet { get; } = new([
        Fields.SystemUserId,
        Fields.ApplicationId,
        Fields.ApplicationIdUri,
        Fields.FullName,
        Fields.AzureActiveDirectoryObjectId,
        Fields.IsDisabled,
    ]);
}