using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace ThirdPartyBroadcast.Consumer.Constants;

public static class MsgContentType
{
    public const short Running = 105;
    public const short Settled = 101;
}