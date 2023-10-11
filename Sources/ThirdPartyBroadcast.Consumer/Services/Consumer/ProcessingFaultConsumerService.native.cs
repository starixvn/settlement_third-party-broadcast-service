namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public class ProcessingFaultConsumerService : IConsumer<Fault<IBroadcastApiProcessing>>
{
    private readonly LogDirectlyService logDirectlyService;

    public ProcessingFaultConsumerService(LogDirectlyService logDirectlyService)
    {
        this.logDirectlyService = logDirectlyService;
    }

    public async Task Consume(ConsumeContext<Fault<IBroadcastApiProcessing>> context)
    {
        var originalMessage = context.Message;
        var message = originalMessage.Message;
        var exception = originalMessage.Exceptions;
        var errorMessage = exception.Aggregate(string.Empty, (current, ex) => current + $"{ex.Message}\n");

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            var result = new Result
            {
                ErrorMessage = errorMessage
            };
            await logDirectlyService.HandleLog(result, message.ProductType, message.Data);
        }

        await Task.CompletedTask;
    }
}