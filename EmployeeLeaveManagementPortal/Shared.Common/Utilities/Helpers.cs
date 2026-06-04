using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Utilities
{
    public class Helpers
    {
        public static Guid GetLoggedInUserId(ClaimsPrincipal principal)
        {
            return Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out Guid userId) ?
                    userId : Guid.Empty;
        }
    }
}
