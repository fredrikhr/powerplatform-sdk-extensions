using Microsoft.Xrm.Sdk.Query;

namespace FredrikHr.PowerPlatformSdkExtensions.PluginRuntime.Entities;

partial class ApplicationUser
{
    public static ColumnSet ColumnSet { get; } = new([
        Fields.ApplicationUserId,
        Fields.ApplicationId,
        Fields.ApplicationName,
        Fields.ApplicationType
    ]);
}