using System;
using System.Collections.Generic;
using System.Text;

namespace Webview.Services
{
    public interface IBiometricService
    {
        Task<bool> IsAvailableAsync();
        Task<bool> AuthenticateAsync(string reason);
    }
}
