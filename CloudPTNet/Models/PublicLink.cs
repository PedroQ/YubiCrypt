using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudPTNet
{
    public class PublicLinkListEntry
    {
        public string url { get; set; }
        public string path { get; set; }
        public string shareid { get; set; }
        public MetadataContent metadata { get; set; }
    }

    public class PublicLink
    {
        public string url { get; set; }
        public string expires { get; set; }
    }
}
