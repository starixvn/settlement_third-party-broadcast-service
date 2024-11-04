namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface ScoreValidatorService
{
    Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources);

    string GetInconsistentMessage(List<ResourceItemModel> finalResources);
}

public class ScoreValidatorServiceImp(
    FullTimeScoreService fullTimeScoreService) : ScoreValidatorService
{
    public Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources)
    {
        var finalResources = GetMarkedList(request.Resources, currentResources);

        request.Resources = finalResources;
        var fullTime = finalResources.Find(r => r.Id == ResourceId.FullTimeResourceId);

        if (!fullTimeScoreService.CheckForAllQuarters(finalResources))
        {
            const string message = "Cannot Calculate FT due to all quarters have not been calculated or selected to calculate.";
            return GetResultMessage(1, message);
        }
        var quarters = finalResources.Where(r => r.Id != ResourceId.FullTimeResourceId
                                                 && r.Id != ResourceId.HalfTimeResourceId);

        if (!fullTimeScoreService.IsFullTimeEqualToQuarterScores(quarters, fullTime))
        {
            var message = GetInconsistentMessage(finalResources);
            return GetResultMessage(2, $"Inconsistent value {message}.");
        }

        if (fullTime != null && fullTime.Home.Equals(fullTime.Away))
        {
            var message = $"Cannot calculate FT({fullTime.Home} - {fullTime.Away}) due to FT should not be draw.";
            return GetResultMessage(3, message);
        }

        return new Result();
    }

    private static List<ResourceItemModel> GetMarkedList(IEnumerable<ResourceItemModel> newResources, IEnumerable<ResourceItemModel> currentResources)
    {
        return currentResources
            .Concat(newResources)
            .GroupBy(item => item.Id)
            .Select(g =>
                new ResourceItemModel
                {
                    Id = g.Key,
                    Home = g.Last().Home,
                    Away = g.Last().Away,
                    IsDuplicated = g.Count() > 1
                })
            .ToList();
    }

    private static Result GetResultMessage(int errorCode, string errorMessage)
    {
        return new Result
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    public string GetInconsistentMessage(List<ResourceItemModel> finalResources)
    {
        var listResults = new List<string>
        {
             ResourceId.Quarter1, ResourceId.Quarter2, ResourceId.Quarter3, ResourceId.Quarter4, ResourceId .OverTime, ResourceId.FullTimeResourceId
        };
        var messages = (from item in listResults
                        let score = finalResources.Find(r => r.Id == item)
                        where score != null
                        let title = item.Equals(ResourceId.FullTimeResourceId) ? "FT" : item
                        select $"{title}({score.Home} - {score.Away})").ToList();

        return string.Join(", ", messages);
    }
}