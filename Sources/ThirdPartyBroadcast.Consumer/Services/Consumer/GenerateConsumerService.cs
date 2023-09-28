using Fanex.Logging;

namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public interface GenerateConsumerService
{
    Task SettleByProduct(string productType, string data);
}

public class GenerateConsumerServiceImp : GenerateConsumerService
{
    private readonly GroupOneService groupOneService;

    private readonly GroupTwoService groupTwoService;

    private static List<string> ProductsGroupOne => new()
    {
        ProductType.Outright,
        ProductType.BitCoin,
        ProductType.NumberGame,
        ProductType.BetBuilder,
        ProductType.FastMarket,
        ProductType.HappyFive
    };

    private static List<string> ProductsGroupTwo => new()
    {
        ProductType.VirtualBetRadar,
        ProductType.VirtualSports,
        ProductType.LeapGaming
    };

    public GenerateConsumerServiceImp(
        GroupOneService groupOneService,
        GroupTwoService groupTwoService)
    {
        this.groupOneService = groupOneService;
        this.groupTwoService = groupTwoService;
    }

    public async Task SettleByProduct(string productType, string data)
    {
        if (ProductsGroupOne.Contains(productType))
        {
            await SettleProductByGroupOne(productType, data);
        }

        if (ProductsGroupTwo.Contains(productType))
        {
            await SettleProductByGroupTwo(productType, data);
        }

        await LogService.InfoAsync($"ThirdPartyBroadcast Service - ProductType:{productType} - Match info: {data}");
    }

    private async Task SettleProductByGroupOne(string productType, string data)
    {
        switch (productType)
        {
            case ProductType.Outright:
                await groupOneService.SettleOutright(productType, data);
                break;

            case ProductType.BitCoin:
                await groupOneService.SettleBitCoin(productType, data);
                break;

            case ProductType.NumberGame:
                await groupOneService.SettleNumberGame(productType, data);
                break;

            case ProductType.BetBuilder:
                await groupOneService.SettleBetBuilder(productType, data);
                break;

            case ProductType.FastMarket:
                await groupOneService.SettleFastMarket(productType, data);
                break;

            default:
                await groupOneService.SettleHappyFive(productType, data);
                break;
        }
    }

    private async Task SettleProductByGroupTwo(string productType, string data)
    {
        switch (productType)
        {
            case ProductType.VirtualBetRadar:
                await groupTwoService.SettleVirtualBetRadar(productType, data);
                break;

            case ProductType.LeapGaming:
                await groupTwoService.SettleLeapGaming(productType, data);
                break;

            default:
                await groupTwoService.SettleVirtualSoccer(productType, data);
                break;
        }
    }
}