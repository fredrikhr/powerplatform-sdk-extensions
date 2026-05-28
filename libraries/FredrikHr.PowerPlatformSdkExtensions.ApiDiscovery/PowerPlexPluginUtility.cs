namespace FredrikHr.PowerPlatformSdkExtensions.ApiDiscovery;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1055: URI-like return values should not be strings",
    Justification = AssemblyQualifiedName
    )]
public static class PowerPlexPluginUtility
{
    private const string AssemblyQualifiedName =
        "PowerPlexPlugin.Utility, Microsoft.PowerFx.PlexPlugins, PublicKeyToken=31bf3856ad364e35";

    private static Type TypeReference
        => field ??= Type.GetType(AssemblyQualifiedName, throwOnError: true);

    private static Func<string, string, string> GetPowerPlatformBaseUriFunc
        => field ??= (Func<string, string, string>)TypeReference.GetMethod(
            nameof(GetPowerPlatformBaseUri),
            BindingFlags.Static | BindingFlags.Public,
            Type.DefaultBinder,
            types: [typeof(string), typeof(string)],
            modifiers: default
            ).CreateDelegate(typeof(Func<string, string, string>));

    private static Func<string, string> GetPowerPlatformTokenAudienceUrlFunc
        => field ??= (Func<string, string>)TypeReference.GetMethod(
            nameof(GetPowerPlatformTokenAudienceUrl),
            BindingFlags.Static | BindingFlags.Public,
            Type.DefaultBinder,
            types: [typeof(string)],
            modifiers: default
            ).CreateDelegate(typeof(Func<string, string>));

    public static string GetPowerPlatformBaseUri(string geo, string environmentId)
        => GetPowerPlatformBaseUriFunc(geo, environmentId);

    public static string GetPowerPlatformTokenAudienceUrl(string geo)
        => GetPowerPlatformTokenAudienceUrlFunc(geo);
}