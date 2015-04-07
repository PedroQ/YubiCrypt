using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using YubiCrypt.Web.Models.API;

namespace YubiCrypt.Web.Models
{
    public class FilesViewModel
    {
        public IList<YubiCryptFile> Files { get; set; }
    }
}