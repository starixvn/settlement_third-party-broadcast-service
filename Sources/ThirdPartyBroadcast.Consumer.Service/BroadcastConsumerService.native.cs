using Microsoft.Extensions.Hosting;
using ThirdPartyBroadcast.Consumer.Services.Consumer;

namespace ThirdPartyBroadcast.Consumer.Service;

public class BroadcastConsumerService : IHostedService
{
    private readonly BroadcastConsumerController broadcastConsumerController;

    public BroadcastConsumerService(BroadcastConsumerController broadcastConsumerController)
    {
        this.broadcastConsumerController = broadcastConsumerController;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"ThirdPartyBroadcast.Consumer.Service is starting at {DateTime.Now}");
        await broadcastConsumerController.Process();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"ThirdPartyBroadcast.Consumer.Service is stopping at {DateTime.Now}");
        return Task.CompletedTask;
    }
}