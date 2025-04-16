using System.Diagnostics;
using System.Net;
using System.Text;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Microsoft.Maui.Dispatching;
using Microsoft.Extensions.Configuration;
using MauiAppFilter.Models;
#if WINDOWS
using Microsoft.Win32;
using System.Runtime.InteropServices;
#elif MACCATALYST
using System.Diagnostics;
#endif

namespace MauiAppFilter
{
    public class ProxyService : IDisposable
    {
        private readonly ProxyServer _proxyServer;
        private readonly ExplicitProxyEndPoint _endPoint;
        private readonly IDispatcher _dispatcher;
        private readonly IConfiguration _configuration;
        private Settings _settings;
        private bool _isDisposed;

        public string CurrentUserId { get; set; }
        public string CurrentToken { get; set; }
        public string BaseUrl { get; set; } = "https://test.example";

        public ProxyService(IDispatcher dispatcher, IConfiguration configuration)
        {
            _proxyServer = new ProxyServer();
            _endPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, true);
            _dispatcher = dispatcher;
            _configuration = configuration;
            LoadSettings();
        }

        public async Task StartAsync()
        {
            try
            {
                _proxyServer.BeforeRequest += OnRequest;
                _proxyServer.BeforeResponse += OnResponse;
                _proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
                _proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;

                _proxyServer.AddEndPoint(_endPoint);
                _proxyServer.Start();

                await SetSystemProxyAsync(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Proxy start failed: {ex}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                await SetSystemProxyAsync(false);

                _proxyServer.BeforeRequest -= OnRequest;
                _proxyServer.BeforeResponse -= OnResponse;
                _proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
                _proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

                _proxyServer.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Proxy stop failed: {ex}");
                throw;
            }
        }

        private async Task OnRequest(object sender, SessionEventArgs e)
        {
            var url = e.HttpClient.Request.Url;
            Debug.WriteLine($"Processing request: {url}");

            if (!IsUrlAllowed(url))
            {
                await _dispatcher.DispatchAsync(async () =>
                {
                    await Shell.Current.GoToAsync("//TimeUpPage");
                });

                if (!string.IsNullOrEmpty(CurrentUserId) && !string.IsNullOrEmpty(CurrentToken))
                {
                    var encoded = EncodeToBase64(
                        $"<x><user>{CurrentUserId}</user>" +
                        $"<url>{WebUtility.HtmlEncode(url)}</url>" +
                        $"<token>{CurrentToken}</token></x>");

                    e.Redirect($"{BaseUrl}/Allow?xd={encoded}");
                }
                else
                {
                    e.Ok("Website Blocked");
                }
            }
        }

        private Task OnResponse(object sender, SessionEventArgs e) => Task.CompletedTask;
        private Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            e.IsValid = true;
            return Task.CompletedTask;
        }
        private Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e) => Task.CompletedTask;

        #region Proxy Configuration
        private async Task SetSystemProxyAsync(bool enable)
        {
#if WINDOWS
            await SetWindowsProxyAsync(enable);
#elif MACCATALYST
            await SetMacProxyAsync(enable);
#endif
        }

#if WINDOWS
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        private async Task SetWindowsProxyAsync(bool enable)
        {
            const string key = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            
            try
            {
                Registry.SetValue(key, "ProxyEnable", enable ? 1 : 0, RegistryValueKind.DWord);
                if (enable) Registry.SetValue(key, "ProxyServer", "127.0.0.1:8000", RegistryValueKind.String);
                
                // Refresh settings
                InternetSetOption(IntPtr.Zero, 39, IntPtr.Zero, 0); // INTERNET_OPTION_SETTINGS_CHANGED
                InternetSetOption(IntPtr.Zero, 37, IntPtr.Zero, 0); // INTERNET_OPTION_REFRESH
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Windows proxy error: {ex}");
                throw;
            }
        }
#endif

#if MACCATALYST
        private async Task SetMacProxyAsync(bool enable)
        {
            var interfaceName = await GetNetworkInterfaceAsync();
            if (string.IsNullOrEmpty(interfaceName)) return;

            var commands = new[]
            {
                $"networksetup -setwebproxy {interfaceName} 127.0.0.1 8000",
                $"networksetup -setsecurewebproxy {interfaceName} 127.0.0.1 8000",
                $"networksetup -setwebproxystate {interfaceName} {(enable ? "on" : "off")}",
                $"networksetup -setsecurewebproxystate {interfaceName} {(enable ? "on" : "off")}"
            };

            foreach (var cmd in commands)
            {
                try { await ExecuteShellCommandAsync(cmd); }
                catch (Exception ex) { Debug.WriteLine($"macOS command failed: {cmd} - {ex}"); }
            }
        }

        private async Task<string> GetNetworkInterfaceAsync()
        {
            try
            {
                var result = await ExecuteShellCommandAsync(
                    "networksetup -listallnetworkservices | grep -Ei '(Wi-Fi|Ethernet)' | head -1");
                return result.Trim();
            }
            catch { return "Wi-Fi"; } // Fallback
        }

        private async Task<string> ExecuteShellCommandAsync(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }
#endif
        #endregion

        #region Helpers
        private bool IsUrlAllowed(string url)
        {
            if (_settings?.AllowedDomains == null) return true;

            try
            {
                var host = new Uri(url).Host;
                return _settings.AllowedDomains.Contains(host) && IsTimeAllowed();
            }
            catch { return false; }
        }

        private bool IsTimeAllowed()
        {
            if (_settings?.TimeSlots == null) return true;

            var now = DateTime.Now.TimeOfDay;
            return _settings.TimeSlots.Any(s => now >= s.StartTime && now <= s.EndTime);
        }

        private static string EncodeToBase64(string input) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(input));

        private void LoadSettings() =>
            _settings = _configuration.GetSection("Settings").Get<Settings>();

        public void Dispose()
        {
            if (_isDisposed) return;
            StopAsync().Wait();
            _proxyServer.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}