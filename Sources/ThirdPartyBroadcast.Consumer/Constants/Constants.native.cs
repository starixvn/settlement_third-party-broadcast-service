namespace ThirdPartyBroadcast.Consumer.Constants;

public static class CacheKeys
{
    public const string BroadcastServiceProductWaitingCacheKey = "Broadcast-Service-Product-Waiting";

    public const string BroadcastSettleUnderMaintenanceCacheKey = "Settlement-Broadcast-Settle-UnderMaintenance";
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

    public const string Running = "running";
}

public static class ResourceId
{
    public const string Quarter1 = "Q1";
    public const string Quarter2 = "Q2";
    public const string Quarter3 = "Q3";
    public const string Quarter4 = "Q4";
    public const string OverTime = "OT";

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

    public const int VirtualBetRadarCalc = 187;

    public const int VirtualBetRadarRefund = 188;

    public const int CalculateAll = 81043;

    public const int RefundAll = 81044;

    public const int CockFightingCalc = 81050;

    public const int CockFightingRefund = 81051;

    public const int HappyFiveCalc = 81000;

    public const int HappyFiveRefund = 81001;

    public const int NgCal = 115;

    public const int NgIsLive = 116;

    public const int NgRefund = 117;

    public const int NgCalHt = 118;

    public const int NgRefundFt = 119;

    public const int VirtualSportFiveCal = 81053;

    public const int VirtualSportFiveRefund = 81054;

    public const int VirtualBetbyCal = 81055;

    public const int VirtualBetbyRefund = 81056;
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
    public const string NewMandarin = "zh-CHS";
}

public static class SportType
{
    public const short VirtualCockFighting = 188;

    public const int VirtualBetbyBasketball = 134;

    public const int HappyFive = 164;

    public const int NumberGame = 161;
}

public static class ErrorCode
{
    public const int NotExistValue = 1_000_003;
}

public static class ErrorMessage
{
    public const string NotExistValue = "The input score values are not valid";

    public const string Inconsistent = "HT({0}) is inconsistent with FT({1})";

    public const string NotExistedGameSession = "The input GameSession values are not valid";
}