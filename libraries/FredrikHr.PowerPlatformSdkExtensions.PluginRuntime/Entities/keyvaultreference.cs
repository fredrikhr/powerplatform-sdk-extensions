using Microsoft.Xrm.Sdk.Query;

namespace FredrikHr.PowerPlatformSdkExtensions.PluginRuntime.Entities;

partial class KeyVaultReference
{
    public static ColumnSet ColumnSet { get; } = new(
        Fields.KeyVaultReferenceId,
        Fields.KeyType,
        Fields.KeyVaultUri,
        Fields.KeyName,
        Fields.ManagedIdentityId,
        Fields.statecode,
        Fields.statuscode,
        Fields.VersionNumber
        );
}