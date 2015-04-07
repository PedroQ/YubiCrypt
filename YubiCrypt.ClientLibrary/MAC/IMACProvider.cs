using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YubiCrypt.ClientLibrary.MAC
{
    public interface IMACProvider
    {
        string Name { get; }
        byte IDByte { get; }
        byte[] CalculateMAC(Stream inputFileStream, byte[] hmacKey);
        bool ValidateMAC(Stream inputFileStream, byte[] hmacKey);
    }
}
