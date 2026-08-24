namespace DashBoard.Exceptions
{
    public class GlpiConfigurationException : Exception
    {
        public GlpiConfigurationException(string message ): base(message) { }
        public GlpiConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
