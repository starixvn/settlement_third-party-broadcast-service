using System.Data;

namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public class ProcessingConsumerService : IConsumer<IBroadcastApiProcessing>
{
    private readonly GenerateConsumerService service;

    public ProcessingConsumerService(GenerateConsumerService service)
    {
        this.service = service;
    }

    public async Task Consume(ConsumeContext<IBroadcastApiProcessing> context)
    {
        var message = context.Message;
        await service.SettleByProduct(message.ProductType, message.Data);
    }
}

public class MyConsumerDefinition : ConsumerDefinition<ProcessingConsumerService>
{
    private const int RetryInterval = 1000;

    private const int RetryCount = 1;

    private const int FetchItemNumber = 1;

    private const string IgnoreMessage = "SQL";

    public MyConsumerDefinition()
    {
        EndpointName = QueueName.Processing;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<ProcessingConsumerService> consumerConfigurator,
        IRegistrationContext context)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rmq)
        {
            rmq.PrefetchCount = FetchItemNumber;
            rmq.DiscardSkippedMessages();
            rmq.DiscardFaultedMessages();
            rmq.Consumer<ProcessingFaultConsumerService>();
            rmq.UseMessageRetry(retryConfig =>
                {
                    retryConfig.Ignore<ArgumentNullException>();
                    retryConfig.Ignore<DataException>(x => x.Message.Contains(IgnoreMessage));
                    retryConfig.Interval(RetryCount, TimeSpan.FromMilliseconds(RetryInterval));
                }
            );
        }
    }
}