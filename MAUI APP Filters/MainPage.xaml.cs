using MauiAppFilter;
using Microsoft.Maui.Controls;

namespace MauiAppFilter
{
    public partial class MainPage : ContentPage
    {
        private readonly ProxyService _proxyService;

        public MainPage(ProxyService proxyService)
        {
            InitializeComponent();
            _proxyService = proxyService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _proxyService.StartAsync();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await _proxyService.StopAsync();
        }
    }
}