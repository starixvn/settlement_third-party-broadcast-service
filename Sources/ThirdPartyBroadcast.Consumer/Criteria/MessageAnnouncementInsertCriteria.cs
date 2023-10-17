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

    public int MsgType { get; set; }

    public bool IsBatch { get; set; } = true;

    public short MsgContentsType { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public override string GetSettingKey() => "";

    public override bool IsValid() => true;
}