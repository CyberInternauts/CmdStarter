using Microsoft.Extensions.Hosting;

namespace CyberInternauts.CmdStarter.Tests.Commands.Hosting;

public sealed class RecordingHostedService(LifecycleRecorder recorder) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        recorder.Events.Add("started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        recorder.Events.Add("stopped");
        return Task.CompletedTask;
    }
}
