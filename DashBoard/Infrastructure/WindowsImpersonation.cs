using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace DashBoard.Infrastructure
{
    // Lets the app authenticate to SQL Server (Windows auth) as a service
    // account that differs from the identity running the process. Kerberos
    // SSO always wins over a Windows Credential Manager entry when the
    // caller already has a ticket for their own domain session, so the only
    // way to present a different Windows identity is to actually log on as
    // that account (LOGON32_LOGON_NEW_CREDENTIALS is the same logon type
    // `runas /netonly` uses: it doesn't require "log on locally" rights on
    // this machine, it only affects outbound network authentication).
    public static class WindowsImpersonation
    {
        private const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
        private const int LOGON32_PROVIDER_WINNT50 = 3;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(
            string lpszUsername,
            string? lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out SafeAccessTokenHandle phToken);

        public static void RunAsServiceAccount(string? domain, string username, string password, Action action)
        {
            if (!LogonUser(username, domain, password, LOGON32_LOGON_NEW_CREDENTIALS, LOGON32_PROVIDER_WINNT50, out var tokenHandle))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            using (tokenHandle)
            {
                WindowsIdentity.RunImpersonated(tokenHandle, action);
            }
        }
    }
}
