using Fanex.Logging;
using Fanex.Logging.Extensions;
using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public class ProcessingFaultConsumerService : IConsumer<Fault<IBroadcastApiProcessing>>
{
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
            await HandleLog(result, message.ProductType, message.Data);
        }

        await Task.CompletedTask;
    }

    private static async Task HandleLog(Result result, string productType, string data)
    {
        if (!result.IsSuccessful)
        {
            var info = new Dictionary<string, object>
            {
                { "Product Name", productType.ToProductName() },
                { "Match Info", data },
                { "Details", result.ErrorMessage }
            };

            var logMessage = new LogMessage(result.ErrorMessage)
                .AddSection("Error Info", info)
                .ToString();

            await LogService.ErrorAsync(logMessage);
        }
    }
}