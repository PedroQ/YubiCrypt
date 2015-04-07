using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudPTNet
{
    public class MetadataContent
    {
        public int bytes { get; set; }
        public bool thumb_exists { get; set; }
        public string rev { get; set; }
        public DateTime modified { get; set; }
        public string path { get; set; }
        public bool is_dir { get; set; }
        public string icon { get; set; }
        public string root { get; set; }
        public string mime_type { get; set; }
        public string size { get; set; }
        public string hash { get; set; }
    }

    public class Metadata
    {
        public string hash { get; set; }
        public int bytes { get; set; }
        public bool thumb_exists { get; set; }
        public string rev { get; set; }
        public DateTime modified { get; set; }
        public bool is_link { get; set; }
        public string path { get; set; }
        public bool is_dir { get; set; }
        public string root { get; set; }
        public List<MetadataContent> contents { get; set; }
        public string size { get; set; }
    }
}
