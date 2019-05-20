
namespace Scribe.Connector.Common
{
    using System;
    using System.Security.Principal;
    using System.Runtime.InteropServices;
    using System.ComponentModel;

    internal class Impersonator : IDisposable
    {
        #region Constants, Private
        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_PROVIDER_DEFAULT = 0;
        #endregion Constants, Private

        #region Fields, Private
        private WindowsImpersonationContext impersonationContext = null;
        #endregion Fields, Private

        internal class NativeMethods
        {
            private NativeMethods() { }

            // DllImports
             [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int LogonUser(string lpszUserName, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

            [DllImport("advapi32.dll", SetLastError = true)]
            internal static extern int DuplicateToken(IntPtr hToken, int impersonationLevel, ref IntPtr hNewToken);

            [DllImport("advapi32.dll", SetLastError = true)]
            internal static extern bool RevertToSelf();

            [DllImport("kernel32.dll")]
            internal static extern bool CloseHandle(IntPtr handle); 
        }

        private Impersonator()
        {
            // Hide
        }

        #region Methods, Public
        public Impersonator(bool impersonate, string domainName, string userName, string password)
        {
            if (impersonate)
            {
                ImpersonateValidUser(userName, domainName, password);
            }
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // free managed resources  
            if (impersonationContext != null)
            {
                impersonationContext.Undo();
                impersonationContext = null;
            }
        }

        #endregion Methods, Public

        #region Methods, Private

        private void ImpersonateValidUser(string userName, string domain, string password)
        {
            var token = IntPtr.Zero;
            var tokenDuplicate = IntPtr.Zero;

            try
            {
                if (!NativeMethods.RevertToSelf())
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (NativeMethods.LogonUser(userName, domain, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, ref token) == 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (NativeMethods.DuplicateToken(token, 2, ref tokenDuplicate) == 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
                impersonationContext = tempWindowsIdentity.Impersonate();
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(token);
                }
                if (tokenDuplicate != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(tokenDuplicate);
                }
            }
        }
        #endregion
    }
}