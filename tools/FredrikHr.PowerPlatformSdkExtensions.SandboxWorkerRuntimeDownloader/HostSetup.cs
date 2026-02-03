using System.Net;
using System.Runtime.Versioning;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal static class HostSetup
{
    public static void Configure(HostApplicationBuilder host)
    {
        var embeddedNames = typeof(HostSetup).Assembly
            .GetManifestResourceNames();
        IEnumerable<string> settingsNames = [
            "appsettings.json",
            $"appsettings.{host.Environment.EnvironmentName}.json"
            ];
        foreach (var settingsName in settingsNames)
        {
            if (embeddedNames.FirstOrDefault(
                (n) => $"{typeof(HostSetup).Namespace}.{settingsName}".Equals(n, StringComparison.OrdinalIgnoreCase)
                ) is string manifestResourceName)
            {
                host.Configuration.AddJsonStream(
                    typeof(HostSetup).Assembly
                    .GetManifestResourceStream(manifestResourceName)!
                    );
            }
        }

        var services = host.Services;
        services.AddOptions<FederatedCredentialsOptions>()
            .BindConfiguration(FederatedCredentialsOptions.ConfigurationSectionName);
        services.ConfigureAll<HttpClientFactoryOptions>(opts =>
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
        services.AddHttpClient<GitHubActionsIdTokenClient>("GitHubActionsOidc");
        services.AddMsal()
            .UseLogging(enablePiiLogging: host.Environment.IsDevelopment())
            ;
        services.Add(ServiceDescriptor.Singleton<
            IMsalHttpClientFactory,
            MsalHttpClientFactory
            >(sp => new(sp.GetRequiredService<IHttpMessageHandlerFactory>(), "Msal")));
        services.ConfigureAll<
            ConfidentialClientApplicationBuilder,
            IMsalHttpClientFactory
            >(
            (_, msal, http) => msal.WithHttpClientFactory(http));
        services.PostConfigureAll<
            ConfidentialClientApplicationBuilder,
            IServiceProvider
            >((name, builder, sp) =>
            {
                var optionsProvider = sp.GetRequiredService<
                    IOptionsMonitor<FederatedCredentialsOptions>
                    >();
                if (optionsProvider.CurrentValue.Provider != FederatedCredentialsOptions.ProviderType.GitHubActions ||
                    !GitHubActionsIdTokenClient.IsAvailable)
                    return;
                var idTokenClient = sp.GetRequiredService<
                    GitHubActionsIdTokenClient
                    >();
                builder.WithClientAssertion(async assertionContext =>
                {
                    if (!GitHubActionsIdTokenClient.IsAvailable)
                        return null;
                    var options = optionsProvider.Get(Options.DefaultName);
                    string? idToken = await idTokenClient.GetIdTokenAsync(
                        options.Audience ?? FederatedCredentialsOptions.DefaultAudience,
                        assertionContext.CancellationToken
                        ).ConfigureAwait(continueOnCapturedContext: false);
                    return idToken;
                });
            });
        services.ConfigureAll<
            PublicClientApplicationBuilder,
            IMsalHttpClientFactory
            >(
            (_, builder, msalHttp) => builder.WithHttpClientFactory(msalHttp));
        services.ConfigureAll<PublicClientApplicationBuilder>(
            builder => builder.WithDefaultRedirectUri()
            );
        services.AddOptions<ConfidentialClientApplicationOptions>()
            .BindConfiguration(nameof(ConfidentialClientApplication));
        services.AddOptions<PublicClientApplicationOptions>()
            .BindConfiguration(nameof(ConfidentialClientApplication))
            .BindConfiguration(nameof(PublicClientApplication));
        if (OperatingSystem.IsWindowsVersionAtLeast(5, 0))
        {
            ConfigureWindowsAccountManagerBroker(host);
        }
        services.AddSingleton<MsalHttpAuthorizationResourceRegistry>();
        services.AddOptions<MsalDelegatingHandlerOptions>()
            .Configure(o => o.AuthenticationMode = MsalDelegatingHandlerAuthenticationMode.AppOnly)
            .BindConfiguration("MsalAuthenticationHandler");
        services.ConfigureAll<HttpClientFactoryOptions>(options =>
            options.HttpMessageHandlerBuilderActions.Add(http =>
            {
                MsalHttpAuthorizationDelegatingHandler delegatingHandler =
                    new(http.Name, http.Services);
                http.AdditionalHandlers.Add(delegatingHandler);
            }));

        services.AddSingleton<SandboxWorkerRuntimeDownloaderApp>();
        services.AddHostedService<SandboxWorkerRuntimeDownloaderService>();
    }

    [SupportedOSPlatform("windows5.0")]
    private static void ConfigureWindowsAccountManagerBroker(HostApplicationBuilder host)
    {
        var services = host.Services;
        nint hWnd = Windows.Win32.PInvoke.GetAncestor(
            Windows.Win32.PInvoke.GetConsoleWindow(),
            Windows.Win32.UI.WindowsAndMessaging.GET_ANCESTOR_FLAGS.GA_ROOTOWNER
            );
        if (hWnd != default)
        {
            services.AddMsal()
                .ConfigureAllBrokerOptions(BrokerOptions.OperatingSystems.Windows);
            services.ConfigureAll<
                PublicClientApplicationBuilder,
                IOptionsMonitor<BrokerOptions>
                >((name, builder, brokerOptionsProvider) => builder
                    .WithBroker(brokerOptionsProvider.Get(name))
                    .WithParentActivityOrWindow(() => hWnd)
                );
        }
    }
}
