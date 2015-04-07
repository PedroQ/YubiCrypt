using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace YubiCrypt.Web.Models.API
{
    public class YubiCryptKey
    {
        public string TFKDEncryptedSecret { get; set; }
        public string TFKDEncryptionSalt { get; set; }
        public int TokenSerialNumber { get; set; }

        public YubiCryptKey(string key, string salt, int serialNumber)
        {
            TFKDEncryptedSecret = key;
            TFKDEncryptionSalt = salt;
            TokenSerialNumber = serialNumber;
        }
    }
}