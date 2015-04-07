using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YubiCrypt.ClientLibrary.MAC
{
    [Export(typeof(IMACProvider))]
    class HMAC256Provider : IMACProvider
    {
        public string Name
        {
            get { return "HMAC with SHA-256"; }
        }

        public byte IDByte
        {
            get { return 0x11; } //0x10 is HMAC with SHA-1
        }

        public byte[] CalculateMAC(Stream inputFileStream, byte[] hmacKey)
        {
            inputFileStream.Seek(0, SeekOrigin.Begin);
            HMac hmac = new HMac(new Sha256Digest());
            byte[] buffer = new byte[hmac.GetMacSize()];
            int read;
            hmac.Init(new KeyParameter(hmacKey));
            while (inputFileStream.Position < inputFileStream.Length)
            {
                read = inputFileStream.Read(buffer, 0, buffer.Length);
                hmac.BlockUpdate(buffer, 0, read);
            }
            hmac.DoFinal(buffer, 0);
            return buffer;
        }

        public bool ValidateMAC(Stream inputFileStream, byte[] hmacKey)
        {
            inputFileStream.Seek(-32, SeekOrigin.End);
            byte[] fileHMAC = new byte[32];
            inputFileStream.Read(fileHMAC, 0, 32);
            inputFileStream.SetLength(inputFileStream.Length - 32);
            var calculatedHMAC = CalculateMAC(inputFileStream, hmacKey);

            return fileHMAC.SequenceEqual(calculatedHMAC);
        }
    }
}
