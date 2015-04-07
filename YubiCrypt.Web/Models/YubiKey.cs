using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace YubiCrypt.Web.Models
{
    public class YubiKey
    {
        public virtual ApplicationUser UserProfile { get; set; }
        [Key]
        public string YubiKeyUID { get; set; }
        public string Privateidentity { get; set; }
        public string AESKey { get; set; }
        public bool Active { get; set; }
        public int Counter { get; set; }
        public int Time { get; set; }
        public DateTime DateAdded { get; set; }
        public int SerialNumber { get; set; }
        public string YubikeyVersion { get; set; }
        public bool NDEFEnabled { get; set; }
        public virtual TFKDSecret TFKDSecret { get; set; }
    }
}