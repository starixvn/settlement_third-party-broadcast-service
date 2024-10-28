namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface GenerateMatchInfoService
{
    string GetBetStatus(VirtualBetbyRequest request, string betStatus);

    string GetEventStatus(VirtualBetbyRequest request, string eventStatus);
}

public class GenerateMatchInfoServiceImp : GenerateMatchInfoService
{
    public string GetBetStatus(VirtualBetbyRequest request, string betStatus)
    {
        var ftResult = request.Resources.Find(resources => resources.Id == ResourceId.FullTimeResourceId);
        var isRefund = string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase);
        return isRefund && ftResult is not null
            ? ActionName.Refund
            : betStatus;
    }

    public string GetEventStatus(VirtualBetbyRequest request, string eventStatus)
    {
        if (string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return EventStatus.Completed;
        }

        var ftResult = request.Resources.Find(resources => resources.Id == ResourceId.FullTimeResourceId);
        var isCalc = string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase);

        return isCalc && ftResult is not null
            ? EventStatus.Completed
            : eventStatus;
    }
}