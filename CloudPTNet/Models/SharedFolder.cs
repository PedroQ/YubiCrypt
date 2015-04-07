using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudPTNet
{
    public class SharedFolder
    {
        public Dictionary<string, SharedFolderInternal> folder { get; set; }
    }

    public class User
    {
        public string id { get; set; }
        public bool owner { get; set; }
        public bool user { get; set; }
        public string email { get; set; }
        public string name { get; set; }
    }

    public class SharedFolderInternal
    {
        public string folder_type { get; set; }
        public bool is_owner { get; set; }
        public List<User> users { get; set; }
        public string shared_folder_path { get; set; }
    }

}
