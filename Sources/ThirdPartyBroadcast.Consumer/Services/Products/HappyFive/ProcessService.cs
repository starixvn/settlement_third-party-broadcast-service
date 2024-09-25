using Microsoft.Extensions.Configuration;
using Settlement.Business.Core.Constants;
using Settlement.Business.Requests;
using BusinessModels = Settlement.Business.Core.Models;
using BusinessServices = Settlement.Business.Services;

namespace ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;

public interface ProcessService
{
    Task<BusinessModels.Result> Settle(HappyFiveRequest request, string site, List<BusinessModels.Resources> resources);
}

public class ProcessServiceImp(
    BusinessServices.MatchService matchService,
    BusinessServices.Products.HappyFiveProcessService processService,
    IConfiguration configuration) : ProcessService
{
    public async Task<BusinessModels.Result> Settle(HappyFiveRequest request, string site, List<BusinessModels.Resources> resources)
    {
        try
        {
            var result = await matchService.LockMatch(request.MatchId, 1, site, LockMatchType.Match);
            if (!result.IsSuccessful)
            {
                var errorMessage = GetErrorMessage(request.MatchId, site, result.ErrMessage);
                return new BusinessModels.Result
                {
                    ErrCode = result.ErrCode,
                    ErrMessage = errorMessage
                };
            }

            var batchTicketRequest = CreateBatchTicketRequest(request, site, resources);
            result = await processService.Settle(batchTicketRequest);
            await matchService.LockMatch(request.MatchId, 0, site, LockMatchType.Match);
            return result;
        }
        catch (Exception exception)
        {
            var errorMessage = GetErrorMessage(request.MatchId, site, exception.Message);
            await matchService.LockMatch(request.MatchId, 0, site, LockMatchType.Match);

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

    private BatchTicketRequest CreateBatchTicketRequest(HappyFiveRequest request, string site, List<BusinessModels.Resources> resources)
    {
        return new BatchTicketRequest
        {
            MatchId = request.MatchId,
            SportType = request.SportType,
            Action = request.Action,
            ActionId = request.ActionId,
            Resources = resources,
            DbServer = site,
            LastCustId = 0,
            LastTransId = 0,
            BatchSize = Convert.ToInt32(configuration.GetSection("MaxBatchSize").Value),
            BatchTotal = Convert.ToInt32(configuration.GetSection("MaxWorker").Value)
        };
    }
}