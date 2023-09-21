using ThirdPartyBroadcast.Consumer.Services.Products;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface GroupOneService
{
    Task SettleOutright(string productType, string data);

    Task SettleBitCoin(string productType, string data);

    Task SettleHappyFive(string productType, string data);

    Task SettleNumberGame(string productType, string data);
}

public class GroupOneServiceImp : GroupOneService
{
    private readonly LogDirectlyService logDirectlyService;

    private readonly OutrightService outrightService;

    private readonly BitCoinService bitCoinService;

    private readonly HappyFiveService happyFiveService;

    private readonly NumberGameService numberGameService;

    public GroupOneServiceImp(
        OutrightService outrightService,
        BitCoinService bitCoinService,
        LogDirectlyService logDirectlyService,
        HappyFiveService happyFiveService,
        NumberGameService numberGameService)
    {
        this.outrightService = outrightService;
        this.bitCoinService = bitCoinService;
        this.logDirectlyService = logDirectlyService;
        this.happyFiveService = happyFiveService;
        this.numberGameService = numberGameService;
    }

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

    public async Task SettleHappyFive(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<HappyFiveRequest>(data);
        var result = await happyFiveService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleNumberGame(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<NumberGameRequest>(data);
        var result = await numberGameService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }
}