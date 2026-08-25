namespace DashBoard.Exceptions
{
    public class GlpiNotFoundException : Exception
    {
        public GlpiNotFoundException(string message) : base(message) { }
        public GlpiNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
