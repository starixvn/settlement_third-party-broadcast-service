namespace ThirdPartyBroadcast.UnitTests.Services;

using Criteria.VirtualBetRadar;

public class VirtualBetRadarServiceTests
{
    private readonly CalculateService calculateService;

    private readonly IDynamicRepository repository;

    private readonly VirtualBetRadarService service;
    private readonly LogDirectlyService logService;
    public VirtualBetRadarServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        repository = Substitute.For<IDynamicRepository>();
        logService = Substitute.For<LogDirectlyService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new VirtualBetRadarServiceImp(repository, mapper, calculateService, logService);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task Settle_ValidateFailed_StopProcess(int validateDeployFlag, int validateTicket)
    {
        calculateService.ValidationDeployFlag(SpName.VirtualBetRadar)
            .Returns(new Result { ErrorCode = validateDeployFlag });
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>())
            .Returns(new Result { ErrorCode = validateTicket });

        var result = await service.Settle(new AutoVirtualBetRadarRequest());

        Assert.Equal(1, result.ErrorCode);
        await repository.DidNotReceive().ExecuteAsync(Arg.Any<BetRadarUpdateMatchCriteria>());
    }

    [Theory]
    [InlineData(ActionType.Calculate, false, false, true, true, 0)]
    [InlineData(ActionType.Calculate, true, false, false, false, 2)]
    [InlineData(ActionType.Calculate, false, true, false, false, 2)]
    [InlineData(ActionType.Calculate, true, false, true, false, 10)]
    [InlineData(ActionType.Calculate, true, false, false, true, 10)]
    [InlineData(ActionType.Refund, true, false, false, true, 10)]
    public async Task Settle_ValidatePassed_CallSettleWithCorrectNotifyStatus(string action, bool calculate, bool calRefund, bool multiple, bool refund, int expected)
    {
        var request = new AutoVirtualBetRadarRequest
        {
            Action = action,
        };
        var matchStatus = new MatchStatus
        {
            Refund = refund,
            Multiple = multiple,
            CalRefund = calRefund,
            Calculate = calculate
        };

        IMapper mapperMock = Substitute.For<IMapper>();
        var serviceMock = new VirtualBetRadarServiceImp(repository, mapperMock, calculateService, logService);
        calculateService.ValidationDeployFlag(SpName.VirtualBetRadar).Returns(new Result());
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>()).Returns(new Result());
        await repository.ExecuteAsync(Arg.Any<BetRadarUpdateMatchCriteria>());
        calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>()).Returns(new Result());
        repository.GetAsync<MatchStatus>(Arg.Any<GetMatchStatusCriteria>()).Returns(matchStatus);
        mapperMock.Map<AutoVirtualBetRadarRequest, VirtualBetRadarCriteria>(request).Returns(new VirtualBetRadarCriteria { DbServer = "DBTXN" });

        //Action
        await serviceMock.Settle(request);

        //Assert
        await calculateService.Received().Execute(Arg.Is<VirtualBetRadarCriteria>(item => item.NotifyFlag == expected));
    }

    [Fact]
    public async Task Settle_ValidatePassed_SettleSuccessAndWriteLog()
    {
        calculateService.ValidationDeployFlag(SpName.VirtualBetRadar)
            .Returns(new Result());
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>())
            .Returns(new Result());
        calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>()).Returns(new Result());

        await service.Settle(new AutoVirtualBetRadarRequest());

        await repository.Received().ExecuteAsync(Arg.Any<BetRadarUpdateMatchCriteria>());
        await calculateService.Received().Execute(Arg.Any<VirtualBetRadarCriteria>());
        repository.Received().Execute(Arg.Any<WriteLogCriteria>());
    }

    [Fact]
    public async Task Settle_ValidatePassedAndSettleFailed_WriteCorrectLog()
    {
        var resultSettle = new Result
        {
            ErrorCode = 1,
            ErrorMessage = "Message"
        };
        calculateService.ValidationDeployFlag(SpName.VirtualBetRadar)
            .Returns(new Result());
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>())
            .Returns(new Result());
        calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>()).Returns(resultSettle);

        //action
        await service.Settle(new AutoVirtualBetRadarRequest());
        var message = "NAP: VS BetRadar - Error: API inserted into single queue failed. Message";

        //assert
        await repository.Received().ExecuteAsync(Arg.Any<BetRadarUpdateMatchCriteria>());
        await calculateService.Received().Execute(Arg.Any<VirtualBetRadarCriteria>());
        repository.Received().Execute(Arg.Is<WriteLogCriteria>(item => item.Remark == message));
    }
}