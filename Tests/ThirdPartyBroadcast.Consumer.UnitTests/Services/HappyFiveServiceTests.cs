//using ThirdPartyBroadcast.Criteria.HappyFive;

//namespace ThirdPartyBroadcast.UnitTests.Services;

//public class HappyFiveServiceTests
//{
//    private readonly IDynamicRepository repository;

//    private readonly CalculateService calculateService;

//    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

//    private readonly HappyFiveService service;
//    private readonly LogDirectlyService logDirectlyService;

//    public HappyFiveServiceTests()
//    {
//        repository = Substitute.For<IDynamicRepository>();
//        calculateService = Substitute.For<CalculateService>();
//        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
//        logDirectlyService = Substitute.For<LogDirectlyService>();

//        var mapperConfig = new MapperConfigurationExpression();
//        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
//        var config = new MapperConfiguration(mapperConfig);
//        var mapper = new Mapper(config);
//        service = new HappyFiveServiceImp(mapper, calculateService, repository, scaleOutSettleThirdPartyService, logDirectlyService);
//    }

//    [Theory]
//    [InlineData(1, 0)]
//    [InlineData(0, 1)]
//    public async Task Settle_ValidateFailed_StopProcess(int validateDeployFlag, int validateTicket)
//    {
//        calculateService.ValidationDeployFlag(SpName.HappyFive)
//            .Returns(new Result { ErrorCode = validateDeployFlag });
//        await repository.ExecuteAsync(Arg.Do<ValidateHappyFive>(item => item.ErrCode = validateTicket));

//        var result = await service.Settle(new AutoHappyFiveRequest());

//        Assert.Equal(1, result.ErrorCode);
//        await scaleOutSettleThirdPartyService.DidNotReceive().UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3rdRequest>());
//    }

//    [Fact]
//    public async Task Settle_ValidatePassed_DoSettle()
//    {
//        calculateService.ValidationDeployFlag(SpName.HappyFive)
//            .Returns(new Result());
//        await repository.ExecuteAsync(Arg.Do<ValidateHappyFive>(item => item.ErrCode = 0));

//        await service.Settle(new AutoHappyFiveRequest());

//        //Assert
//        await scaleOutSettleThirdPartyService.Received().UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3rdRequest>());
//        await calculateService.Received().Execute(Arg.Any<HappyFiveCriteria>());
//        await logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
//    }
//}