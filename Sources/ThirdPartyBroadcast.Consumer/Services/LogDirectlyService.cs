using Fanex.Logging;
using Fanex.Logging.Extensions;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface LogDirectlyService
{
    Task WriteLog(int matchId, string message);

    Task HandleLog(Result result, string productType, string data);
}

public class LogDirectlyServiceImp : LogDirectlyService
{
    private const string ErrorMessage = "Error from Settlement ThirdPartyBroadcast Service";

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
            await LogService.ErrorAsync($"{ErrorMessage} (Write Directly):", e);
        }
    }

    public async Task HandleLog(Result result, string productType, string data)
    {
        if (!result.IsSuccessful)
        {
            var info = new Dictionary<string, object>
            {
                { "Product Name", GetProductName(productType) },
                { "Match Info", data },
                { "Details", result.ErrorMessage },
            };

            var logMessage = new LogMessage(ErrorMessage)
                .AddSection("Error Info", info)
                .ToString();

            await LogService.ErrorAsync(logMessage);
        }
    }

    private static string GetProductName(string code)
    {
        foreach (var field in typeof(ProductType).GetFields())
        {
            if (string.Equals((string)field.GetValue(null), code))
            {
                return field.Name;
            }
        }

        return string.Empty;
    }
}