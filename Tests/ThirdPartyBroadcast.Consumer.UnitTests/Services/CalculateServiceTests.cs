using Fanex.SiteDbConnectionManager.Client.Models;
using Fanex.SiteDbConnectionManager.Client.Services;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;
using ResultMessage = Fanex.SiteDbConnectionManager.Client.Models.ResultMessage;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services;

public class CalculateServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly SiteDbClientService siteDbClientService;

    private readonly SiteDbPendingCommandService pendingCommandService;

    private readonly CalculateServiceImp service;

    public CalculateServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        siteDbClientService = Substitute.For<SiteDbClientService>();
        pendingCommandService = Substitute.For<SiteDbPendingCommandService>();

        var appSettingsKey = new AppSettingsKey { SiteDbServer = ["01", "02"] };
        var options = Options.Create(appSettingsKey);

        service = new CalculateServiceImp(repository, options, siteDbClientService, pendingCommandService);
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
    [InlineData(0)]
    [InlineData(1)]
    public async Task ExecuteValidationMultiServer_Always_ExecuteCorrectly(int errorCode)
    {
        var sites = new List<SiteDb>
        {
            new SiteDb { SiteDbGroupId = 1, SiteName = "Site1" }
        };

        siteDbClientService.GetActiveSiteDbAsync().Returns(sites);

        await repository.ExecuteAsync(Arg.Do<BaseCriteria>(criteria => criteria.ErrCode = errorCode));

        var results = await service.ExecuteValidationMultiServer(new BaseCriteria());

        Assert.Equal(results.ErrorCode, errorCode);
        await repository.Received(1).ExecuteAsync(Arg.Any<BaseCriteria>());
    }

    [Fact]
    public async Task Execute_AllSitesActiveAndNoPendingCommand_ReturnsSuccess()
    {
        // Arrange
        var criteria = new CalculateCriteria();
        var command = new SiteDbPendingCommand();
        var sites = new List<SiteDb>
        {
            new SiteDb { SiteDbGroupId = 1, SiteName = "Site1" },
            new SiteDb { SiteDbGroupId = 2, SiteName = "Site2" }
        };

        siteDbClientService.GetAllSiteDbAsync().Returns(sites);
        siteDbClientService.IsSiteDbActiveAsync(Arg.Any<int>()).Returns(true);
        pendingCommandService.CheckingPendingCommand(Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ResultMessage { ErrorCode = 0 });

        // Act
        var result = await service.Execute(criteria, command);

        // Assert
        Assert.Equal(0, result.ErrorCode);
        Assert.Equal("", result.ErrorMessage);
        await repository.Received().ExecuteAsync(Arg.Any<CalculateCriteria>());
    }

    [Fact]
    public async Task Execute_OneSiteInactiveWithPendingCommand_ReturnsError()
    {
        // Arrange
        var criteria = new CalculateCriteria();
        var command = new SiteDbPendingCommand();
        var sites = new List<SiteDb>
        {
            new SiteDb { SiteDbGroupId = 1, SiteName = "Site1" },
            new SiteDb { SiteDbGroupId = 2, SiteName = "Site2" }
        };

        siteDbClientService.GetAllSiteDbAsync().Returns(sites);

        // First site is inactive and has a pending command
        siteDbClientService.IsSiteDbActiveAsync(1).Returns(false);
        pendingCommandService.CheckingPendingCommand(command.MatchId, 1)
            .Returns(new ResultMessage { ErrorCode = 1, ErrorMessage = "Pending" });

        pendingCommandService.SavePendingCommand(Arg.Any<SiteDbPendingCommand>())
            .Returns(new ResultMessage { ErrorCode = 1, ErrorMessage = "Pending" });

        // Second site is active
        siteDbClientService.IsSiteDbActiveAsync(2).Returns(true);
        pendingCommandService.CheckingPendingCommand(command.MatchId, 2)
            .Returns(new ResultMessage { ErrorCode = 0 });

        // Act
        var result = await service.Execute(criteria, command);

        // Assert
        Assert.True(result.ErrorCode > 0);
        Assert.Contains("Pending", result.ErrorMessage);
        await repository.Received().ExecuteAsync(Arg.Any<CalculateCriteria>());
    }
}