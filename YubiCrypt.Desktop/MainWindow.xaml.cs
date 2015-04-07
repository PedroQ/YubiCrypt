using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YubiCrypt.ClientLibrary;
using YubiCrypt.ClientLibrary.Models;
using YubiCrypt.Configurator;

namespace YubiCrypt.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IDisposable
    {

        private YubiCryptClient _client;
        private YubiCryptEngine _engine;
        private CipherSuite selectedCipherSuite;
        private const string API_CLIENT_ID = "123456";
        private const string API_CLIENT_SECRET = "abcdef";


        public MainWindow()
        {
            InitializeComponent();
            _engine = new YubiCryptEngine(typeof(MainWindow).Assembly);
        }

        private async void provisionStartBtn_Click(object sender, RoutedEventArgs e)
        {
            bool backupSecret = false;

            if (ProvisionTabBackupCheckBox.IsChecked.HasValue && ProvisionTabBackupCheckBox.IsChecked.Value)
            {
                if (string.IsNullOrWhiteSpace(ProvisionConfigPassphraseTextbox.Password))
                {
                    await this.ShowMessageAsync("Invalid passphrase.", "You must enter a passphrase to protect your secret key!");
                    return;
                }
                if (ProvisionConfigPassphraseTextbox.Password != ProvisionConfigPassphraseConfirmTextbox.Password)
                {
                    await this.ShowMessageAsync("Invalid passphrase.", "Entered passphrases are do not match.");
                    return;
                }
                backupSecret = true;
            }

            var msgResult = await this.ShowMessageAsync(string.Empty, "Yubikey slots 1 and 2 will be erased!\nContinue?", MessageDialogStyle.AffirmativeAndNegative);

            if (msgResult == MessageDialogResult.Affirmative)
            {
                statusBox.Items.Clear();
                provisionStartBtn.IsEnabled = false;
                YubikeyConfigurator ykConfigurator = new YubikeyConfigurator();
                ykConfigurator.WriteError = WriteError;
                ykConfigurator.WriteInfo = WriteInfo;
                ykConfigurator.WriteSuccess = WriteSuccess;
                ykConfigurator.WriteWarning = WriteWarning;
                if (backupSecret == true)
                {
                    await Task.Run(() =>
                    {
                        ykConfigurator.Provision(ProvisionConfigPassphraseTextbox.Password, true);
                    });
                }
                else
                {
                    await Task.Run(() =>
                            {
                                ykConfigurator.Provision(true);
                            });
                }
                provisionUploadBtn.IsEnabled = true;
                provisionStartBtn.IsEnabled = true;
            }
        }

        private void WriteError(string message, params object[] args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var itm = new ListBoxItem();
                itm.Content = string.Format("ERROR: " + message, args);
                itm.Foreground = Brushes.Red;
                statusBox.Items.Add(itm);
            }));
        }

        private void WriteInfo(string message, params object[] args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var itm = new ListBoxItem();
                itm.Content = string.Format("INFO: " + message, args);
                itm.Foreground = Brushes.Blue;
                statusBox.Items.Add(itm);
            }));
        }

        private void WriteSuccess(string message, params object[] args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var itm = new ListBoxItem();
                itm.Content = string.Format(message, args);
                itm.Foreground = Brushes.Green;
                statusBox.Items.Add(itm);
            }));
        }

        private void WriteWarning(string message, params object[] args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var itm = new ListBoxItem();
                itm.Content = string.Format("WARNING: " + message, args);
                itm.Foreground = Brushes.Orange;
                statusBox.Items.Add(itm);
            }));
        }

        private void provisionUploadBtn_Click(object sender, RoutedEventArgs e)
        {
            provisionUploadBtn.IsEnabled = false;

            Process ycSite = new Process();

            ycSite.StartInfo.FileName = "http://yubicrypt.azurewebsites.net/Yubikeys/Upload";

            ycSite.Start();

            provisionStartBtn.IsEnabled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RenewAccessToken();
        }

        private void InitializeWebServerClient()
        {

            //var authorizationServerUri = new Uri("http://localhost:25033");
            //var authorizationServer = new AuthorizationServerDescription
            //{
            //    AuthorizationEndpoint = new Uri(authorizationServerUri, "/OAuth/Authorize"),
            //    TokenEndpoint = new Uri(authorizationServerUri, "/OAuth/Token")
            //};
            //_webServerClient = new WebServerClient(authorizationServer, "123456", "abcdef");

            //_userAgentClient = new UserAgentClient(authorizationServer, "123456", "abcdef");
        }

        private void RequestToken()
        {

        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            YubiCryptOAuth2Token t = Properties.Settings.Default.AccessToken;

            SettingKDFComboBox.ItemsSource = _engine.KeyDerivationFunctionProviders;
            SettingSymmetricCipherComboBox.ItemsSource = _engine.SymmetricCipherProviders;
            SettingMACComboBox.ItemsSource = _engine.MACProviders;

            if (!await LoadSettings())
                return;

            if (t == null)
            {
                await this.ShowMessageAsync("No YubiCrypt Access Token", "In order to access the YubiCrypt platform you must first request an access token.\nPlease sign in.", MessageDialogStyle.Affirmative);
                tabAccount.IsSelected = true;
                return;
            }

            _client = new YubiCryptClient(API_CLIENT_ID, API_CLIENT_SECRET, t);

            await LoadFiles();
        }

        private async Task<bool> LoadSettings()
        {
            selectedCipherSuite = Properties.Settings.Default.CipherSuite;

            if (selectedCipherSuite == null)
            {
                await this.ShowMessageAsync("Welcome!", "In order to use YubiCrypt you must first select a default Cipher Suite to use.\nPlease save your preferences and restart the application before continuing.", MessageDialogStyle.Affirmative);
                SettingsFlyout.IsOpen = true;
                return false;
            }

            SettingKDFComboBox.SelectedValue = selectedCipherSuite.KeyDerivationFunctionIDByte;
            SettingSymmetricCipherComboBox.SelectedValue = selectedCipherSuite.SymmetricCipherIDByte;
            SettingMACComboBox.SelectedValue = selectedCipherSuite.MACIDByte;

            return true;
        }

        private async Task LoadFiles()
        {
            if (CheckClientAndAccessToken())
            {
                try
                {
                    DisplayLoading(true);
                    var response = await _client.GetFiles();
                    filesListBox.ItemsSource = response;
                    DisplayLoading(false);
                }
                catch (Exception e)
                {
                    this.ShowMessageAsync("Houston, we've had a problem.", e.Message);
                }
            }
        }

        private bool CheckClientAndAccessToken()
        {
            if (_client == null)
                return false;

            if (_client.OAuth2TokenIsValid())
                return true;

            this.ShowMessageAsync("Session expired", "You YubiCrypt session has expired.\nPlease sign in again.");

            return false;
        }

        private void RenewAccessToken()
        {
            var signInWindow = new YubiCryptSignIn();
            signInWindow.AccessTokenReceived += signInWindow_AccessTokenReceived;
            signInWindow.ShowDialog();

        }

        void signInWindow_AccessTokenReceived(object sender, AccessTokenReceivedEventArgs e)
        {
            if (_client == null)
                _client = new YubiCryptClient(API_CLIENT_ID, API_CLIENT_SECRET, e.AccessToken);
            else
                _client.OAuth2Token = e.AccessToken;

            Properties.Settings.Default.AccessToken = e.AccessToken;
            Properties.Settings.Default.Save();

            UpdateTokenStatus();
        }

        private void DisplayLoading(bool state)
        {
            if (state)
            {
                fileListPanel.Visibility = System.Windows.Visibility.Hidden;
                loadingGif.Visibility = System.Windows.Visibility.Visible;
                loadingTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                loadingGif.Visibility = System.Windows.Visibility.Hidden;
                loadingTextBlock.Visibility = System.Windows.Visibility.Hidden;
                fileListPanel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private async void uploadButton_Click(object sender, RoutedEventArgs e)
        {
            await SelectAndUploadFile();
        }

        private async Task SelectAndUploadFile()
        {
            if (await CheckUSBToken())
            {

                var tempFilePath = Path.GetTempFileName();

                OpenFileDialog dlg = new OpenFileDialog();
                //dlg.FileName = "Document"; // Default file name
                //dlg.DefaultExt = ".txt"; // Default file extension
                //dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension 

                // Show open file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process open file dialog box results 
                if (result == true)
                {
                    // Open document 
                    string filename = dlg.FileName;
                    var passphrase = await this.ShowInputAsync("Input passhprase", "Please select a passphrase to protect this file");

                    try
                    {
                        using (var outputFileStream = File.Open(tempFilePath, FileMode.Open, FileAccess.ReadWrite))
                        {
                            using (var inputFileStream = File.Open(filename, FileMode.Open))
                            {
                                EncryptFile(inputFileStream, outputFileStream, passphrase);
                            }
                            outputFileStream.Seek(0, SeekOrigin.Begin);
                            await UploadFile(System.IO.Path.GetFileName(filename) + ".ybc", outputFileStream);
                        }
                    }
                    catch (Exception e)
                    {
                        this.ShowMessageAsync("Houston, we've had a problem.", e.Message);
                    }
                    finally
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                    }
                    await LoadFiles();

                }
            }
        }

        private void EncryptFile(Stream inputFileStream, Stream outputFileStream, string passphrase)
        {
            _engine.EncryptFile(inputFileStream, outputFileStream, passphrase, selectedCipherSuite);
        }

        private void DecryptFile(Stream inputFileStream, Stream outputFileStream, string passphrase)
        {
            _engine.DecryptFile(inputFileStream, outputFileStream, passphrase);
        }
        private async Task UploadFile(string filename, Stream encryptedFileStream)
        {
            if (CheckClientAndAccessToken())
            {
                await _client.UploadFile(filename, encryptedFileStream);
            }
        }

        private async Task DownloadFile(string filePath, Stream outputStream)
        {
            if (CheckClientAndAccessToken())
            {
                await _client.GetFile(filePath, outputStream);
            }
        }

        private async Task DeleteFile(string filePath)
        {
            if (CheckClientAndAccessToken())
            {
                await _client.DeleteFile(filePath);
            }
        }

        private async Task SelectAndDownloadFile(YubiCryptFile file)
        {
            if (await CheckUSBToken())
            {

                var tempFilePath = Path.GetTempFileName();

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = file.FileName;
                dlg.OverwritePrompt = true;

                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {

                    try
                    {
                        var password = await this.ShowInputAsync("Passphrase needed", "Please enter the passphrase used to protect this file:");
                        ToggleProgressOverlay(true);
                        SetProgressMessage(string.Format("Downloading {0} from your YubiCrypt account...", file.FileName));
                        using (var encryptedFileStream = File.Open(tempFilePath, FileMode.Open, FileAccess.ReadWrite))
                        {
                            await DownloadFile(file.InternalName, encryptedFileStream);

                            SetProgressMessage("Decrypting your file.\nMake sure your YubiKey token is plugged in.");
                            encryptedFileStream.Seek(0, SeekOrigin.Begin);
                            using (var plaintextFileStream = File.Open(dlg.FileName + ".tmp", FileMode.Create))
                            {
                                DecryptFile(encryptedFileStream, plaintextFileStream, password);
                            }
                            if (File.Exists(dlg.FileName))
                                File.Delete(dlg.FileName);
                            File.Move(dlg.FileName + ".tmp", dlg.FileName);
                            ToggleProgressOverlay(false);
                        }
                        File.Delete(tempFilePath);
                    }
                    catch (YubiCryptEngineException e)
                    {
                        ToggleProgressOverlay(false);
                        this.ShowMessageAsync("Houston, we've had a problem.", e.Message);
                    }
                    catch (Exception e)
                    {
                        ToggleProgressOverlay(false);
                        this.ShowMessageAsync("Houston, we've had a problem.", e.Message);
                    }
                    finally
                    {
                        if (File.Exists(dlg.FileName + ".tmp"))
                            File.Delete(dlg.FileName + ".tmp");
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                    }
                }
            }
        }

        private async Task ConfirmAndDeleteFile(YubiCryptFile file)
        {
            if (await CheckUSBToken())
            {
                try
                {
                    var msgResult = await this.ShowMessageAsync(string.Empty, string.Format("{0} will be deleted.\nContinue?", file.FileName), MessageDialogStyle.AffirmativeAndNegative);

                    if (msgResult == MessageDialogResult.Affirmative)
                    {
                        await DeleteFile(file.InternalName);
                        await LoadFiles();
                    }
                }
                catch (Exception e)
                {
                    ToggleProgressOverlay(false);
                    this.ShowMessageAsync("Houston, we've had a problem.", e.Message);
                }
            }
        }

        private void SetProgressMessage(string p)
        {
            progressText.Text = p;
        }

        private void ToggleProgressOverlay(bool p)
        {
            if (p)
            {
                filesListBox.Visibility = System.Windows.Visibility.Hidden;
                progressStack.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                progressStack.Visibility = System.Windows.Visibility.Hidden;
                filesListBox.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private async Task<bool> CheckUSBToken()
        {
            //ToDo: Convert this to do... while

            //if (_engine.IsExternalTokenAvailable())
            //    return true;

            //await this.ShowMessageAsync("Yubikey not found", "This operation requires that you plug in your Yubikey token.");

            //if (_engine.IsExternalTokenAvailable())
            //    return true;

            //return false;

            return true;
        }

        private void filesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var lb = sender as ListBox;
            //if (lb.IsLoaded)
            //{
            //    if (e.AddedItems.Count > 0)
            //    {
            //        var item = e.AddedItems[0];
            //    }
            //}
            e.Handled = true;
        }

        private async void filesListDownloadContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (filesListBox.SelectedItem != null)
            {
                var file = ((YubiCryptFile)filesListBox.SelectedItem);
                await SelectAndDownloadFile(file);

            }
        }

        private void filesListBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (filesListBox.SelectedItem == null)
                e.Handled = true;
        }

        private void TabItem_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateTokenStatus()
        {
            YubiCryptOAuth2Token t = Properties.Settings.Default.AccessToken;
            if (t != null)
                statusMessage.Text = "Session token expires " + t.Expires;
            else
                statusMessage.Text = "No session token";
        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabAccount.IsSelected)
                UpdateTokenStatus();
            if (tabFiles.IsSelected)
                await LoadFiles();
        }

        private void SettingsSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCipherSuite == null)
                selectedCipherSuite = new CipherSuite();

            selectedCipherSuite.KeyDerivationFunctionIDByte = (byte)SettingKDFComboBox.SelectedValue;
            selectedCipherSuite.SymmetricCipherIDByte = (byte)SettingSymmetricCipherComboBox.SelectedValue;
            selectedCipherSuite.MACIDByte = (byte)SettingMACComboBox.SelectedValue;

            Properties.Settings.Default.CipherSuite = selectedCipherSuite;
            Properties.Settings.Default.Save();

            SettingsFlyout.IsOpen = false;
        }

        private async void OfflineEncryptFileButton_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openDlg = new OpenFileDialog();
            SaveFileDialog saveDlg = new SaveFileDialog()
            {
                Title = "Select where to save the encrypted file...",
                OverwritePrompt = true,
                AddExtension = true,
                DefaultExt = ".ybc",
                Filter = "YubiCrypt files (.ybc)|*.ybc"

            };

            //dlg.FileName = "Document"; // Default file name
            //dlg.DefaultExt = ".txt"; // Default file extension
            //dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> openResult = openDlg.ShowDialog();

            // Process open file dialog box results 
            if (openResult == true)
            {
                string filename = openDlg.FileName;

                Nullable<bool> saveResult = saveDlg.ShowDialog();

                if (saveResult == true)
                {
                    string encryptedFilename = saveDlg.FileName;

                    var passphrase = await this.ShowInputAsync("Input passhprase", "Please select a passphrase to protect this file");

                    using (var inputFileStream = File.Open(filename, FileMode.Open))
                    using (var outputFileStream = File.Open(encryptedFilename, FileMode.CreateNew))
                    {
                        try
                        {
                            EncryptFile(inputFileStream, outputFileStream, passphrase);
                        }
                        catch (Exception ex)
                        {
                            ToggleProgressOverlay(false);
                            this.ShowMessageAsync("Houston, we've had a problem in the offline encryption", ex.Message);
                        }
                    }
                }
            }

        }

        private async void OfflineDecryptFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "Select a YubiCrypt File"; // Default file name
            dlg.DefaultExt = ".ybc"; // Default file extension
            dlg.Filter = "YubiCrypt files (.ybc)|*.ybc"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;

                var passphrase = await this.ShowInputAsync("Input passhprase", "Please select a passphrase to protect this file");

                using (var inputFileStream = File.Open(filename, FileMode.Open))
                using (var outputFileStream = File.Open(filename.Replace(".ybc", ".dec"), FileMode.CreateNew))
                {
                    try
                    {
                        _engine.DecryptFile(inputFileStream, outputFileStream, passphrase);
                    }
                    catch (Exception ex)
                    {
                        ToggleProgressOverlay(false);
                        this.ShowMessageAsync("Houston, we've had a problem in the offline encryption", ex.Message);
                    }
                }

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

        private async void GetKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var otpInput = await this.ShowInputAsync("Yubikey OTP required", "Please touch your Yubikey to generate an OTP");

            if (string.IsNullOrWhiteSpace(otpInput))
                return;

            YubiCryptKey storedKey = await _client.GetTokenStoredSecret(otpInput);
            debugMessage.Text = "Secret: " + storedKey.TFKDEncryptedSecret + Environment.NewLine + "Salt: " + storedKey.TFKDEncryptionSalt + Environment.NewLine + "Serial #: " + storedKey.TokenSerialNumber; ;

            var passphraseInput = await this.ShowInputAsync("Passphrase", "Please enter the passphrase");

            if (string.IsNullOrWhiteSpace(passphraseInput))
                return;

            var secretkey = _engine.DecryptTokenSecret(storedKey.TFKDEncryptedSecret, storedKey.TFKDEncryptionSalt, passphraseInput);
            var secretKeyHexString = BitConverter.ToString(secretkey).Replace("-", "").ToLowerInvariant();
        }

        private void ShowSettingsFlyout(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = true;
        }

        private async void SettingsFlyout_ClosingFinished(object sender, RoutedEventArgs e)
        {
            await LoadSettings();
        }

        private async void filesListDeleteContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (filesListBox.SelectedItem != null)
            {
                var file = ((YubiCryptFile)filesListBox.SelectedItem);
                await ConfirmAndDeleteFile(file);

            }
        }
    }
}
