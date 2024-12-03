using Settlement.Business.Core.Models;
using System.Diagnostics;

namespace ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;

using Criteria;
using Criteria.HappyFive;
using Fanex.Settlement.Converter.Services;
using Models;
using ThirdPartyBroadcast.Consumer.Models.HappyFive;

public interface HappyFiveService
{
    Task<Result> Settle(HappyFiveRequest request);
}

public class HappyFiveServiceImp(
    SettleSitesService settleSitesService,
    IDynamicRepository repository,
    MatchService broadcastMatchService,
    Settlement.Business.Services.MatchService matchService,
    XmlConvertService xmlConvertService)
    : HappyFiveService
{
    public async Task<Result> Settle(HappyFiveRequest request)
    {
        var result = await ValidateBeforeSettle(request);

        if (!result.IsSuccessful)
        {
            return result;
        }

        await broadcastMatchService.UpdateMatchInfoHappyFive(request);
        await broadcastMatchService.WriteLog(request.MatchId);

        var (resources, balls) = await matchService.GetResourceHappy5(request.MatchId, request.Action);
        var watch = Stopwatch.StartNew();
        result = await settleSitesService.Settle(request, resources.ToList());
        watch.Stop();

        await UpdateBalls(result, request, balls);
        await WriteLogSettled(request, result, balls, watch.ElapsedMilliseconds);

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

    private static bool IsCalculate(string actionName) => string.Equals(actionName, ActionName.Calculate, StringComparison.OrdinalIgnoreCase);

    private async Task<Result> ValidateBeforeSettle(HappyFiveRequest request)
    {
        if (!IsCalculate(request.Action))
        {
            return new Result();
        }

        var criteriaValidation = new ValidateHappyFive { MatchId = request.MatchId, Action = request.Action };
        await repository.ExecuteAsync(criteriaValidation);
        return new Result
        {
            ErrorCode = criteriaValidation.ErrCode,
            ErrorMessage = criteriaValidation.ErrMsg
        };

    }

    private async Task UpdateBalls(Result result, HappyFiveRequest request, IEnumerable<Ball> balls)
    {
        if (result.IsSuccessful && !IsCalculate(request.Action))
        {
            var updateMatchCriteria = new UpdateBallsCriteria
            {
                MatchId = request.MatchId,
                Resources = ResourcesBalls(balls)
            };

            await repository.ExecuteAsync(updateMatchCriteria);
        }
    }

    private string ResourcesBalls(IEnumerable<Ball> balls)
    {
        var resourceItems = balls.Select(ball => new ResourcesItem { Seq = ball.Seq, NumberBall = 0 }).ToList();

        var resources = new Resources
        {
            ResourceItems = resourceItems
        };

        return xmlConvertService.Serialize(resources);
    }
}