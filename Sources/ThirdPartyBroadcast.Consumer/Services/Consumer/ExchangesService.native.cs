using Microsoft.Extensions.Options;

namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public interface ExchangesService
{
    Task ProcessingQueueReceiver();

    Task WaitingQueueReceiver();
}

public class ExchangesServiceImp : ExchangesService
{
    private const int DelayTimeStartBus = 3000;

    private const int RetryInterval = 500;

    private const int RetryCount = 1;

    private const int FetchCount = 4;

    private readonly AppSettingsKey appSettingsKey;

    private readonly GenerateConsumerService generateConsumerService;

    private readonly LogDirectlyService logDirectlyService;

    public ExchangesServiceImp(
        GenerateConsumerService generateConsumerService,
        LogDirectlyService logDirectlyService,
        IOptions<AppSettingsKey> appSettingsKey)
    {
        this.generateConsumerService = generateConsumerService;
        this.logDirectlyService = logDirectlyService;
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
                e.UseMessageRetry(retryConfig => retryConfig.Interval(RetryCount, TimeSpan.FromMilliseconds(RetryInterval)));
                e.PrefetchCount = FetchCount;
                e.Consumer(() => new ProcessingConsumerService(generateConsumerService), c => c.UseMessageRetry(retryConfig =>
                {
                    retryConfig.Interval(RetryCount, TimeSpan.FromMilliseconds(RetryInterval));
                }));

                e.Consumer(() => new ProcessingFaultConsumerService(logDirectlyService));
            });
        });

        try
        {
            await busControl.StartAsync(new CancellationToken());
            await Task.Delay(DelayTimeStartBus);
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
                e.UseMessageRetry(retryConfig => retryConfig.Interval(RetryCount, TimeSpan.FromMilliseconds(RetryInterval)));
                e.PrefetchCount = FetchCount;
                e.Consumer(() => new WaitingConsumerService(generateConsumerService), c => c.UseMessageRetry(retryConfig =>
                {
                    retryConfig.Interval(RetryCount, TimeSpan.FromMilliseconds(RetryInterval));
                }));

                e.Consumer(() => new WaitingFaultConsumerService(logDirectlyService));
            });
        });

        try
        {
            await busControl.StartAsync(new CancellationToken());
            await Task.Delay(DelayTimeStartBus);
        }
        finally
        {
            await busControl.StopAsync();
        }
    }
}