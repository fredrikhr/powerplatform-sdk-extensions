using Microsoft.Xrm.Sdk.Query;

namespace FredrikHr.PowerPlatformSdkExtensions.PluginRuntime.Entities;

partial class ManagedIdentity
{
    public static ColumnSet ColumnSet { get; } = new(allColumns: true);
}