using System;
using System.Collections.Generic;
using System.Text;

namespace Webview.Services
{
    public class AuthStorageService
    {
        private const string AccessTokenKey = "auth_access_token";
        private const string RefreshTokenKey = "auth_refresh_token";
        private const string UseBiometricsKey = "use_biometrics";

        public async Task SaveTokensAsync(
            string accessToken,
            string? refreshToken = null,
            bool enableBiometrics = false)
        {
            if (!string.IsNullOrEmpty(accessToken))
                await SecureStorage.SetAsync(AccessTokenKey, accessToken);

            if (!string.IsNullOrEmpty(refreshToken))
                await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);

            Preferences.Set(UseBiometricsKey, enableBiometrics);
        }

        public bool IsBiometricUnlockEnabled()
            => Preferences.Get(UseBiometricsKey, false);

        public async Task<(string accessToken, string? refreshToken)?> TryGetTokensAsync()
        {
            var access = await SecureStorage.GetAsync(AccessTokenKey);
            if (string.IsNullOrEmpty(access))
                return null;

            var refresh = await SecureStorage.GetAsync(RefreshTokenKey);
            return (access, refresh);
        }

        public void DisableBiometrics()
        {
            Preferences.Set(UseBiometricsKey, false);
        }

        public void ClearAll()
        {
            SecureStorage.Remove(AccessTokenKey);
            SecureStorage.Remove(RefreshTokenKey);
            Preferences.Remove(UseBiometricsKey);
        }
    }
}
