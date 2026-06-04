using Newtonsoft.Json;
using System.Net;

namespace Shared.Common.ExceptionHandling
{
    public class CustomExceptionDetails
    {
        /// <summary>Gets or sets the unique trace identifier which identifies the request in trace logs.</summary>
        /// <value>The trace identifier.</value>
        public string TraceIdentifier { get; set; }

        /// <summary>Gets or sets the path of the request.</summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>Gets or sets the HTTP status code.</summary>
        /// <value>The HTTP status code.</value>
        public HttpStatusCode HttpStatusCode { get; set; }

        /// <summary>Gets or sets the exception.</summary>
        /// <value>The exception.</value>
        public Exception Exception { get; internal set; }

        /// <summary>Converts to string.</summary>
        /// <returns>A <see cref="string"/> that represents this instance.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        }
    }
}

