namespace ThirdPartyBroadcast.Consumer.Constants;

public static class MessageAnnounceTemplate
{
    public const string RunningMsgEng = "Attn:[Virtual Sports] Due to technical error, \"@team_name\" [@league_name - @KO_day_month] " +
                                        "bet ID: \"@@\" taken are considered REFUNDED. Sorry for the inconvenience caused!";

    public const string RunningMsgCantonese = "請注意:[虛擬運動] 由於技術有誤, \"@team_name\" [@league_name - @KO_day_month] 注單: \"@@\" 投注金額已被退還. 不便之處 敬請見諒!";
    public const string RunningMsgMandarin = "请注意:[虚拟运动] 由于技术有误, \"@team_name\" [@league_name - @KO_day_month] 注单: \"@@\" 投注金额已被退还. 不便之处 敬请见谅!";

    public const string SettledMsgEng = "Attn:[Virtual Sports] Due to the wrong calculation, \"@team_name\" [@league_name - @KO_day_month] " +
        "bet ID: \"@@\" has been re-settled. Kindly double check your statement again. Sorry for the inconvenience caused!";

    public const string SettledMsgCantonese = "請注意:[虛擬運動] 由於成績結算有誤, \"@team_name\" [@league_name - @KO_day_month] 注單: \"@@\" 已重新結算. 請重新檢查您的帳目. 不便之處 敬請見諒!";
    public const string SettledMsgMandarin = "请注意:[虚拟运动] 由于成绩结算有误, \"@team_name\" [@league_name - @KO_day_month] 注单: \"@@\" 已重新结算. 请重新检查您的帐目. 不便之处 敬请见谅!";
}