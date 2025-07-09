using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed partial class MsalDelegatingHandler(
    string name,
    MsalDelegatingHandlerScopeRegistry scopeRegistry,
    IOptionsMonitor<MsalDelegatingHandlerOptions> optionsProvider,
    IOptionsMonitor<IPublicClientApplication> publicClientProvider,
    IOptionsMonitor<IConfidentialClientApplication> confidentialClientProvider,
    ILogger<DeviceCodeResult> deviceCodeLogger
    ) : DelegatingHandler()
{

    private IEnumerable<string>? GetMsalScopes(HttpRequestMessage requestMessage)
    {
        return requestMessage.RequestUri is Uri requestUri
            ? scopeRegistry.GetScopes(requestUri)
            : null;
    }

    private bool ShouldHandleAuthentication(
        HttpRequestMessage requestMessage,
        [NotNullWhen(returnValue: true)]
        out IEnumerable<string>? msalScopes,
        [NotNullWhen(returnValue: true)]
        out MsalDelegatingHandlerOptions? options
        )
    {
        options = null;
        if ((msalScopes = GetMsalScopes(requestMessage)) is null)
            return false;
        options = optionsProvider.Get(name);
        if (options.AuthenticationMode == MsalDelegatingHandlerAuthenticationMode.None)
            return false;

        return true;
    }

    private async Task HandleAuthenticationAsync(
        HttpRequestMessage requestMessage,
        IEnumerable<string> msalScopes,
        MsalDelegatingHandlerOptions options,
        CancellationToken cancelToken
        )
    {
        IConfidentialClientApplication confClient;
        IPublicClientApplication publicClient;
        AuthenticationResult? msalAuthResult = null;
        switch (options.AuthenticationMode)
        {
            case MsalDelegatingHandlerAuthenticationMode.AppOnly:
                confClient = confidentialClientProvider.Get(name);
                msalAuthResult = await confClient
                    .AcquireTokenForClient(msalScopes)
                    .ExecuteAsync(cancelToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                break;

            case MsalDelegatingHandlerAuthenticationMode.UserInteractive:
                publicClient = publicClientProvider.Get(name);
                if (!publicClient.IsUserInteractive())
                    goto case MsalDelegatingHandlerAuthenticationMode.UserDeviceCode;
                try
                    {
                        if (options.UserAccount is IAccount account)
                        {
                            msalAuthResult = await HandleSilentUserAuthentication(
                                publicClient,
                                account,
                                msalScopes,
                                cancelToken
                                ).ConfigureAwait(continueOnCapturedContext: false);
                            options.UserAccount = msalAuthResult.Account;
                        }
                    }
                    catch (MsalClientException) { msalAuthResult = null; }

                msalAuthResult ??= await publicClient
                    .AcquireTokenInteractive(msalScopes)
                    .ExecuteAsync(cancelToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                break;

            case MsalDelegatingHandlerAuthenticationMode.UserDeviceCode:
                publicClient = publicClientProvider.Get(name);
                try
                {
                    if (options.UserAccount is IAccount account)
                    {
                        msalAuthResult = await HandleSilentUserAuthentication(
                            publicClient,
                            account,
                            msalScopes,
                            cancelToken
                            ).ConfigureAwait(continueOnCapturedContext: false);
                        options.UserAccount = msalAuthResult.Account;
                    }
                }
                catch (MsalClientException) { msalAuthResult = null; }

                if (msalAuthResult is null)
                {
                    msalAuthResult = await publicClient
                        .AcquireTokenWithDeviceCode(msalScopes, OnDeviceCodeResult)
                        .ExecuteAsync(cancelToken)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    Task OnDeviceCodeResult(DeviceCodeResult result)
                    {
                        LogDeviceCodeResult(deviceCodeLogger, result.Message);
                        return Task.CompletedTask;
                    }
                }
                break;
        }

        if (msalAuthResult is null) return;

        AuthenticationHeaderValue authHeader = new(
            msalAuthResult.TokenType,
            msalAuthResult.AccessToken
            );
        requestMessage.Headers.Authorization = authHeader;

        static async Task<AuthenticationResult> HandleSilentUserAuthentication(
            IPublicClientApplication publicClient,
            IAccount account,
            IEnumerable<string> scopes,
            CancellationToken cancelToken
            )
        {
            return await publicClient.AcquireTokenSilent(
                scopes,
                account
                ).ExecuteAsync(cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    protected override HttpResponseMessage Send(
        HttpRequestMessage request,
        CancellationToken cancellationToken
        )
    {
        if (ShouldHandleAuthentication(request, out var msalScopes, out var options))
        {
            HandleAuthenticationAsync(request, msalScopes, options, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        return base.Send(request, cancellationToken);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
        )
    {
        if (ShouldHandleAuthentication(request, out var msalScopes, out var options))
        {
            await HandleAuthenticationAsync(
                request,
                msalScopes,
                options,
                cancellationToken
                ).ConfigureAwait(continueOnCapturedContext: false);
        }
        return await base.SendAsync(request, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    [LoggerMessage(
        Microsoft.Extensions.Logging.LogLevel.Critical,
        EventId = 1001, EventName = nameof(DeviceCodeResult),
        Message = "{Message}"
        )]
    private static partial void LogDeviceCodeResult(ILogger logger, string message);
}