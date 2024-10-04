using Settlement.Business.Core.Models;
using System.Diagnostics;
using ThirdPartyBroadcast.Consumer.Criteria;
using Result = ThirdPartyBroadcast.Consumer.Models.Result;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;

using RacingInfo = (RaceInfo RaceInfo, IEnumerable<Outright> Outright, IEnumerable<Resources> Resources);

public interface VirtualRacingService
{
    Task<Result> Settle(VirtualRacingRequest request);
}

public class VirtualRacingServiceImp(
    RacersValidator racersValidator,
    SettleSitesService settleSitesService,
    IDynamicRepository repository,
    MatchService broadcastMatchService,
    Settlement.Business.Services.MatchService matchService,
    AnnouncementService announcementService,
    RecipientMessageService recipientMessageService,
    TicketInfoService ticketService) : VirtualRacingService
{
    private List<TicketSettleInfo> CurrentTickets { get; set; }

    public async Task<Result> Settle(VirtualRacingRequest request)
    {
        RacingInfo raceInfo = await matchService.GetRacingInfo(request.EventId);

        request.SportType = raceInfo.RaceInfo.SportType;
        request.Racers = raceInfo.RaceInfo.Racers;

        var result = racersValidator.ValidateBeforeSettle(request, raceInfo.Outright);

        if (!result.IsSuccessful)
        {
            return result;
        }

        CurrentTickets = await ticketService.GetTicketsByAction(request.EventId, request.SportType, request.Action);

        await broadcastMatchService.UpdateMatchInfoRacing(request);
        await broadcastMatchService.WriteLog(request.EventId);

        return await HandleSettle(request, raceInfo);
    }

    private async Task<Result> HandleSettle(VirtualRacingRequest request, RacingInfo racingInfo)
    {
        var watch = Stopwatch.StartNew();
        var result = await settleSitesService.Settle(request, racingInfo.Outright, racingInfo.Resources);
        watch.Stop();

        await WriteLogSettled(request, result, racingInfo.Outright, watch.ElapsedMilliseconds);

        var matchInfo = broadcastMatchService.GetRacingInfo(racingInfo.RaceInfo);
        await recipientMessageService.Message(request.Action, matchInfo);
        await announcementService.SendAnnounceRefund(CurrentTickets, matchInfo, request.AdminId);

        return result;
    }

    private async Task WriteLogSettled(VirtualRacingRequest request, Result result, IEnumerable<Outright> outright, long duration)
    {
        var message = result.ErrorCode > 0 ? $"<br/>{result.ErrorMessage}" : string.Empty;

        string remark;
        if (string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            remark = $"IsAuto: 1<br/>Duration: {duration} ms {message}";
        }
        else
        {
            var placingResult = racersValidator.PlacingResult(request.SportType, request.Racers, outright);
            var racers = string.Join("; ", placingResult.Select(ball => $"{ball.Placing}. {ball.RacerId}").ToArray());
            remark = $"IsAuto: 1<br/>Results: {racers}<br/>Duration: {duration} ms {message}";
        }

        var criteria = CreateWriteLogCriteria(request, remark);

        await repository.ExecuteAsync(criteria);
    }

    private static WriteLogCriteria CreateWriteLogCriteria(VirtualRacingRequest request, string remark)
    {
        return new WriteLogCriteria(request.EventId, request.ActionId, request.AdminId, request.Ip)
        {
            Remark = remark
        };
    }
}