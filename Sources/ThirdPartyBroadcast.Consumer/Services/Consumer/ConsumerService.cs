namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public class ProcessingConsumerService : IConsumer<IBroadcastApiProcessing>
{
    //private readonly BitCoinService bitCoinService;

    //public ProcessingConsumerService(BitCoinService bitCoinService)
    //{
    //    this.bitCoinService = bitCoinService;
    //}

    public async Task Consume(ConsumeContext<IBroadcastApiProcessing> context)
    {
        //switch (context.Message.ProductType)
        //{
        //    case ProductType.BitCoin:
        //        var request = JsonConvert.DeserializeObject<AutoBitCoinRequest>(context.Message.Data);
        //        if (request != null)
        //        {
        //            await bitCoinService.Settle(request);
        //        }
        //        break;
        //}
        var data = JsonConvert.DeserializeObject<AutoHappyFiveRequest>(context.Message.Data);
        Console.WriteLine($"- Processing MatchId: {data.MatchId}");
    }
}

public class WaitingConsumerService : IConsumer<IBroadcastApiWaiting>
{
    public async Task Consume(ConsumeContext<IBroadcastApiWaiting> context)
    {
        var data = JsonConvert.DeserializeObject<AutoHappyFiveRequest>(context.Message.Data);
        Console.WriteLine($"- Waiting MatchId: {data.MatchId}");
    }
}