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
    class NoMACProvider : IMACProvider
    {
        public string Name
        {
            get { return "No MAC"; }
        }

        public byte IDByte
        {
            get { return 0x00; }
        }

        public byte[] CalculateMAC(Stream inputFileStream, byte[] hmacKey)
        {
            return null;
        }

        public bool ValidateMAC(Stream inputFileStream, byte[] hmacKey)
        {
            return true;
        }
    }
}
