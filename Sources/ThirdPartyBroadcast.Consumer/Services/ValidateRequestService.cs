using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPartyBroadcast.Consumer.Services
{
    public class ValidateRequestService
    {
        private bool IsValidScore(ResourceItemModel result, string action)
        {
            if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
            {
                return result is { Home: "", Away: "" };
            }
            return result != null && int.TryParse(result.Home, out _) && int.TryParse(result.Away, out _);
        }
    }
}