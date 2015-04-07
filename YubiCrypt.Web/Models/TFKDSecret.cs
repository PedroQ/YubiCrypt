using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace YubiCrypt.Web.Models
{
    public class TFKDSecret
    {
        [Key, ForeignKey("YubikeyToken")]
        public string YubiKeyUID { get; set; }
        public string TFKDEncryptedSecret { get; set; }
        public string TFKDEncryptionSalt { get; set; }
        public virtual YubiKey YubikeyToken { get; set; }
    }
}