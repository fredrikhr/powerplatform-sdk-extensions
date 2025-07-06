using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

using FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

var cliBuilder = Host.CreateApplicationBuilder(args ?? []);
var cliServices = cliBuilder.Services;
cliServices.ConfigureAll<HttpClientFactoryOptions>(opts =>
    opts.HttpMessageHandlerBuilderActions.Add(http =>
    {
        var cookiesProvider = http.Services
            .GetRequiredService<IOptionsMonitor<CookieContainer>>();
        http.PrimaryHandler = new HttpClientHandler()
        {
            UseCookies = true,
            CookieContainer = cookiesProvider.Get(http.Name)
        };
    }));
cliServices.AddHttpClient<GitHubActionsIdTokenClient>("GitHubActionsOidc");
cliServices.AddMsal().UseLogging(cliBuilder.Environment.IsDevelopment());
cliServices.AddOptions<ConfidentialClientApplicationBuilder>()
    .UseHttpClient("Msal");
cliServices.AddOptions<ConfidentialClientApplicationOptions>()
    .BindConfiguration(nameof(ConfidentialClientApplication));

const string federatedCredentialsSectionName = "FederatedCredentials";
string? federatedCredentialsProvider = cliBuilder
    .Configuration[ConfigurationPath.Combine(federatedCredentialsSectionName, "Provider")];
string federatedCredentialsAudience = cliBuilder
    .Configuration[ConfigurationPath.Combine(federatedCredentialsSectionName, "Audience")]
    ?? "api://AzureADTokenExchange";
switch (federatedCredentialsProvider?.ToUpperInvariant() ?? "")
{
    case "GITHUBACTIONS":
        cliServices.AddOptions<ConfidentialClientApplicationBuilder>()
            .PostConfigure<GitHubActionsIdTokenClient, IConfiguration>(
                (builder, idTokenClient, config) => builder
                .WithClientAssertion(async assertionOptions =>
                {
                    string? idToken = await idTokenClient.GetIdTokenAsync(
                        federatedCredentialsAudience,
                        assertionOptions.CancellationToken
                        ).ConfigureAwait(continueOnCapturedContext: false);
                    return idToken;
                }));
        break;
}
cliServices.AddHostedService<SandboxWorkerRuntimeDownloaderService>();

using var cliHost = cliBuilder.Build();
await cliHost.RunAsync().ConfigureAwait(continueOnCapturedContext: false);