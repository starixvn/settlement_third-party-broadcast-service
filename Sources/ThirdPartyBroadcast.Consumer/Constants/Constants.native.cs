namespace ThirdPartyBroadcast.Consumer.Constants;

public static class CacheKeys
{
    public const string BroadcastServiceProductWaitingCacheKey = "Broadcast-Service-Product-Waiting";
}

public static class QueueName
{
    public const string Processing = "broadcast-api-processing-event";
}

public static class EmailType
{
    public const string Credit = "Credit";

    public const string Deposit = "Deposit";
}

public static class EventStatus
{
    public const string Completed = "completed";
}

public static class ResourceId
{
    public const string FullTimeResourceId = "00";

    public const string HalfTimeResourceId = "01";
}

public static class SpName
{
    public const string VirtualSports = "NAP_Admin_Settlement_VS_180";

    public const string ProfilePush = "NAP_Admin_Settlement_Profile_Push";
}

public static class ActionId
{
    public const int BitCoinCalc = 81033;

    public const int BitCoinRefund = 81034;

    public const int BetBuilderCalc = 81012;

    public const int BetBuilderRefund = 81010;

    public const int VirtualBetRadarCalc = 187;

    public const int VirtualBetRadarRefund = 188;

    public const int LeapGamingCalc = 81043;

    public const int LeapGamingRefund = 81044;

    public const int CockFightingCalc = 81050;

    public const int CockFightingRefund = 81051;

    public const int HappyFiveCalc = 81000;

    public const int HappyFiveRefund = 81001;

    public const int NgCal = 115;

    public const int NgIsLive = 116;

    public const int NgRefund = 117;

    public const int NgCalHt = 118;

    public const int NgRefundFt = 119;
}

public static class ActionName
{
    public const string Calculate = "Cal";

    public const string ReSettle = "ReSettle";

    public const string Refund = "Refund";
}

public enum TicketType
{
    Running,
    Settled
}

public static class MsgContentType
{
    public const short Running = 105;
    public const short Settled = 110;
}

public static class StatusType
{
    public const short Running = 0;
}

public static class ContentLanguage
{
    public const string English = "en";
    public const string Cantonese = "zh-TW";
    public const string Mandarin = "zh-CN";
}

public static class SportType
{
    public const short VirtualCockFighting = 188;
}