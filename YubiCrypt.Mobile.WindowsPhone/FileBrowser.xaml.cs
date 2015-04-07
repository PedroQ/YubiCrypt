using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using YubiCrypt.ClientLibrary;
using YubiCrypt.Mobile.WindowsPhone.Common;
using YubiCrypt.Mobile.WindowsPhone.Data;
using YubiCrypt.Mobile.WindowsPhone.Dialogs;

namespace YubiCrypt.Mobile.WindowsPhone
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileBrowser : Page
    {

        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        private readonly string clientId = "123456";
        private readonly string clientSecret = "abcdef";
        private readonly string callbackUrl = "https://callback.com/";

        private readonly YubiCryptEngine engine;

        private YubiCryptClient client;

        public FileBrowser()
        {
            this.InitializeComponent();

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            engine = new YubiCryptEngine(typeof(App).GetTypeInfo().Assembly);
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


            await RefreshUI();
        }

        private async Task RefreshUI()
        {
            YubiCryptOAuth2Token storedAccessToken = SettingsHelper.LoadSetting<YubiCryptOAuth2Token>(Consts.SETTINGS_ACCESS_TOKEN_KEY);
            if (CheckAccessToken(storedAccessToken))
            {
                client = new YubiCryptClient(clientId, clientSecret, storedAccessToken);
                if (!IsTokenSecretAvailable())
                {
                    await TokenSecretFirstTimeSetup();
                    return;
                }
                var secret = SettingsHelper.LoadSetting<TFTokenData>(Consts.SETTINGS_TOKEN_SECRET_KEY);
                await LoadFileListAsync();
            }
            else
            {
                await RenewAccessToken();
            }
        }



        private async Task<bool> TokenSecretFirstTimeSetup()
        {
            var noTokenMessage = new MessageDialog(this.resourceLoader.GetString("Message_NoTokenSecret"));
            await noTokenMessage.ShowAsync();

            var passphraseDialog = new RetrieveTokenSecretDialog();
            ContentDialogResult passphraseDialogResult = await passphraseDialog.ShowAsync();

            if (passphraseDialogResult == ContentDialogResult.Primary)
            {
                await ToggleProgressBar(true);
                var tokenStoredSecret = await client.GetTokenStoredSecret(passphraseDialog.TokenGeneratedOTP);
                TFTokenData tokenSecret = new TFTokenData();
                tokenSecret.SerialNumber = tokenStoredSecret.TokenSerialNumber;
                await Task.Run(() =>
                {
                    tokenSecret.SecretKey = engine.DecryptTokenSecret(tokenStoredSecret.TFKDEncryptedSecret, tokenStoredSecret.TFKDEncryptionSalt, passphraseDialog.Passphrase);
                });
                SettingsHelper.SaveSetting(Consts.SETTINGS_TOKEN_SECRET_KEY, tokenSecret);
                await ToggleProgressBar(false);
                return true;
            }

            return false;
        }

        private bool IsTokenSecretAvailable()
        {
            return SettingsHelper.SettingsExists(Consts.SETTINGS_TOKEN_SECRET_KEY);
        }

        private async Task LoadFileListAsync()
        {
            await ToggleProgressBar(true);

            var fileList = await client.GetFiles();
            var fileDataList = new List<YCFileData>();
            foreach (var file in fileList)
            {
                var fileData = new YCFileData(file.FileName, file.Size, file.Modified, file.InternalName);
                fileDataList.Add(fileData);
            }

            this.DefaultViewModel["Contents"] = fileDataList;

            await ToggleProgressBar(false);

        }

        private bool CheckAccessToken(YubiCryptOAuth2Token storedAccessToken)
        {
            if (storedAccessToken == null || storedAccessToken.Expires < DateTime.Now)
                return false;
            return true;
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

        private async Task RenewAccessToken()
        {
            client = new YubiCryptClient(clientId, clientSecret, callbackUrl);

            var noTokenMessage = new MessageDialog(this.resourceLoader.GetString("Message_NoAccessToken"));
            await noTokenMessage.ShowAsync();
            if (!Frame.Navigate(typeof(Authentication), client))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
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
            // TODO: Save the unique state of the page here.
        }

        /// <summary>
        /// Shows the details of a clicked group in the <see cref="SectionPage"/>.
        /// </summary>
        private void GroupSection_ItemClick(object sender, ItemClickEventArgs e)
        {
            //var groupId = ((SampleDataGroup)e.ClickedItem).UniqueId;
            //if (!Frame.Navigate(typeof(SectionPage), groupId))
            //{
            //    throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            //}
        }

        /// <summary>
        /// Shows the details of an item clicked on in the <see cref="ItemPage"/>
        /// </summary>
        private async void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            //var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            //if (!Frame.Navigate(typeof(ItemPage), itemId))
            //{
            //    throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            //}
            Exception exception = null;
            try
            {
                var passphraseDialog = new PassphraseInputDialog();
                ContentDialogResult passphraseDialogResult = await passphraseDialog.ShowAsync();

                if (passphraseDialogResult == ContentDialogResult.Primary)
                {
                    var passphrase = passphraseDialog.Passphrase;
                    var fileData = (YCFileData)e.ClickedItem;

                    StorageFolder tempFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                    StorageFile encryptedFile = await tempFolder.CreateFileAsync(fileData.InternalName, CreationCollisionOption.ReplaceExisting);
                    StorageFile decryptedFile = await tempFolder.CreateFileAsync(fileData.FileName, CreationCollisionOption.ReplaceExisting);

                    using (var encryptedFileStream = await encryptedFile.OpenStreamForWriteAsync())
                    {
                        await client.GetFile(fileData.InternalName, encryptedFileStream);
                        // reset encrypted file tape head
                        encryptedFileStream.Seek(0, SeekOrigin.Begin);

                        using (var decryptedFileStream = await decryptedFile.OpenStreamForWriteAsync())
                        {
                            engine.DecryptFile(encryptedFileStream, decryptedFileStream, passphrase);
                        }
                    }
                    //No further use for the local encrypted file
                    await encryptedFile.DeleteAsync(StorageDeleteOption.PermanentDelete);

                    // Lift Off!
                    //var launcherOpts = new LauncherOptions()
                    //{
                    //    DisplayApplicationPicker = false
                    //};
                    await Launcher.LaunchFileAsync(decryptedFile);

                    //Burn after reading!
                    //await decryptedFile.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            if (exception != null)
            {
                var dialog = new MessageDialog("An exception was thrown:" + Environment.NewLine + exception.Message, "Oh noes!");
                await dialog.ShowAsync();
            }

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
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void AppbarRefreshButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await RefreshUI();
        }

        private void AppBarSettingsButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(Settings), client))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

    }
}
