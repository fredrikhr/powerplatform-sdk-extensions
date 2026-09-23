using Microsoft.Xrm.Sdk.Query;

namespace FredrikHr.PowerPlatformSdkExtensions.PluginRuntime.Entities;

partial class PluginType
{
    public static ColumnSet ColumnSet { get; } = new([
        Fields.PluginTypeId,
        Fields.PluginAssemblyId,
    ]);
}