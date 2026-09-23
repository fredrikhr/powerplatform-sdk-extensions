using Microsoft.Xrm.Sdk.Query;

namespace FredrikHr.PowerPlatformSdkExtensions.PluginRuntime.Entities;

partial class PluginPackage
{
    public static ColumnSet ColumnSet { get; } = new([
        Fields.PluginPackageId,
        Fields.managedidentityid,
    ]);
}