using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Shared.Common.ExceptionHandling
{
    public class CustomProblemDetails : ProblemDetails
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

    }
}
