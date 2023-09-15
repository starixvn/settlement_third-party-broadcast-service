using Microsoft.Extensions.Options;

namespace ThirdPartyBroadcast.UnitTests.Services;

public class CalculateServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly CalculateService service;

    public CalculateServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        var appSettingsKey = new AppSettingsKey { SiteDbServer = new List<string> { "01", "02" } };
        var options = Options.Create(appSettingsKey);

        service = new CalculateServiceImp(repository, options, scaleOutSettleThirdPartyService);
    }

    [Fact]
    public async Task Execute_IsExistErrorCode99_ReturnErrorCode99()
    {
        await repository.ExecuteAsync(Arg.Do<CalculateCriteria>(criteria =>
        {
            criteria.ErrMsg = "Test";
            criteria.ErrCode = 99;
        }));

        var result = await service.Execute(new CalculateCriteria());

        //Assert
        Assert.Equal(99, result.ErrorCode);
    }

    [Fact]
    public async Task Execute_IsNotExistErrorCode99_ReturnCountError()
    {
        await repository.ExecuteAsync(Arg.Do<CalculateCriteria>(criteria =>
        {
            criteria.ErrMsg = "Test";
            criteria.ErrCode = 12345;
        }));

        var result = await service.Execute(new CalculateCriteria());

        //Assert
        Assert.Equal(2, result.ErrorCode);
    }

    [Fact]
    public async Task Execute_ExistError_ReturnCorrectMessage()
    {
        await repository.ExecuteAsync(Arg.Do<CalculateCriteria>(criteria =>
        {
            criteria.ErrMsg = "Test";
            criteria.ErrCode = 1;
        }));

        var result = await service.Execute(new CalculateCriteria());
        const string expected = "01: 1 - Test | 02: 1 - Test";

        //Assert
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Fact]
    public async Task Execute_NotError_ReturnResult()
    {
        await repository.ExecuteAsync(Arg.Any<CalculateCriteria>());

        var result = await service.Execute(new CalculateCriteria());

        //Assert
        Assert.Equal(0, result.ErrorCode);
        Assert.Empty(result.ErrorMessage);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 1)]
    public async Task ExecuteValidationMultiServer_Always_ExecuteCorrectly(int errorCode, int executeTime)
    {
        await repository.ExecuteAsync(Arg.Do<BaseCriteria>(criteria => criteria.ErrCode = errorCode));

        await service.ExecuteValidationMultiServer(new BaseCriteria());

        await repository.Received(executeTime).ExecuteAsync(Arg.Any<BaseCriteria>());
    }

    [Fact]
    public void ValidationDeployFlag_Always_CallService3rd()
    {
        service.ValidationDeployFlag("Mock");

        scaleOutSettleThirdPartyService.Received()
            .GetDeploymentInfo(Arg.Is<GetDeploymentInfoRequest>(item => item.SpName == "Mock"));
    }
}