using Microsoft.Extensions.Configuration;
using Settlement.Business.Requests;
using ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.HappyFive;

using BusinessModels = Settlement.Business.Core.Models;
using BusinessServices = Settlement.Business.Services;

public class ProcessServiceTests
{
    private readonly BusinessServices.MatchService matchService;

    private readonly BusinessServices.HappyFive.BatchTicketsService batchTicketsService;

    private readonly ProcessService service;

    public ProcessServiceTests()
    {
        matchService = Substitute.For<BusinessServices.MatchService>();
        batchTicketsService = Substitute.For<BusinessServices.HappyFive.BatchTicketsService>();
        var configuration = Substitute.For<IConfiguration>();
        configuration.GetSection("MaxWorker").Value = "1";
        configuration.GetSection("MaxBatchSize").Value = "2000";

        service = new ProcessServiceImp(matchService, batchTicketsService, configuration);
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

        matchService.LockMatch(request.MatchId, 1, Arg.Any<string>()).Returns(result);

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

        matchService.LockMatch(request.MatchId, 1, site).Returns(result);
        batchTicketsService.Settle(Arg.Any<BatchTicketRequest>()).Returns(result);

        var actual = await service.Settle(request, site, resources);

        await batchTicketsService.Received().Settle(Arg.Any<BatchTicketRequest>());
        await matchService.Received().LockMatch(request.MatchId, 0, site);

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

        matchService.LockMatch(request.MatchId, 1, site).Returns(result);
        batchTicketsService.When(s => s.Settle(Arg.Any<BatchTicketRequest>()))
            .Throw(new Exception("Didn't work"));

        var actual = await service.Settle(request, site, resources);
        await matchService.Received().LockMatch(request.MatchId, 0, site);
        await batchTicketsService.Received().Settle(Arg.Any<BatchTicketRequest>());

        const string expected = "Match Id = 1 - SiteDb = DB1 - Didn't work";

        Assert.Equal(expected, actual.ErrMessage);
    }
}