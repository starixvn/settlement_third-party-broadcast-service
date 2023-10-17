using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class MessageUploadCriteria : NonQueryCommand
{
    public string XMLMessage { get; set; }
    public int CreatorId { get; set; }
    public int ErrorCode { get; set; }
    public string ErrorMsg { get; set; }

    public override string GetSettingKey() => "NAP_SpecialDelete_BatchMessage_Upload";

    public override bool IsValid() => true;
}