namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;

public interface VirtualRacingService
{
    Task<Result> Settle(VirtualRacingRequest request);
}

public class VirtualRacingServiceImp : VirtualRacingService
{
    public async Task<Result> Settle(VirtualRacingRequest request)
    {
        return new Result();
    }
}