using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YubiCrypt.Mobile.WindowsPhone
{
    public class TFTokenData
    {
        public int SerialNumber { get; set; }
        public byte[] SecretKey { get; set; }
    }
}
