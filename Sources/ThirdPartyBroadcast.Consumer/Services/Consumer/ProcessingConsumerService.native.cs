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