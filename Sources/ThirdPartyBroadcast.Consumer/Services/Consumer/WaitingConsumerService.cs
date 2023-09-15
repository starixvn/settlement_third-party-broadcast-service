namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public class WaitingConsumerService : IConsumer<IBroadcastApiWaiting>
{
    private readonly GenerateConsumerService service;

    public WaitingConsumerService(GenerateConsumerService service)
    {
        this.service = service;
    }

    public async Task Consume(ConsumeContext<IBroadcastApiWaiting> context)
    {
        var message = context.Message;
        await service.SettleByProduct(message.ProductType, message.Data);
    }
}