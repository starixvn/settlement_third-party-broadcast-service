using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class MessageAnnouncementInsertCriteria : NonQueryCommand
{
    public int? MessageId { get; set; }

    [SpParam(SqlParamName = "Content_en")]
    public string Content_en { get; set; }

    [SpParam(SqlParamName = "Content_ch")]
    public string Content_ch { get; set; }

    [SpParam(SqlParamName = "Content_cs")]
    public string Content_cs { get; set; }

    public int MatchId { get; set; }

    public bool IsSticky { get; set; }

    public short MsgType { get; set; } = 1;

    public bool IsBatch { get; set; } = true;

    public short MsgContentsType { get; set; }
    public int Creator { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public override string GetSettingKey() => "NAP_SpecialDelete_Message_Insert";

    public override bool IsValid() => true;
}