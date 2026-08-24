using System.Net;
namespace DashBoard.Exceptions
{
    public class GlpiAuthenticationException : Exception
    {
        public GlpiAuthenticationException(string message) : base(message) { }

    }
}
