using Microsoft.Maui.Controls;
using System;
#if WINDOWS
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using static Microsoft.UI.Win32Interop;
#elif MACCATALYST
using UIKit;
using ObjCRuntime;
#endif

namespace MauiAppFilter
{
    public partial class TimeUpPage : ContentPage
    {
        public TimeUpPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

        #if WINDOWS
                    await Task.Delay(300); // Ensure window is ready
        #endif

            MakeFullScreen();

            // Simulate time usage info
            UsageInfoLabel.Text = $"You have used the PC for 15 minutes today.";
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent back navigation
            return true;
        }

        private async void OnAskForMoreTimeClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Request Sent", "Your request for more time has been sent to the administrator.", "OK");
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnOkClicked(object sender, EventArgs e)
        {
            #if WINDOWS
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "shutdown",
                                Arguments = "/l", // Logout
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                        }
                        catch (Exception ex)
                        {
                            await DisplayAlert("Error", $"Could not log out: {ex.Message}", "OK");
                        }
            #else
                        await Shell.Current.GoToAsync("//MainPage");
            #endif
        }

        private void MakeFullScreen()
        {
            #if WINDOWS
                        var window = GetParentWindow();
                        if (window != null)
                        {
                            var handle = WindowNative.GetWindowHandle(window);
                            var id = GetWindowIdFromWindow(handle);
                            var appWindow = AppWindow.GetFromWindowId(id);
                            if (appWindow != null)
                            {
                                appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                            }
                        }
            #elif MACCATALYST
                        var keyWindow = UIApplication.SharedApplication.KeyWindow;
                        if (keyWindow != null)
                        {
                            keyWindow.PerformSelector(new Selector("toggleFullScreen:"), keyWindow, 0);
                        }
            #endif
        }

        #if WINDOWS
                private Microsoft.UI.Xaml.Window? GetParentWindow()
                {
                    return this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                }
        #endif
    }
}
