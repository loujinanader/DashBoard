using System.Net;

namespace DashBoard.Exceptions
{
    public class GlpiApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public GlpiApiException(
            HttpStatusCode statusCode,
            string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}