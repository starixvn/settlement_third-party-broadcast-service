using Microsoft.Extensions.Options;

namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public interface ExchangesService
{
    Task ProcessingQueueReceiver();

    Task WaitingQueueReceiver();
}

public class ExchangesServiceImp : ExchangesService
{
    private readonly AppSettingsKey appSettingsKey;

    private readonly GenerateConsumerService generateConsumerService;

    public ExchangesServiceImp(
        GenerateConsumerService generateConsumerService,
        IOptions<AppSettingsKey> appSettingsKey)
    {
        this.generateConsumerService = generateConsumerService;
        this.appSettingsKey = appSettingsKey.Value;
    }

    public async Task ProcessingQueueReceiver()
    {
        var rabbitMq = appSettingsKey.RabbitMq;
        var busControl = Bus.Factory.CreateUsingRabbitMq(configure =>
        {
            configure.Host(rabbitMq.Hosting, rabbitMq.VirtualHost, h =>
            {
                h.Username(rabbitMq.Username);
                h.Password(rabbitMq.Password);
            });

            configure.ReceiveEndpoint(rabbitMq.ProcessingQueue, e =>
            {
                e.UseMessageRetry(retryConfig => retryConfig.Interval(10, TimeSpan.FromMilliseconds(500)));
                e.Consumer(() => new ProcessingConsumerService(generateConsumerService), c => c.UseMessageRetry(retryConfig =>
                {
                    retryConfig.Interval(10, TimeSpan.FromMilliseconds(500));
                }));
            });
        });

        try
        {
            await busControl.StartAsync(new CancellationToken());
            Thread.Sleep(3000);
        }
        finally
        {
            await busControl.StopAsync();
        }
    }

    public async Task WaitingQueueReceiver()
    {
        var rabbitMq = appSettingsKey.RabbitMq;
        var busControl = Bus.Factory.CreateUsingRabbitMq(configure =>
        {
            configure.Host(rabbitMq.Hosting, rabbitMq.VirtualHost, h =>
            {
                h.Username(rabbitMq.Username);
                h.Password(rabbitMq.Password);
            });

            configure.ReceiveEndpoint(rabbitMq.WaitingQueue, e =>
            {
                e.UseMessageRetry(retryConfig => retryConfig.Interval(10, TimeSpan.FromMilliseconds(500)));

                e.Consumer(() => new WaitingConsumerService(generateConsumerService), c => c.UseMessageRetry(retryConfig =>
                {
                    retryConfig.Interval(10, TimeSpan.FromMilliseconds(500));
                }));
            });
        });

        try
        {
            await busControl.StartAsync(new CancellationToken());
            Thread.Sleep(3000);
        }
        finally
        {
            await busControl.StopAsync();
        }
    }
}