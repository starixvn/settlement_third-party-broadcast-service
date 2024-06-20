using Settlement.Business.Core.Models;
using System.Diagnostics;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;
using Criteria.HappyFive;
using HappyFive;
using Models;
using Settlement.Business.Services;

public interface HappyFiveService
{
    Task<Result> Settle(HappyFiveRequest request);
}

public class HappyFiveServiceImp(
    SettleSitesService settleSitesService,
    IDynamicRepository repository,
    ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
    LogDirectlyService logDirectlyService,
    MatchService matchService)
    : HappyFiveService
{
    public async Task<Result> Settle(HappyFiveRequest request)
    {
        var result = await ValidateBeforeSettle(request);

        if (result.IsSuccessful)
        {
            await UpdateMatchInfo(request);
            await WriteLog(request.MatchId);

            var (resources, balls) = await matchService.GetResourceHappy5(request.MatchId, request.Action);
            var watch = Stopwatch.StartNew();
            result = await settleSitesService.Settle(request, resources.ToList());
            watch.Stop();

            await WriteLogSettled(request, result, balls, watch.ElapsedMilliseconds);
        }

        return result;
    }

    private async Task WriteLogSettled(HappyFiveRequest request, Result result, IEnumerable<Ball> balls, long duration)
    {
        var ball = string.Join(", ", balls.Select(ball => $"B{ball.Seq}({ball.Number})").ToArray());
        var message = result.ErrorCode > 0 ? $"<br/>{result.ErrorMessage}" : string.Empty;
        var results = request.ActionId == ActionId.HappyFiveCalc ? "Results" : "Old Results";
        var remark = $"IsAuto: 1<br/>{results}: {ball}<br/>Duration: {duration} ms {message}";

        var criteria = CreateWriteLogCriteria(request, remark);

        await repository.ExecuteAsync(criteria);
    }

    private static WriteLogCriteria CreateWriteLogCriteria(HappyFiveRequest request, string remark)
    {
        return new WriteLogCriteria(request.MatchId, request.ActionId, request.AdminId, request.Ip)
        {
            Remark = remark
        };
    }

    private async Task WriteLog(int matchId)
    {
        var logMessage = logDirectlyService.Message(matchId);
        await logDirectlyService.WriteLog(matchId, logMessage);
    }

    private async Task<Result> ValidateBeforeSettle(HappyFiveRequest request)
    {
        if (string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase))
        {
            var criteriaValidation = new ValidateHappyFive { MatchId = request.MatchId, Action = request.Action };
            await repository.ExecuteAsync(criteriaValidation);
            return new Result
            {
                ErrorCode = criteriaValidation.ErrCode,
                ErrorMessage = criteriaValidation.ErrMsg
            };
        }

        return new Result();
    }

    private async Task UpdateMatchInfo(HappyFiveRequest request)
    {
        var updateRequest = new UpdateMatchInfo3RdRequest
        {
            Command = EventStatus.Completed,
            MatchId = request.MatchId,
            Action = request.Action,
            Creator = request.AdminId
        };
        await scaleOutSettleThirdPartyService.UpdateMatchInfo3rd(updateRequest);
    }
}