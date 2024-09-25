using Microsoft.Extensions.Configuration;
using Settlement.Business.Core.Constants;
using Settlement.Business.Core.Models;
using Settlement.Business.Requests;

using BusinessModels = Settlement.Business.Core.Models;

using BusinessServices = Settlement.Business.Services;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;

public interface ProcessService
{
    Task<BusinessModels.Result> Settle(VirtualRacingRequest request, string site, IEnumerable<Outright> outright, IEnumerable<Resources> resources);
}

public class ProcessServiceImp(
    BusinessServices.MatchService matchService,
    BusinessServices.Products.RacingProcessService processService,
    IConfiguration configuration) : ProcessService
{
    public async Task<BusinessModels.Result> Settle(VirtualRacingRequest request, string site, IEnumerable<Outright> outright, IEnumerable<Resources> resources)
    {
        try
        {
            var result = await matchService.LockMatch(request.EventId, 1, site, LockMatchType.VirtualRacing);
            if (!result.IsSuccessful)
            {
                var errorMessage = GetErrorMessage(request.EventId, site, result.ErrMessage);
                return new BusinessModels.Result
                {
                    ErrCode = result.ErrCode,
                    ErrMessage = errorMessage
                };
            }

            var batchTicketRequest = CreateBatchTicketRequest(request, site, resources);
            result = await processService.Settle(batchTicketRequest, outright);
            await matchService.LockMatch(request.EventId, 0, site, LockMatchType.VirtualRacing);
            return result;
        }
        catch (Exception exception)
        {
            var errorMessage = GetErrorMessage(request.EventId, site, exception.Message);
            await matchService.LockMatch(request.EventId, 0, site, LockMatchType.VirtualRacing);

            return new BusinessModels.Result
            {
                ErrCode = 1,
                ErrMessage = errorMessage
            };
        }
    }

    private static string GetErrorMessage(int matchId, string site, string message)
    {
        return $"Match Id = {matchId} - SiteDb = {site} - {message}";
    }

    private BatchTicketRequest CreateBatchTicketRequest(VirtualRacingRequest request, string site, IEnumerable<Resources> resources)
    {
        return new BatchTicketRequest
        {
            MatchId = request.EventId,
            SportType = request.SportType,
            Racers = request.Racers,
            Action = request.Action,
            ActionId = request.ActionId,
            Resources = resources.ToList(),
            DbServer = site,
            LastCustId = 0,
            LastTransId = 0,
            BatchSize = Convert.ToInt32(configuration.GetSection("MaxBatchSize").Value),
            BatchTotal = Convert.ToInt32(configuration.GetSection("MaxWorker").Value)
        };
    }
}