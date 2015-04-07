using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudPTNet
{
    public class AccountInvites
    {
        public int invites_sent { get; internal set; }
        public int quota_invites { get; internal set; }
    }

    public class AccountQuotaInfo
    {
        public int shared { get; internal set; }
        public long quota { get; internal set; }
        public int normal { get; internal set; }
    }

    public class AccountInfo
    {
        public string created_on { get; internal set; }
        public AccountInvites invites { get; internal set; }
        public string display_name { get; internal set; }
        public string uid { get; internal set; }
        public string last_event { get; internal set; }
        public bool active { get; internal set; }
        public string segment { get; internal set; }
        public AccountQuotaInfo quota_info { get; internal set; }
        public string email { get; internal set; }
    }
}
