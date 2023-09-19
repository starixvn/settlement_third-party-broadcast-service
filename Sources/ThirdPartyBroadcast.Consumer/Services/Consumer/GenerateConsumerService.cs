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
        switch (productType)
        {
            case ProductType.Outright:
                await groupOneService.SettleOutright(productType, data);
                break;

            case ProductType.BitCoin:
                await groupOneService.SettleBitCoin(productType, data);
                break;
        }

        Console.WriteLine($"- Match info: {data}");
    }
}