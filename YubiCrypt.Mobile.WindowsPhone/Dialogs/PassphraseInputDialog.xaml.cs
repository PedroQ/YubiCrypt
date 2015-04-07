using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace YubiCrypt.Mobile.WindowsPhone.Dialogs
{
    public sealed partial class PassphraseInputDialog : ContentDialog
    {
        public string Passphrase { get; private set; }

        public PassphraseInputDialog()
        {
            this.InitializeComponent();
        }

        public PassphraseInputDialog(string bodyText) : this()
        {
            body.Text = bodyText;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrEmpty(passphrase.Password))
            {
                args.Cancel = true;
                body.Text = "Password is required.";
            }

            Passphrase = passphrase.Password;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
