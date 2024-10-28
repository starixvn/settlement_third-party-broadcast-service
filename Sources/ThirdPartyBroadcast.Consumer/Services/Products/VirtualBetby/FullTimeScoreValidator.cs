namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface FullTimeScoreValidator
{
    Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources);
}

public class FullTimeScoreValidatorImp : FullTimeScoreValidator
{
    public Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources)
    {
        var finalResources = currentResources
            .Concat(request.Resources)
            .GroupBy(item => item.Id)
            .Select(group => group.Last())
            .ToList();

        request.Resources = finalResources;
        var fullTime = finalResources.Find(r => r.Id == ResourceId.FullTimeResourceId);

        if (!CheckForAllQuarters(finalResources))
        {
            return new Result
            {
                ErrorCode = 1,
                ErrorMessage = "Cannot Calculate FT due to all quarters have not been calculated or selected to calculate."
            };
        }
        var quarters = finalResources.Where(r => r.Id != ResourceId.FullTimeResourceId
                                                 && r.Id != ResourceId.HalfTimeResourceId);

        if (!IsFullTimeEqualToQuarterScores(quarters, fullTime))
        {
            var message = GetInconsistentMessage(finalResources);
            return new Result
            {
                ErrorCode = 2,
                ErrorMessage = $"Inconsistent value {message}."
            };
        }

        if (fullTime != null && fullTime.Home.Equals(fullTime.Away))
        {
            return new Result
            {
                ErrorCode = 3,
                ErrorMessage = $"Cannot calculate FT({fullTime.Home} - {fullTime.Away}) due to FT should not be draw."
            };
        }

        return new Result();
    }

    private static bool CheckForAllQuarters(IReadOnlyCollection<ResourceItemModel> resources)
    {
        return resources.Any(item => item.Id.Equals(ResourceId.Quarter1))
               && resources.Any(item => item.Id.Equals(ResourceId.Quarter2))
               && resources.Any(item => item.Id.Equals(ResourceId.Quarter3))
               && resources.Any(item => item.Id.Equals(ResourceId.Quarter4));
    }

    private static bool IsFullTimeEqualToQuarterScores(IEnumerable<ResourceItemModel> quarters, ResourceItemModel fullTime)
    {
        var (totalHome, totalAway) = SumHomeAwayScores(quarters);
        return Convert.ToInt32(fullTime.Home) == totalHome && Convert.ToInt32(fullTime.Away) == totalAway;
    }

    private static (int, int) SumHomeAwayScores(IEnumerable<ResourceItemModel> scores)
    {
        var totalHome = scores.Sum(score => Convert.ToInt32(score.Home));
        var totalAway = scores.Sum(score => Convert.ToInt32(score.Away));

        return (totalHome, totalAway);
    }

    private static string GetInconsistentMessage(List<ResourceItemModel> finalResources)
    {
        var listResults = new List<string>
        {
            ResourceId.FullTimeResourceId, ResourceId.Quarter1, ResourceId.Quarter2, ResourceId.Quarter3, ResourceId.Quarter4, ResourceId .OverTime
        };
        var messages = (from item in listResults
                        let score = finalResources.Find(r => r.Id == item)
                        where score != null
                        let title = item.Equals(ResourceId.FullTimeResourceId) ? "FT" : item
                        select $"{title}({score.Home} - {score.Away})").ToList();

        return string.Join(", ", messages);
    }
}