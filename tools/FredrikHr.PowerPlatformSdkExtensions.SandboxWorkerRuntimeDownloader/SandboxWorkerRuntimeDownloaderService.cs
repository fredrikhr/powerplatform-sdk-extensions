using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812: Avoid uninstantiated internal classes",
    Justification = nameof(Microsoft.Extensions.DependencyInjection)
    )]
internal sealed class SandboxWorkerRuntimeDownloaderService : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly CancellationTokenRegistration _startedRegistration;
    private readonly SandboxWorkerRuntimeDownloaderApp _app;
    private readonly TaskCompletionSource _started =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Task ExecuteCoreAsync(CancellationToken cancelToken)
    {
        return _app.RunAsync(cancelToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var startingRegistration = stoppingToken.Register(CancelStartup, _started))
        {
            await _started.Task.ConfigureAwait(continueOnCapturedContext: false);
        }

        try
        {
            await ExecuteCoreAsync(stoppingToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        finally
        {
            _lifetime.StopApplication();
        }

        static void CancelStartup(object? state)
        {
            var tcs = (TaskCompletionSource)state!;
            tcs.TrySetCanceled(CancellationToken.None);
        }
    }

    public SandboxWorkerRuntimeDownloaderService(
        SandboxWorkerRuntimeDownloaderApp app,
        IHostApplicationLifetime lifetime
        ) : base()
    {
        _lifetime = lifetime;

        _startedRegistration = lifetime.ApplicationStarted.Register(
            static state => ((TaskCompletionSource)state!).TrySetResult(),
            _started
            );

        _app = app;
    }

    public override void Dispose()
    {
        _startedRegistration.Dispose();
        base.Dispose();
    }
}