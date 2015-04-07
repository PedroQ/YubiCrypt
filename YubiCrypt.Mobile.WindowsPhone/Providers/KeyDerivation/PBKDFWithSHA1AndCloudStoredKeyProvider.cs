using System;
using System.Composition;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using YubiCrypt.ClientLibrary.KeyDerivation;

namespace YubiCrypt.Mobile.WindowsPhone.Providers.KeyDerivation
{
    [Export(typeof(ITwoFactorKeyDerivationFunctionProvider))]
    class PBKDFWithSHA1AndCloudStoredKeyProvider : ITwoFactorKeyDerivationFunctionProvider
    {
        public string Name
        {
            get { return "PBKDF with SHA1 and Retrieved Secret"; }
        }

        public byte IDByte
        {
            get { return 0x00; }
        }

        public int NumberOfOterations { get; set; }

        public PBKDFWithSHA1AndCloudStoredKeyProvider()
        {
            NumberOfOterations = 64000;
        }

        public void DeriveHMACAndEncryptionKey(byte[] passphrase, byte[] salt, ref byte[] hmacKey, ref byte[] encryptionKey)
        {
            var tokenData = SettingsHelper.LoadSetting<TFTokenData>(Consts.SETTINGS_TOKEN_SECRET_KEY);

            IBuffer passphraseBuffer = CryptographicBuffer.CreateFromByteArray(passphrase);
            IBuffer saltBuffer = CryptographicBuffer.CreateFromByteArray(salt);

            IBuffer hmacKeyBuffer = CryptographicBuffer.CreateFromByteArray(tokenData.SecretKey);

            MacAlgorithmProvider hmacProvider = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
            CryptographicKey hmacCryptoKey = hmacProvider.CreateKey(hmacKeyBuffer);
            IBuffer hmacOutputBuffer = CryptographicEngine.Sign(hmacCryptoKey, saltBuffer);

            KeyDerivationAlgorithmProvider pbkdf2Provider = Windows.Security.Cryptography.Core.KeyDerivationAlgorithmProvider.OpenAlgorithm("PBKDF2_SHA1");
            KeyDerivationParameters pbkdf2Parms = KeyDerivationParameters.BuildForPbkdf2(hmacOutputBuffer, (uint)NumberOfOterations);
            CryptographicKey pbkdf2Key = pbkdf2Provider.CreateKey(passphraseBuffer);

            IBuffer derivedKeyBuffer = CryptographicEngine.DeriveKeyMaterial(pbkdf2Key, pbkdf2Parms, (uint)(hmacKey.Length + encryptionKey.Length));

            byte[] derivedKeyBytes = derivedKeyBuffer.ToArray();
            Array.Copy(derivedKeyBytes, hmacKey, hmacKey.Length);
            Array.Copy(derivedKeyBytes, hmacKey.Length, encryptionKey, 0, encryptionKey.Length);
            Array.Clear(derivedKeyBytes, 0, derivedKeyBytes.Length);
        }

        public void DeriveEncryptionKey(byte[] passphrase, byte[] salt, ref byte[] encryptionKey)
        {
            var tokenData = SettingsHelper.LoadSetting<TFTokenData>(Consts.SETTINGS_TOKEN_SECRET_KEY);

            IBuffer passphraseBuffer = CryptographicBuffer.CreateFromByteArray(passphrase);
            IBuffer hmacKeyBuffer = CryptographicBuffer.CreateFromByteArray(tokenData.SecretKey);

            MacAlgorithmProvider hmacProvider = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
            CryptographicHash saltHMAC = hmacProvider.CreateHash(hmacKeyBuffer);
            IBuffer saltBuffer = saltHMAC.GetValueAndReset();

            KeyDerivationAlgorithmProvider pbkdf2Provider = Windows.Security.Cryptography.Core.KeyDerivationAlgorithmProvider.OpenAlgorithm("PBKDF2_SHA1");
            KeyDerivationParameters pbkdf2Parms = KeyDerivationParameters.BuildForPbkdf2(saltBuffer, (uint)NumberOfOterations);
            CryptographicKey pbkdf2Key = pbkdf2Provider.CreateKey(passphraseBuffer);

            IBuffer derivedKeyBuffer = CryptographicEngine.DeriveKeyMaterial(pbkdf2Key, pbkdf2Parms, (uint)encryptionKey.Length);

            byte[] derivedKeyBytes = derivedKeyBuffer.ToArray();
            Array.Copy(derivedKeyBytes, encryptionKey, encryptionKey.Length);
            Array.Clear(derivedKeyBytes, 0, derivedKeyBytes.Length);
        }

        public byte[] GetExternalTokenSerial()
        {
            var tokenData = SettingsHelper.LoadSetting<TFTokenData>(Consts.SETTINGS_TOKEN_SECRET_KEY);
            return BitConverter.GetBytes(tokenData.SerialNumber);
        }

        public bool IsExternalTokenAvailable()
        {
            return SettingsHelper.SettingsExists(Consts.SETTINGS_TOKEN_SECRET_KEY);
        }
    }
}
