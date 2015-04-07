using YubiCrypt.Mobile.WindowsPhone.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YubiCrypt.ClientLibrary;
using Windows.Storage;
using System.Threading.Tasks;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace YubiCrypt.Mobile.WindowsPhone
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private StorageFolder localStorageFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
        public Settings()
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
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            await LoadSettings();
        }

        private async Task LoadSettings()
        {
            YubiCryptOAuth2Token storedAccessToken = SettingsHelper.LoadSetting<YubiCryptOAuth2Token>(Consts.SETTINGS_ACCESS_TOKEN_KEY);
            TFTokenData storedTokenSecretData = SettingsHelper.LoadSetting<TFTokenData>(Consts.SETTINGS_TOKEN_SECRET_KEY);
            var localStoredFiles = await localStorageFolder.GetFilesAsync();

            if (storedAccessToken != null)
            {
                authenticationInfoText.Text = string.Format("Access token expires: {0}", storedAccessToken.Expires);
                DeleteTokenButton.IsEnabled = true;
            }
            else
            {
                authenticationInfoText.Text = "No access token.";
                DeleteTokenButton.IsEnabled = false;
            }

            if (storedTokenSecretData != null)
            {
                keyStorageInfoText.Text = string.Format("Storing Secret Key for token with Serial# {0}.", storedTokenSecretData.SerialNumber);
                DeleteKeyButton.IsEnabled = true;
            }
            else
            {
                keyStorageInfoText.Text = "No stored token information";
                DeleteKeyButton.IsEnabled = false;
            }

            DeleteLocalFilesButton.IsEnabled = false;
            LocalFileStorageInfo.Text = string.Format("{0} decrypted file(s) stored locally.", localStoredFiles.Count);
            if (localStoredFiles.Count > 0)
                DeleteLocalFilesButton.IsEnabled = true;
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

        private async void DeleteTokenButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsHelper.DeleteSetting(Consts.SETTINGS_ACCESS_TOKEN_KEY);
            await LoadSettings();
        }

        private async void DeleteKeyButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsHelper.DeleteSetting(Consts.SETTINGS_TOKEN_SECRET_KEY);
            await LoadSettings();
        }

        private async void DeleteLocalFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var localStoredFiles = await localStorageFolder.GetFilesAsync();
            foreach (var item in localStoredFiles)
            {
                await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            await LoadSettings();
        }
    }
}
