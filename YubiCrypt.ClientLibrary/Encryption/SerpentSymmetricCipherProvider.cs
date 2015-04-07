using Org.BouncyCastle.Crypto.Engines;
using System.Composition;
using System.IO;



namespace YubiCrypt.ClientLibrary.Encryption
{
    [Export(typeof(ISymmetricCipherProvider))]
    public class SerpentSymmetricCipherProvider : ISymmetricCipherProvider
    {

        public string Name
        {
            get { return "Serpent with 256-bit key"; }
        }

        public byte IDByte
        {
            get { return 0x01; }
        }

        public int KeySize
        {
            get { return 256; }
        }

        public int BlockSize
        {
            get { return 128; }
        }

        public void Encrypt(Stream inputStream, Stream outputStream, byte[] key, byte[] iv)
        {
            BouncyCastleHelper.BlockCipherRunner(inputStream, outputStream, key, iv, new SerpentEngine(), true);
        }

        public void Decrypt(Stream inputStream, Stream outputStream, byte[] key, byte[] iv)
        {
            BouncyCastleHelper.BlockCipherRunner(inputStream, outputStream, key, iv, new SerpentEngine(), false);
        }
    }
}
