using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.IO;

namespace YubiCrypt.ClientLibrary.Encryption
{
    internal class BouncyCastleHelper
    {
        internal static void BlockCipherRunner(Stream inputStream, Stream outputStream, byte[] key, byte[] iv, IBlockCipher blockCipherEngine, bool encrypt)
        {
            PaddedBufferedBlockCipher blockCipher;
            blockCipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(blockCipherEngine), new Pkcs7Padding());
            blockCipher.Init(encrypt, new ParametersWithIV(new KeyParameter(key), iv));
            var blockCipherBlockSize = blockCipher.GetBlockSize();
            byte[] buffer = new byte[blockCipherBlockSize * 8];
            byte[] processedBuffer = new byte[blockCipherBlockSize * 8];
            int read;
            int processed;
            while (inputStream.Position < inputStream.Length)
            {
                read = inputStream.Read(buffer, 0, buffer.Length);
                processed = blockCipher.ProcessBytes(buffer, 0, read, processedBuffer, 0);
                outputStream.Write(processedBuffer, 0, processed);
            }
            processed = blockCipher.DoFinal(processedBuffer, 0);
            outputStream.Write(processedBuffer, 0, processed);
        }
    }
}
