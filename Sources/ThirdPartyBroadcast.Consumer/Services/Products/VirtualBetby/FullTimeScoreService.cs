namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface FullTimeScoreService
{
    bool CheckForAllQuarters(IReadOnlyCollection<ResourceItemModel> resources);

    bool IsFullTimeEqualToQuarterScores(IEnumerable<ResourceItemModel> quarters, ResourceItemModel fullTime);

    string GetInconsistentMessage(List<ResourceItemModel> finalResources);
}

public class FullTimeScoreServiceImp : FullTimeScoreService
{
    public bool CheckForAllQuarters(IReadOnlyCollection<ResourceItemModel> resources)
    {
        return resources.Any(item => item.Id.Equals(ResourceId.Quarter1))
               && resources.Any(item => item.Id.Equals(ResourceId.Quarter2))
               && resources.Any(item => item.Id.Equals(ResourceId.Quarter3))
               && resources.Any(item => item.Id.Equals(ResourceId.Quarter4));
    }

    public bool IsFullTimeEqualToQuarterScores(IEnumerable<ResourceItemModel> quarters, ResourceItemModel fullTime)
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

    public string GetInconsistentMessage(List<ResourceItemModel> finalResources)
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