using ThirdPartyBroadcast.Consumer.Models.Happy5;

namespace ThirdPartyBroadcast.Consumer.Services.Products.Happy5;

public interface BetTransService
{
    Task<List<BatchBetTrans>> GetBatchTransId(int match);

    Task<List<BetTrans>> GetTransId(BatchBetTrans batch);
}

public class BetTransServiceImp : BetTransService
{
    public async Task<List<BatchBetTrans>> GetBatchTransId(int match)
    {
        var batch = new List<BatchBetTrans>();
        return batch;
    }

    public async Task<List<BetTrans>> GetTransId(BatchBetTrans batch)
    {
        var betTrans = new List<BetTrans>();
        return betTrans;
    }
}