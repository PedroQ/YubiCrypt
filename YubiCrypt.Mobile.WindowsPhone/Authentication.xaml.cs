using NdefLibrary.Ndef;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.Proximity;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using YubiCrypt.ClientLibrary;
using YubiCrypt.Mobile.WindowsPhone.Common;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace YubiCrypt.Mobile.WindowsPhone
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Authentication : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private YubiCryptClient _client;

        private ProximityDevice nfcDevice;
        private long nfcNDEFSubscriptionId;

        public Authentication()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            _client = e.NavigationParameter as YubiCryptClient;
            if (_client == null)
                return;

            var authUrl = _client.GetAuthorizationCodeRequestUrl(new string[] { "files", "keys" });

            browserView.Navigate(new Uri(authUrl));
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }
        private async void browserView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri.OriginalString.StartsWith("https://callback.com/"))
            {
                args.Cancel = true;
                await ToggleProgressBar(false);

                string code = args.Uri.Query.Replace("?code=", "");
                await GetAccessToken(code);

                if (Frame.CanGoBack)
                    Frame.GoBack();
            }
            await ToggleProgressBar(true);
        }

        private static async Task ToggleProgressBar(bool show)
        {
            StatusBarProgressIndicator progressbar = StatusBar.GetForCurrentView().ProgressIndicator;
            if (show)
            {
                progressbar.Text = "Loading...";
                await progressbar.ShowAsync();
            }
            else
            {
                progressbar.Text = string.Empty;
                await progressbar.HideAsync();
            }
        }

        private async void browserView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            await ToggleProgressBar(false);

            if (args.Uri.OriginalString.StartsWith("https://yubicrypt.azurewebsites.net/Account/VerifyCode?Provider=Yubikey"))
            {
                var dialog = new MessageDialog("Please touch your Yubikey NEO to complete the authentication process.", "Two-factor Authentication");
                await dialog.ShowAsync();

                nfcDevice = ProximityDevice.GetDefault();

                if (nfcDevice == null)
                    return;

                nfcNDEFSubscriptionId = nfcDevice.SubscribeForMessage("NDEF", messageReceivedHandler);
                nfcDevice.DeviceArrived += nfcDevice_DeviceArrived;
            }
        }

        private async Task FillOTPCodeInput(string yubikeyOTP)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await browserView.InvokeScriptAsync("eval", new string[] { "document.getElementById('Code').value = '" + yubikeyOTP + "';" });
            });
        }

        private async Task GetAccessToken(string authorizationCode)
        {
            try
            {
                var token = await _client.GetOAuth2AccessToken(authorizationCode);

                SettingsHelper.SaveSetting("YubiCryptAccessToken", token);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void nfcDevice_DeviceArrived(ProximityDevice sender)
        {
            Debug.WriteLine("NFC TAG");
        }

        private async void messageReceivedHandler(ProximityDevice device, ProximityMessage message)
        {
            var rawMsg = message.Data.ToArray();

            var ndefMessage = NdefMessage.FromByteArray(rawMsg);

            foreach (NdefRecord record in ndefMessage)
            {
                if (record.CheckSpecializedType(false) == typeof(NdefTextRecord))
                {
                    var spRecord = new NdefTextRecord(record);
                    var receivedOTP = spRecord.Text.Replace("YubiCryptOTP=", string.Empty);
                    await FillOTPCodeInput(receivedOTP);

                    UnsubscribeNFC(nfcNDEFSubscriptionId);

                }
            }
        }

        private void UnsubscribeNFC(long subscritpionId)
        {
            
            if (nfcDevice == null)
                return;
            nfcDevice.StopSubscribingForMessage(subscritpionId);
            nfcDevice.DeviceArrived -= nfcDevice_DeviceArrived;
            subscritpionId = 0;
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

    }
}
