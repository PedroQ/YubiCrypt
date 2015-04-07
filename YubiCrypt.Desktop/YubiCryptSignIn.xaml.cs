using MahApps.Metro.Controls;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using YubiCrypt.ClientLibrary;

namespace YubiCrypt.Desktop
{
    /// <summary>
    /// Interaction logic for YubiCryptSignIn.xaml
    /// </summary>
    public partial class YubiCryptSignIn : MetroWindow, IDisposable
    {
        string clientId = "123456";
        string clientSecret = "abcdef";
        string callbackUrl = "https://callback.com/";

        public YubiCryptSignIn()
        {
            InitializeComponent();
        }


        public event EventHandler<AccessTokenReceivedEventArgs> AccessTokenReceived;

        private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
        {
            GetAuthorizationCode();
        }

        public void HideScriptErrors(WebBrowser wb, bool Hide)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            object objComWebBrowser = fiComWebBrowser.GetValue(wb);
            if (objComWebBrowser == null) return;
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
        }

        private void MainBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            //HideScriptErrors(MainBrowser, true);
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        YubiCryptClient _client;
        private void GetAuthorizationCode()
        {
            _client = new YubiCryptClient(clientId, clientSecret, callbackUrl);

            messageText.Text = "Please sign in to your YubiCrypt account:";
            var url = _client.GetAuthorizationCodeRequestUrl(new string[] { "files", "keys" });
            MainBrowser.Navigate(url);
        }

        private async void GetAccessToken(string authorizationCode)
        {
            try
            {
                var token = await _client.GetOAuth2AccessToken(authorizationCode);

                if (this.AccessTokenReceived != null)
                    AccessTokenReceived(this, new AccessTokenReceivedEventArgs(token));

                this.Close();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void MainBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri.Host.Equals("callback.com", StringComparison.InvariantCultureIgnoreCase))
            {
                e.Cancel = true;
                MainBrowser.Visibility = System.Windows.Visibility.Hidden;
                // Query	"?code=b54e870e222648dda0f6707c5407e20da69e192fd54f46d4b8dbc3b88d32f71c"	string
                string code = e.Uri.Query.Replace("?code=", "");
                GetAccessToken(code);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_client != null) _client.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class AccessTokenReceivedEventArgs : EventArgs
    {
        public YubiCryptOAuth2Token AccessToken { get; set; }
        public AccessTokenReceivedEventArgs(YubiCryptOAuth2Token accessToken)
        {
            AccessToken = accessToken;
        }
    }
}
