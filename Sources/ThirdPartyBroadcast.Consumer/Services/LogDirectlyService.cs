using Fanex.Logging;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Services;

public interface LogDirectlyService
{
    Task WriteLog(int matchId, string message);
}

public class LogDirectlyServiceImp : LogDirectlyService
{
    private readonly IDynamicRepository dynamicRepository;

    public LogDirectlyServiceImp(IDynamicRepository dynamicRepository)
    {
        this.dynamicRepository = dynamicRepository;
    }

    public async Task WriteLog(int matchId, string message)
    {
        try
        {
            var messageDoing = string.IsNullOrWhiteSpace(message) ? $"Settlement for matchId: {matchId}" : message;
            var criteria = new LogDirectlyCriteria
            {
                Extend = matchId,
                Doing = messageDoing
            };

            await dynamicRepository.ExecuteAsync(criteria);
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync("Settlement.ThirdPartyBroadcastAPI(Write Directly):", e);
        }
    }
}