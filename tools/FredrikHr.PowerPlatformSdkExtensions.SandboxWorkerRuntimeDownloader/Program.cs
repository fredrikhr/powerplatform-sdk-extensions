using FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

using Microsoft.Extensions.Hosting;

var cliBuilder = Host.CreateApplicationBuilder(args ?? []);
HostSetup.Configure(cliBuilder);

using var cliHost = cliBuilder.Build();
await cliHost.RunAsync().ConfigureAwait(continueOnCapturedContext: false);