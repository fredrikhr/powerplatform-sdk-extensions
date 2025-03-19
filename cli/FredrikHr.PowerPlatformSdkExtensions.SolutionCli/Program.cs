using System.CommandLine;
using System.CommandLine.Hosting;

using FredrikHr.PowerPlatformSdkExtensions.SolutionCli;

using Microsoft.Extensions.DependencyInjection;

RootCommand cliRoot = [new HostingConfigurationDirective()];
Option<bool> globalOption = new("--global", "-g")
{
    Arity = ArgumentArity.ZeroOrOne,
    Required = false,
    Recursive = true,
    DefaultValueFactory = static _ => true,
};
globalOption.Configure(
    (PersistenceScopeOptions opt, bool isGlobal) => opt.IsGlobal = isGlobal
    );
cliRoot.Add(globalOption);
Option<bool> localOption = new("--local", "-l")
{
    Arity = ArgumentArity.ZeroOrOne,
    Required = false,
    Recursive = true,
};
localOption.Configure(
    (PersistenceScopeOptions opt, bool isLocal) => opt.IsLocal = isLocal
    );
cliRoot.Add(localOption);

CommandLineConfiguration cliConfig = new(cliRoot);
cliConfig.UseHosting();

return await cliConfig.InvokeAsync(args ?? [])
    .ConfigureAwait(continueOnCapturedContext: false);
