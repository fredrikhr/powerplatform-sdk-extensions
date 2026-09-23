using Microsoft.Xrm.Sdk.Query;

namespace FredrikHr.PowerPlatformSdkExtensions.PluginRuntime.Entities;

partial class SdkMessageProcessingStep
{
    public static ColumnSet ColumnSet { get; } = new([
        Fields.SdkMessageProcessingStepId,
        Fields.PluginTypeId,
        Fields.EventHandler
    ]);
}