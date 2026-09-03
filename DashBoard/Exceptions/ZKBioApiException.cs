using System.Net;

namespace DashBoard.Exceptions
{
    public class ZKBioApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public ZKBioApiException(
            HttpStatusCode statusCode,
            string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
