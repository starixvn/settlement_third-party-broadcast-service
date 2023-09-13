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

    public ExchangesServiceImp(IOptions<AppSettingsKey> appSettingsKey)
    {
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
                e.Consumer<ProcessingConsumerService>();
            });
        });

        try
        {
            Console.WriteLine("Processing - Started");
            await busControl.StartAsync(new CancellationToken());
            Thread.Sleep(2000);
        }
        finally
        {
            await busControl.StopAsync();
            Console.WriteLine("Processing - Stopped");
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
                e.Consumer<WaitingConsumerService>();
            });
        });

        try
        {
            Console.WriteLine("Processing - Started");
            await busControl.StartAsync(new CancellationToken());
            Thread.Sleep(2000);
        }
        finally
        {
            await busControl.StopAsync();
            Console.WriteLine("Waiting - Stopped");
        }
    }
}