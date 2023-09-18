namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public interface GenerateConsumerService
{
    Task SettleByProduct(string productType, string data);
}

public class GenerateConsumerServiceImp : GenerateConsumerService
{
    private readonly GroupOneService groupOneService;

    public GenerateConsumerServiceImp(GroupOneService groupOneService)
    {
        this.groupOneService = groupOneService;
    }

    public async Task SettleByProduct(string productType, string data)
    {
        var matchId = 0;
        switch (productType)
        {
            case ProductType.Outright:
                var autoOutrightRequest = JsonConvert.DeserializeObject<AutoOutrightRequest>(data);
                matchId = autoOutrightRequest.MatchId;
                await groupOneService.SettleOutright(autoOutrightRequest);
                break;

            case ProductType.BitCoin:
                var autoBitCoinRequest = JsonConvert.DeserializeObject<AutoBitCoinRequest>(data);
                matchId = autoBitCoinRequest.MatchId;
                await groupOneService.SettleBitCoin(autoBitCoinRequest);
                break;
        }

        Console.WriteLine($"- MatchId: {matchId}");
    }
}