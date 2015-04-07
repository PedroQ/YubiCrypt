using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YubiCrypt.Web.Models
{
    class YubiCryptConfig
    {
        public string HexOTPPublicID;
        public int YubikeySerialNumber { get; set; }
        public string ChallengeResponseKey { get; set; }
        public string OTPPublicID { get; set; }
        public string OTPPrivateID { get; set; }
        public string OTPAESKey { get; set; }
        public string YubikeyVersion { get; set; }
        public bool NDEFEnabled { get; set; }
    }
}
