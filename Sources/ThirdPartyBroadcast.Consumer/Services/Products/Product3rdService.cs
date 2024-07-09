using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.BitCoin;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

public interface Product3rdService
{
    Task<Result> Settle(VirtualSportFiveRequest request);
}

public class Product3rdServiceImp : Product3rdService
{
    private readonly IDynamicRepository repository;
    private readonly LogDirectlyService logDirectlyService;
    private readonly IMapper mapper;

    public Product3rdServiceImp(IDynamicRepository repository, LogDirectlyService logDirectlyService, IMapper mapper)
    {
        this.repository = repository;
        this.logDirectlyService = logDirectlyService;
        this.mapper = mapper;
    }

    public async Task<Result> Settle(VirtualSportFiveRequest request)
    {
        throw new NotImplementedException();
    }

    //public async Task<Result> Settle(BitCoinRequest request)
    //{
    //    var eventStatus = await GetEventStatus(request.MatchId);

    //    await UpdateMatchInfo(request);
    //    await WriteLog(request.MatchId);

    //    var criteria = mapper.Map<BitCoinRequest, BitCoinCriteria>(request);
    //    var result = await calculateService.Execute(criteria);

    //    if (result.IsSuccessful && !string.IsNullOrWhiteSpace(eventStatus))
    //    {
    //        await Notification(request, eventStatus);
    //    }

    //    return result;
    //}

    private async Task WriteLog(int matchId)
    {
        var logMessage = logDirectlyService.Message(matchId);
        await logDirectlyService.WriteLog(matchId, logMessage);
    }

    private async Task<string> GetEventStatus(int matchId)
    {
        var criteria = new Match3rdSelCriteria { MatchId = matchId };

        return await repository.GetAsync<string>(criteria);
    }

    private async Task Notification(BitCoinRequest request, string eventStatus)
    {
        if (string.Equals(eventStatus.ToLowerInvariant(), EventStatus.Completed) || string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            var criteria = new NotifyCriteria
            {
                MatchId = request.MatchId,
                NotifyAction = string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase) ? ActionId.BitCoinRefund : ActionId.BitCoinCalc
            };

            await repository.ExecuteAsync(criteria);
        }
    }

    private async Task UpdateMatchInfo(BitCoinRequest request)
    {
        var criteria = mapper.Map<BitCoinRequest, BitCoinUpdateMatchCriteria>(request);
        await repository.ExecuteAsync(criteria);

        var updateEventStatus = new UpdateEventStatusCriteria
        {
            MatchId = request.MatchId,
            Action = request.Action
        };
        await repository.ExecuteAsync(updateEventStatus);
    }
}