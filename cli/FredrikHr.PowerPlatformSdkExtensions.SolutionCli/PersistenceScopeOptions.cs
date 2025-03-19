using Microsoft.Extensions.DependencyInjection;

namespace FredrikHr.PowerPlatformSdkExtensions.SolutionCli;

internal class PersistenceScopeOptions
{
    public bool IsGlobal { get; set; }
    public bool IsLocal { get; set; }

    public static void ConfigureValidation(IServiceCollection services)
    {
        services.AddOptions<PersistenceScopeOptions>()
        .Validate(
            opts => opts.IsGlobal ^ opts.IsLocal,
            "Application cannot run in both local and global mode, nor can it be explicitly be set to run in neither mode."
        );
    }
}