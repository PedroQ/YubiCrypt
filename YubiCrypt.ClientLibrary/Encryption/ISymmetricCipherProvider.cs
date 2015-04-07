using System.IO;

namespace YubiCrypt.ClientLibrary.Encryption
{
    public interface ISymmetricCipherProvider
    {
        string Name { get;}
        byte IDByte { get; }
        int KeySize { get; }
        int BlockSize { get; }

        void Encrypt(Stream inputStream, Stream outputStream, byte[] key, byte[] iv);

        void Decrypt(Stream inputStream, Stream outputStream, byte[] key, byte[] iv);

    }
}
