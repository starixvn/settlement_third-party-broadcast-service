using Microsoft.Extensions.Configuration;
using Settlement.Business.Core.Requests;
using ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.HappyFive;

using BusinessModels = Settlement.Business.Core.Models;
using BusinessServices = Settlement.Business.Data.Services;

public class ProcessServiceTests
{
    private readonly BusinessServices.MatchService matchService;

    private readonly BusinessServices.Products.HappyFiveProcessService processService;

    private readonly ProcessServiceImp service;

    public ProcessServiceTests()
    {
        matchService = Substitute.For<BusinessServices.MatchService>();
        processService = Substitute.For<BusinessServices.Products.HappyFiveProcessService>();
        var configuration = Substitute.For<IConfiguration>();
        configuration.GetSection("MaxWorker").Value = "1";
        configuration.GetSection("MaxBatchSize").Value = "1";

        service = new ProcessServiceImp(matchService, processService, configuration);
    }

    [Fact]
    public async Task Settle_IsLockedMatch_ReturnsError()
    {
        var request = new HappyFiveRequest
        {
            MatchId = 1,
            AdminId = 0,
            SportType = 0,
            Action = null,
            Ip = null
        };
        const string site = "DB1";
        var result = new BusinessModels.Result
        {
            ErrCode = 1,
            ErrMessage = "Match is locked"
        };

        var resources = new List<BusinessModels.Resources>();

        matchService.LockMatch(request.MatchId, 1, Arg.Any<string>(), Arg.Any<int>()).Returns(result);

        var actual = await service.Settle(request, site, resources);
        const string expected = "Match Id = 1 - SiteDb = DB1 - Match is locked";

        Assert.Equal(expected, actual.ErrMessage);
    }

    [Fact]
    public async Task Settle_IsNotLockedMatch_CallSettleAndReturnsSuccessful()
    {
        var request = new HappyFiveRequest
        {
            MatchId = 1,
            AdminId = 0,
            SportType = 0,
            Action = null,
            Ip = null
        };
        const string site = "DB1";
        var result = new BusinessModels.Result
        {
            ErrCode = 0,
            ErrMessage = string.Empty
        };

        var resources = new List<BusinessModels.Resources>();

        matchService.LockMatch(request.MatchId, 1, site, Arg.Any<int>()).Returns(result);
        processService.Settle(Arg.Any<BatchTicketRequest>()).Returns(result);

        var actual = await service.Settle(request, site, resources);

        await processService.Received().Settle(Arg.Any<BatchTicketRequest>());
        await matchService.Received().LockMatch(request.MatchId, 0, site, Arg.Any<int>());

        Assert.True(actual.IsSuccessful);
    }

    [Fact]
    public async Task Settle_SettleIsException_ReturnError()
    {
        var request = new HappyFiveRequest
        {
            MatchId = 1,
            AdminId = 0,
            SportType = 0,
            Action = null,
            Ip = null
        };
        const string site = "DB1";
        var result = new BusinessModels.Result
        {
            ErrCode = 0,
            ErrMessage = string.Empty
        };

        var resources = new List<BusinessModels.Resources>();

        matchService.LockMatch(request.MatchId, 1, site, Arg.Any<int>()).Returns(result);
        processService.When(s => s.Settle(Arg.Any<BatchTicketRequest>()))
            .Throw(new Exception("Didn't work"));

        var actual = await service.Settle(request, site, resources);
        await matchService.Received().LockMatch(request.MatchId, 0, site, Arg.Any<int>());
        await processService.Received().Settle(Arg.Any<BatchTicketRequest>());

        const string expected = "Match Id = 1 - SiteDb = DB1 - Didn't work";

        Assert.Equal(expected, actual.ErrMessage);
    }
}