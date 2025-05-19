using Fanex.Logging;
using Fanex.Logging.Extensions;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface LogDirectlyService
{
    string Message(int matchId);

    Task WriteInfo(string productType, string data);

    Task WriteLog(int matchId, string message);

    Task HandleLog(Result result, string productType, string data);

    Task WriteLog(InsertLogCriteria criteria);
}

public class LogDirectlyServiceImp(
    IDynamicRepository dynamicRepository,
    IOptions<AppSettingsKey> appSettingsKey)
    : LogDirectlyService
{
    private const string ErrorMessage = "Error from Settlement ThirdPartyBroadcast Service";

    public string Message(int matchId) => $"Settlement for matchId: {matchId}";

    public async Task WriteInfo(string productType, string data)
    {
        if (appSettingsKey.Value.EnableWriteLog)
        {
            await LogService.InfoAsync($"ThirdPartyBroadcast Service - Product Name:{productType.ToProductName()} - Match info: {data}");
        }
    }

    public async Task WriteLog(int matchId, string message)
    {
        try
        {
            var criteria = new LogDirectlyCriteria
            {
                Extend = matchId,
                Doing = message
            };

            await dynamicRepository.ExecuteAsync(criteria);
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync($"{ErrorMessage} (Write Directly):", e);
        }
    }

    public async Task HandleLog(Result result, string productType, string data)
    {
        if (!result.IsSuccessful)
        {
            var info = new Dictionary<string, object>
            {
                { "Product Name", productType.ToProductName() },
                { "Match Info", data },
                { "Details", result.ErrorMessage }
            };

            var logMessage = new LogMessage(result.ErrorMessage)
                .AddSection("Error Info", info)
                .ToString();

            await LogService.ErrorAsync(logMessage);
        }
    }

    public async Task WriteLog(InsertLogCriteria criteria)
    {
        try
        {
            await dynamicRepository.ExecuteAsync(criteria);
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync("Broadcast service(Write resettle Log): ", e);
        }
    }
}