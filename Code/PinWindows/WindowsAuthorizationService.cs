namespace PinWindows
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security.Principal;

    /// <summary>
    /// Service containg methods for querying Windows
    /// authorization for the current user.
    /// </summary>
    public class WindowsAuthorizationService
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

        /// <summary>
        /// Passed to <see cref="GetTokenInformation"/> to specify what
        /// information about the token to return.
        /// </summary>
        enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUiAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        /// <summary>
        /// The elevation type for a user token.
        /// </summary>
        enum TokenElevationType
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        /// <summary>
        /// Determines whether the current user is an administrator
        /// on the local machine.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the user is a local admin; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>This will check if the current user is running as an admin. Additionally,
        /// it will also check for UAC to see if the user has a split token that means they
        /// can elevate the process. This is not 100% reliable but should suffice for systems
        /// where UAC is enabled.</remarks>
        public static bool IsUserALocalAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                if (identity == null) throw new InvalidOperationException("Couldn't get the current user identity");
                var principal = new WindowsPrincipal(identity);

                // Check if this user has the Administrator role. If they do, return immediately.
                // If UAC is on, and the process is not elevated, then this will actually return false.
                if (principal.IsInRole(WindowsBuiltInRole.Administrator)) return true;

                // If we're not running in Vista onwards, we don't have to worry about checking for UAC.
                if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major < 6)
                {
                    Trace.TraceInformation("Operating system {0} does not support UAC, so skipping elevation check.", Environment.OSVersion);
                    return false;
                }

                int tokenInfLength = Marshal.SizeOf(typeof(int));
                IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);

                try
                {
                    var token = identity.Token;

                    Trace.TraceInformation("Getting token elevation information");

                    var result = GetTokenInformation(token, TokenInformationClass.TokenElevationType, tokenInformation, tokenInfLength, out tokenInfLength);

                    if (!result)
                    {
                        var exception = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                        throw new InvalidOperationException("Couldn't get token information", exception);
                    }

                    var elevationType = (TokenElevationType)Marshal.ReadInt32(tokenInformation);

                    Trace.TraceInformation("Token elevation type: {0}", elevationType);

                    switch (elevationType)
                    {
                        case TokenElevationType.TokenElevationTypeDefault:
                            Trace.TraceInformation("TokenElevationTypeDefault - User is not using a split token, so they cannot elevate.");
                            return false;

                        case TokenElevationType.TokenElevationTypeFull:
                            Trace.TraceInformation("TokenElevationTypeFull - User has a split token, and the process is running elevated. Assuming they're an administrator.");
                            return true;

                        case TokenElevationType.TokenElevationTypeLimited:
                            Trace.TraceInformation("TokenElevationTypeLimited - User has a split token, but the process is not running elevated. Assuming they're an administrator.");
                            return true;

                        default:
                            Trace.TraceInformation("Unknown token elevation type.");
                            return false;
                    }
                }
                finally
                {
                    if (tokenInformation != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tokenInformation);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Exception when trying to detect if the user is an administrator", ex);
                return false;
            }
        }
    }
}