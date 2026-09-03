namespace DashBoard.Exceptions
{
    public class ZKBioConfigurationException : Exception
    {
        public ZKBioConfigurationException(string message) : base(message) { }
        public ZKBioConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
