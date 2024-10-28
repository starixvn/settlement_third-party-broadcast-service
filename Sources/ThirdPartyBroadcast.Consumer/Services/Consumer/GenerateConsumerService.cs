using Fanex.Logging;

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
        else
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
                await LogService.ErrorAsync($"ThirdPartyBroadcast Service: ProductType {productType} hasn't implemented yet.");
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

            case ProductType.VirtualSports:
                await groupTwoService.SettleVirtualSoccer(productType, data);
                break;

            default:
                await LogService.ErrorAsync($"ThirdPartyBroadcast Service: ProductType {productType} hasn't implemented yet.");
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

            case ProductType.HappyFive:
                await groupThreeService.SettleHappyFive(productType, data);
                break;

            case ProductType.VirtualSports6:
                await groupThreeService.SettleVirtualBetby(productType, data);
                break;

            default:
                await LogService.ErrorAsync($"ThirdPartyBroadcast Service: ProductType {productType} hasn't implemented yet.");
                break;
        }
    }
}