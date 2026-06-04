using Shared.Common.ExceptionHandling.Base;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;

namespace Shared.Common.ExceptionHandling
{
    public class CustomException : Exception, ICustomException
    {
        public string ExtraInformation { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }

        public CustomException(Exception exception) : base(exception.Message, exception)
        {
            ExtraInformation = exception.Message;
        }

        public CustomException(string message, Exception exception) : base(message, exception)
        {
            ExtraInformation = message;
        }

        public CustomException(Exception exception, HttpStatusCode statusCode) : base(exception.Message, exception)
        {
            ExtraInformation = exception.Message;
            HttpStatusCode = statusCode;
        }

        public CustomException(string message, HttpStatusCode statusCode) : base(message)
        {
            ExtraInformation = message;
            HttpStatusCode = statusCode;
        }

        public CustomException(string message) : base(message)
        {
            ExtraInformation = message;
        }


        [ExcludeFromCodeCoverage]
        protected CustomException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
