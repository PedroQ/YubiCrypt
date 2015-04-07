using Org.BouncyCastle.Crypto.Engines;
using System.Composition;
using System.IO;



namespace YubiCrypt.ClientLibrary.Encryption
{
    [Export(typeof(ISymmetricCipherProvider))]
    public class TwofishSymmetricCipherProvider : ISymmetricCipherProvider
    {

        public string Name
        {
            get { return "Twofish with 256-bit key"; }
        }

        public byte IDByte
        {
            get { return 0x02; }
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
            BouncyCastleHelper.BlockCipherRunner(inputStream, outputStream, key, iv, new TwofishEngine(), true);
        }

        public void Decrypt(Stream inputStream, Stream outputStream, byte[] key, byte[] iv)
        {
            BouncyCastleHelper.BlockCipherRunner(inputStream, outputStream, key, iv, new TwofishEngine(), false);
        }
    }
}
