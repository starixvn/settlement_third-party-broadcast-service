using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPartyBroadcast.Consumer.Extensions;

public static class ResourceItemModelExtensions
{
    public static bool TryParseScore(this ResourceItemModel result, out Score score)
    {
        score = new Score(0, 0);

        if (result == null || !int.TryParse(result.Home, out var homeScore) || !int.TryParse(result.Away, out var awayScore))
        {
            return false;
        }

        score = new Score(homeScore, awayScore);
        return true;
    }
}