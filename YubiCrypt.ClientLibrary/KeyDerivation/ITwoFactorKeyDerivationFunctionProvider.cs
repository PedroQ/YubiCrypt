
namespace YubiCrypt.ClientLibrary.KeyDerivation
{
    public interface ITwoFactorKeyDerivationFunctionProvider
    {
        string Name { get; }

        byte IDByte { get; }

        int NumberOfOterations { get; set; }

        void DeriveHMACAndEncryptionKey(byte[] passphrase, byte[] salt, ref byte[] hmacKey, ref byte[] encryptionKey);

        void DeriveEncryptionKey(byte[] passphrase, byte[] salt, ref byte[] encryptionKey);

        byte[] GetExternalTokenSerial();

        bool IsExternalTokenAvailable();
    }
}
