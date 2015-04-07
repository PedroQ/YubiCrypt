using NdefLibrary.Ndef;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Proximity;
using Windows.Storage.Pickers.Provider;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YubiCrypt.ClientLibrary;
using YubiCrypt.ClientLibrary.Models;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace YubiCrypt.Mobile.WindowsPhone.Dialogs
{
    public sealed partial class RetrieveTokenSecretDialog : ContentDialog
    {
        private ProximityDevice nfcDevice;
        private long nfcNDEFSubscriptionId;

        public string TokenGeneratedOTP { get; private set; }
        public string Passphrase { get; private set; }

        public RetrieveTokenSecretDialog()
        {
            this.InitializeComponent();
            nfcDevice = ProximityDevice.GetDefault();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var ycOTP = otpTextbox.Text;
            var skPassphrase = passphraseTextbox.Password;

            if (string.IsNullOrWhiteSpace(ycOTP) || string.IsNullOrWhiteSpace(skPassphrase))
            {
                args.Cancel = true;
                return;
            }

            TokenGeneratedOTP = ycOTP;
            Passphrase = skPassphrase;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if (nfcDevice == null)
                return;

            nfcNDEFSubscriptionId = nfcDevice.SubscribeForMessage("NDEF", messageReceivedHandler);
            nfcDevice.DeviceArrived += nfcDevice_DeviceArrived;
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

                }
            }
        }

        void nfcDevice_DeviceArrived(ProximityDevice sender)
        {
            Debug.WriteLine("NFC Device Arrived");
        }

        private async Task FillOTPCodeInput(string yubikeyOTP)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                otpTextbox.Text = yubikeyOTP;
            });
        }


        private void UnsubscribeNFC(long subscritpionId)
        {
            if (nfcDevice == null)
                return;
            nfcDevice.StopSubscribingForMessage(subscritpionId);
            subscritpionId = 0;
        }

        private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            UnsubscribeNFC(nfcNDEFSubscriptionId);
        }

        
    }
}
