using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace YubiCrypt.Web.Models.API
{
    public class YubiCryptFile
    {
        public string FileName { get; set; }
        public string Size { get; set; }
        public DateTime Modified { get; set; }
        public string InternalName { get; set; }



        public YubiCryptFile(string fileName, string size, DateTime modified)
        {
            InternalName = fileName;
            Size = size;
            Modified = modified;
            FileName = fileName.Replace(".ybc", "");
        }
    }
}