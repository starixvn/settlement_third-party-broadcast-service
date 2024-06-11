using Settlement.Business.Requests;
using BusinessConstants = Settlement.Business.Core.Constants;
using BusinessModels = Settlement.Business.Core.Models;
using BusinessServices = Settlement.Business.Services;

namespace ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;

public interface ProcessService
{
    Task<BusinessModels.Result> Settle(HappyFiveRequest request, string site, List<BusinessModels.Resources> resources);
}

public class ProcessServiceImp(BusinessServices.MatchService matchService,
    BusinessServices.BatchTicketsService batchTicketsService) : ProcessService
{
    public async Task<BusinessModels.Result> Settle(HappyFiveRequest request, string site, List<BusinessModels.Resources> resources)
    {
        try
        {
            var result = await matchService.LockMatch(request.MatchId, 1, site);
            if (result.IsSuccessful)
            {
                var batchTicketRequest = CreateBatchTicketRequest(request, site, resources);
                result = await batchTicketsService.SettleByBatch(batchTicketRequest);
                await matchService.LockMatch(request.MatchId, 0, site);
            }

            return new BusinessModels.Result
            {
                ErrCode = result.ErrCode,
                ErrMessage = result.ErrMessage
            };
        }
        catch (Exception exception)
        {
            var errorMessage = $"Match Id = {request.MatchId} - SiteDb = {site} - {exception.Message}";
            await matchService.LockMatch(request.MatchId, 0, site);

            return new BusinessModels.Result
            {
                ErrCode = 1,
                ErrMessage = errorMessage
            };
        }
    }

    private static BatchTicketRequest CreateBatchTicketRequest(HappyFiveRequest request, string site, List<BusinessModels.Resources> resources)
    {
        return new BatchTicketRequest
        {
            MatchId = request.MatchId,
            SportType = request.SportType,
            Action = request.Action,
            Resources = resources,
            DbServer = site,
            LastCustId = 0,
            LastTransId = 0,
            BatchSize = BusinessConstants.Settings.MaxBatchSize,
            BatchTotal = BusinessConstants.Settings.MaxWorker
        };
    }
}