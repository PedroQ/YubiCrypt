

namespace YubiCrypt.ClientLibrary
{
    public class CipherSuite
    {
        public byte KeyDerivationFunctionIDByte { get; set; }
        public byte SymmetricCipherIDByte { get; set; }
        public byte MACIDByte { get; set; }
    }
}
