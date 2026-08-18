namespace DashBoard.Service
{
    // Thrown when GLPI calls can't proceed because there's no valid stored
    // refresh token yet (never bootstrapped, or GLPI rejected it as expired/revoked).
    // The fix is always the same: visit /auth/glpi/login to (re)authorize.
    public class GlpiNotAuthorizedException : Exception
    {
        public GlpiNotAuthorizedException(string message) : base(message)
        {
        }
    }
}
