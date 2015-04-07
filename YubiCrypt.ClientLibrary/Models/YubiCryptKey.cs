using System;

namespace YubiCrypt.ClientLibrary.Models
{
    public class YubiCryptKey
    {
        public string TFKDEncryptedSecret { get; set; }
        public string TFKDEncryptionSalt { get; set; }
        public int TokenSerialNumber { get; set; }
    }
}