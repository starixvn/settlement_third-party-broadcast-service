namespace ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;

public class MailRecipientsCriteria : CriteriaBase
{
    public string GroupName { get; set; } = "NOTIFY";

    public int SportType { get; set; } = 178;

    public override string GetSettingKey() => "NAP_Admin_MailingList_Sel";

    public override bool IsValid() => true;
}