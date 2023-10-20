using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetRadar;

public class SendNotifyBetradarCriteria : NonQueryCommand
{
    public int MatchId { get; set; }
    public int NotifyAction { get; set; }
    public bool IsParlay { get; set; }
    public short ParlayStatus { get; set; }
    public int ErrCode { get; set; }
    public string ErrMsg { get; set; }

    public override string GetSettingKey() => "NAP_Admin_SendNotify_VS_Betradar";

    public override bool IsValid() => true;
}