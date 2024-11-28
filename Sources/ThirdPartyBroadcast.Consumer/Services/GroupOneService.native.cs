namespace ThirdPartyBroadcast.Consumer.Services;

using Products;
using Products.BetBuilder;
using Requests;

public interface GroupOneService
{
    Task SettleOutright(string productType, string data);

    Task SettleBitCoin(string productType, string data);

    Task SettleNumberGame(string productType, string data);

    Task SettleBetBuilder(string productType, string data);

    Task SettleFastMarket(string productType, string data);
}

public class GroupOneServiceImp(
    OutrightService outrightService,
    BitCoinService bitCoinService,
    LogDirectlyService logDirectlyService,
    NumberGameService numberGameService,
    BetBuilderService betBuilderService,
    FastMarketService fastMarketService)
    : GroupOneService
{
    public async Task SettleOutright(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<OutrightRequest>(data);
        var result = await outrightService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleBitCoin(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<BitCoinRequest>(data);
        var result = await bitCoinService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleNumberGame(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<NumberGameRequest>(data);
        var result = await numberGameService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleBetBuilder(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<BetBuilderRequest>(data);
        var result = await betBuilderService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleFastMarket(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<FastMarketRequest>(data);
        var result = await fastMarketService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }
}