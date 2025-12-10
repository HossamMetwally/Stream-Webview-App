using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Webview.Services
{
    public class BiometricService : IBiometricService
    {
        public async Task<bool> IsAvailableAsync()
        {
            return await CrossFingerprint.Current.IsAvailableAsync();
        }

        public async Task<bool> AuthenticateAsync(string reason)
        {
            var config = new AuthenticationRequestConfiguration("Unlock App", reason)
            {
                AllowAlternativeAuthentication = true // PIN/Passcode fallback
            };

            var result = await CrossFingerprint.Current.AuthenticateAsync(config);
            return result.Authenticated;
        }
    }
}
