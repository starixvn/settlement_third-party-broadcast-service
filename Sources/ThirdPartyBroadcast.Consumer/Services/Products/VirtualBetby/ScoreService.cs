namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface ScoreService
{
    bool CheckForAllQuarters(IReadOnlyCollection<ResourceItemModel> resources);

    bool CheckForHalftimeQuarters(IReadOnlyCollection<ResourceItemModel> resources);

    bool IsScoreEqualToQuarterScores(IEnumerable<ResourceItemModel> quarters, ResourceItemModel score);

    string GetScoreMessage(List<ResourceItemModel> finalResources, string action);

    string ResourceName(string resourceId);

    List<ResourceItemModel> GetMarkedList(IEnumerable<ResourceItemModel> newResources, IEnumerable<ResourceItemModel> currentResources);
}

public class ScoreServiceImp : ScoreService
{
    public bool CheckForAllQuarters(IReadOnlyCollection<ResourceItemModel> resources)
    {
        return CheckForHalftimeQuarters(resources)
               && resources.Any(item => item.Id.Equals(ResourceId.Quarter3) && IsCorrectedScore(item))
               && resources.Any(item => item.Id.Equals(ResourceId.Quarter4) && IsCorrectedScore(item));
    }

    public bool CheckForHalftimeQuarters(IReadOnlyCollection<ResourceItemModel> resources)
    {
        return resources.Any(item => item.Id.Equals(ResourceId.Quarter1) && IsCorrectedScore(item))
               && resources.Any(item => item.Id.Equals(ResourceId.Quarter2) && IsCorrectedScore(item));
    }

    private static bool IsCorrectedScore(ResourceItemModel resource)
    {
        return !string.IsNullOrWhiteSpace(resource.Home) && !string.IsNullOrWhiteSpace(resource.Away);
    }

    public bool IsScoreEqualToQuarterScores(IEnumerable<ResourceItemModel> quarters, ResourceItemModel score)
    {
        var (totalHome, totalAway) = SumHomeAwayScores(quarters);
        return Convert.ToInt32(score.Home) == totalHome && Convert.ToInt32(score.Away) == totalAway;
    }

    private static (int, int) SumHomeAwayScores(IEnumerable<ResourceItemModel> quarters)
    {
        var totalHome = quarters.Sum(score => Convert.ToInt32(string.IsNullOrWhiteSpace(score.Home) ? 0 : score.Home));
        var totalAway = quarters.Sum(score => Convert.ToInt32(string.IsNullOrWhiteSpace(score.Away) ? 0 : score.Away));

        return (totalHome, totalAway);
    }

    public string GetScoreMessage(List<ResourceItemModel> finalResources, string action)
    {
        var listResults = new List<string>
        {
            ResourceId.Quarter1, ResourceId.Quarter2, ResourceId.HalfTimeResourceId, ResourceId.Quarter3, ResourceId.Quarter4, ResourceId .OverTime, ResourceId.FullTimeResourceId
        };

        var messages = (from item in listResults
                        let score = finalResources.Find(r => r.Id == item)
                        where score != null
                        let title = ResourceName(item)
                        select $"{title}{ShowScore(score, action)}").ToList();

        return string.Join(", ", messages);
    }

    private static string ShowScore(ResourceItemModel score, string action)
    {
        return string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase) ? string.Empty : $"({score.Home} - {score.Away})";
    }

    public string ResourceName(string resourceId)
    {
        if (resourceId.Equals(ResourceId.HalfTimeResourceId))
        {
            return "HT";
        }

        return resourceId.Equals(ResourceId.FullTimeResourceId) ? "FT" : resourceId;
    }

    public List<ResourceItemModel> GetMarkedList(IEnumerable<ResourceItemModel> newResources, IEnumerable<ResourceItemModel> currentResources)
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
}