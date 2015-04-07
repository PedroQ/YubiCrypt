using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudPTNet
{
    public class MetadataShare
    {
        public string hash { get; set; }
        public string rev { get; set; }
        public bool thumb_exists { get; set; }
        public int bytes { get; set; }
        public string modified { get; set; }
        public string bottom_cursor { get; set; }
        public string top_cursor { get; set; }
        public string path { get; set; }
        public bool is_dir { get; set; }
        public string root { get; set; }
        public List<MetadataContent> contents { get; set; }
        public string size { get; set; }
    }
}
