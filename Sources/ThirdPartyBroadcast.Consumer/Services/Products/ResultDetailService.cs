using ThirdPartyBroadcast.Consumer.Criteria.VirtualBetby;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

public interface ResultDetailService
{
    Task<List<ResourceItemModel>> GetResultDetail3rd(int matchId);
}

public class ResultDetailServiceImp(IDynamicRepository repository) : ResultDetailService
{
    public async Task<List<ResourceItemModel>> GetResultDetail3rd(int matchId)
    {
        var criteria = new GetResultDetail3rdCriteria { MatchId = matchId };
        return (await repository.FetchAsync<ResourceItemModel>(criteria)).ToList();
    }
}