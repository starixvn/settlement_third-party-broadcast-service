namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public interface GenerateConsumerService
{
    Task SettleByProduct(string productType, string data);
}

public class GenerateConsumerServiceImp(
    GroupOneService groupOneService,
    GroupTwoService groupTwoService,
    GroupThreeService groupThreeService,
    LogDirectlyService logDirectlyService)
    : GenerateConsumerService
{
    private static List<string> ProductsGroupOne =>
    [
        ProductType.Outright,
        ProductType.BitCoin,
        ProductType.NumberGame,
        ProductType.BetBuilder,
        ProductType.FastMarket,
        ProductType.HappyFive
    ];

    private static List<string> ProductsGroupTwo =>
    [
        ProductType.VirtualBetRadar,
        ProductType.VirtualSports,
        ProductType.LeapGaming,
        ProductType.CockFighting,
        ProductType.PinGoal,
        ProductType.VirtualSports5
    ];

    private static List<string> ProductsGroupThree =>
    [
        ProductType.VirtualRacing,
        ProductType.VirtualSports6
    ];

    public async Task SettleByProduct(string productType, string data)
    {
        if (ProductsGroupOne.Contains(productType))
        {
            await SettleProductByGroupOne(productType, data);
        }
        else if (ProductsGroupTwo.Contains(productType))
        {
            await SettleProductByGroupTwo(productType, data);
        }
        else if (ProductsGroupThree.Contains(productType))
        {
            await SettleProductByGroupThree(productType, data);
        }

        await logDirectlyService.WriteInfo(productType, data);
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

            case ProductType.CockFighting:
            case ProductType.LeapGaming:
                await groupTwoService.SettleLeapGaming(productType, data);
                break;

            case ProductType.PinGoal:
                await groupTwoService.SettlePinGoal(productType, data);
                break;

            case ProductType.VirtualSports5:
                await groupTwoService.SettleVirtualSportFive(productType, data);
                break;

            default:
                await groupTwoService.SettleVirtualSoccer(productType, data);
                break;
        }
    }

    private async Task SettleProductByGroupThree(string productType, string data)
    {
        switch (productType)
        {
            case ProductType.VirtualRacing:
                await groupThreeService.SettleVirtualRacing(productType, data);
                break;
        }
    }
}