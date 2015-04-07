using System;
using System.Composition;
using System.Security.Cryptography;
using YubiCrypt.ClientLibrary.KeyDerivation;
using YubiCrypt.Configurator;

namespace YubiCrypt.Desktop.KeyDerivation
{
    [Export(typeof(ITwoFactorKeyDerivationFunctionProvider))]
    internal class PBKDFWithSHA1AndYubikeyProvider : ITwoFactorKeyDerivationFunctionProvider
    {
        public string Name
        {
            get { return "PBKDF with SHA1 and Yubikey token"; }
        }

        public byte IDByte
        {
            get { return 0x00; }
        }

        public int NumberOfOterations { get; set; }

        public PBKDFWithSHA1AndYubikeyProvider()
        {
            NumberOfOterations = 64000;
        }

        public void DeriveHMACAndEncryptionKey(byte[] passphrase, byte[] salt, ref byte[] hmacKey, ref byte[] encryptionKey)
        {
            byte[] yubikeyHMACsalt = new byte[20]; //HMAC-SHA1, 160bit / 8 = 20B
            var success = Yubikey.YkChallengeResponse(salt, out yubikeyHMACsalt, Yubikey.YkSlot.Slot2);
            if (!success)
                throw new Exception("Yubikey error.");

            var key = new Rfc2898DeriveBytes(passphrase, yubikeyHMACsalt, NumberOfOterations);

            hmacKey = key.GetBytes(hmacKey.Length);
            encryptionKey = key.GetBytes(encryptionKey.Length);
        }

        public void DeriveEncryptionKey(byte[] passphrase, byte[] salt, ref byte[] encryptionKey)
        {
            byte[] yubikeyHMACsalt = new byte[20];
            var success = Yubikey.YkChallengeResponse(salt, out yubikeyHMACsalt, Yubikey.YkSlot.Slot2);
            if (!success)
                throw new Exception("Yubikey error.");

            var key = new Rfc2898DeriveBytes(passphrase, yubikeyHMACsalt, NumberOfOterations);

            encryptionKey = key.GetBytes(encryptionKey.Length);
        }

        public byte[] GetExternalTokenSerial()
        {
            return BitConverter.GetBytes(Yubikey.YkSerial());
        }
        public bool IsExternalTokenAvailable()
        {
            return Yubikey.YkPresent();
        }
    }
}
