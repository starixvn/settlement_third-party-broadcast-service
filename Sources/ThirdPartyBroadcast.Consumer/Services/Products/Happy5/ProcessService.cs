using ThirdPartyBroadcast.Consumer.Models.Happy5;

namespace ThirdPartyBroadcast.Consumer.Services.Products.Happy5;

public interface ProcessService
{
    Task<Result> Settle(int matchId, int sportType);
}

public class ProcessServiceImp(ValidationService validationService,
    BetTransService betTransService,
    CommandService commandService) : ProcessService
{
    private const int Worker = 10;

    public async Task<Result> Settle(int matchId, int sportType)
    {
        var blockingMatch = await validationService.IsBlockingMatch(matchId);
        if (blockingMatch.IsSuccessful)
        {
            var getResource = validationService.GetResource(matchId, sportType);
            if (getResource != null)
            {
                var batches = await betTransService.GetBatchTransId(matchId);
                await SettleByBatch(batches);
            }
        }

        return new Result();
    }

    private async Task<Result> SettleByBatch(List<BatchBetTrans> batches)
    {
        var tasks = new List<Task>();

        foreach (var batch in batches)
        {
            tasks.Add(commandService.Execute(batch));

            if (tasks.Count >= Worker)
            {
                var finishedTask = await Task.WhenAny(tasks);
                tasks.Remove(finishedTask);
            }
        }

        await Task.WhenAll(tasks);

        return new Result();
    }
}