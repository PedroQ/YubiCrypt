using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YubiCrypt.CommandLineTool
{
    class YubiCryptConfig
    {
        public string HexOTPPublicID;
        public uint YubikeySerialNumber { get; set; }
        public string ChallengeResponseKey { get; set; }
        public string OTPPublicID { get; set; }
        public string OTPPrivateID { get; set; }
        public string OTPAESKey { get; set; }
        public string YubikeyVersion { get; set; }
        public bool NDEFEnabled { get; set; }


        public void WriteToFile()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            File.WriteAllText(string.Format("YubiCryptConfig-{0}.json", YubikeySerialNumber), json);
        }
    }
}
