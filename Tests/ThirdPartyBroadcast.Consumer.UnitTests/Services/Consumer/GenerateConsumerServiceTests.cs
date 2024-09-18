using Fanex.Settlement.ThirdPartyBroadcast.Shared;
using ThirdPartyBroadcast.Consumer.Services.Consumer;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Consumer;

public class GenerateConsumerServiceTests
{
    private readonly GroupOneService groupOneService;

    private readonly GroupTwoService groupTwoService;

    private readonly GroupThreeService groupThreeService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly GenerateConsumerService service;

    public GenerateConsumerServiceTests()
    {
        groupOneService = Substitute.For<GroupOneService>();
        groupTwoService = Substitute.For<GroupTwoService>();
        groupThreeService = Substitute.For<GroupThreeService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        service = new GenerateConsumerServiceImp(groupOneService, groupTwoService, groupThreeService, logDirectlyService);
    }

    [Fact]
    public async Task SettleByProduct_Outright_CallSettleOutrightMethod()
    {
        const string productType = ProductType.Outright;
        await service.SettleByProduct(productType, string.Empty);
        await groupOneService.Received().SettleOutright(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SettleByProduct_BitCoin_CallSettleBitCoinMethod()
    {
        const string productType = ProductType.BitCoin;
        await service.SettleByProduct(productType, string.Empty);
        await groupOneService.Received().SettleBitCoin(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SettleByProduct_NumberGame_CallSettleNumberGameMethod()
    {
        const string productType = ProductType.NumberGame;
        await service.SettleByProduct(productType, string.Empty);
        await groupOneService.Received().SettleNumberGame(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SettleByProduct_BetBuilder_CallSettleBetBuilderMethod()
    {
        const string productType = ProductType.BetBuilder;
        await service.SettleByProduct(productType, string.Empty);
        await groupOneService.Received().SettleBetBuilder(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SettleByProduct_FastMarket_CallSettleFastMarketMethod()
    {
        const string productType = ProductType.FastMarket;
        await service.SettleByProduct(productType, string.Empty);
        await groupOneService.Received().SettleFastMarket(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SettleByProduct_HappyFive_CallSettleHappyFiveMethod()
    {
        const string productType = ProductType.HappyFive;
        await service.SettleByProduct(productType, string.Empty);
        await groupOneService.Received().SettleHappyFive(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SettleByProduct_VirtualBetRadar_CallSettleVirtualBetRadarMethod()
    {
        const string productType = ProductType.VirtualBetRadar;
        await service.SettleByProduct(productType, string.Empty);
        await groupTwoService.Received().SettleVirtualBetRadar(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SettleByProduct_PinGoal_CallSettlePinGoalMethod()
    {
        const string productType = ProductType.PinGoal;
        await service.SettleByProduct(productType, string.Empty);
        await groupTwoService.Received().SettlePinGoal(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SettleByProduct_VirtualSoccer_CallSettleVirtualSoccerMethod()
    {
        const string productType = ProductType.VirtualSports;
        await service.SettleByProduct(productType, string.Empty);
        await groupTwoService.Received().SettleVirtualSoccer(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(ProductType.CockFighting)]
    [InlineData(ProductType.LeapGaming)]
    public async Task SettleByProduct_LeapGamingOrCockFighting_CallSettleLeapGamingMethod(string productType)
    {
        await service.SettleByProduct(productType, string.Empty);
        await groupTwoService.Received().SettleLeapGaming(Arg.Any<string>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteInfo(Arg.Any<string>(), Arg.Any<string>());
    }
}