using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YubiCrypt.Web
{
    public static class Clients
    {
        public readonly static Client Client1 = new Client
        {
            Id = "123456",
            Secret = "abcdef",
            RedirectUrl = "https://callback.com/"
        };

        public readonly static Client Client2 = new Client
        {
            Id = "7890ab",
            Secret = "7890ab",
            RedirectUrl = "https://www.getpostman.com/oauth2/callback"
        };
    }

    public class Client
    {
        public string Id { get; set; }
        public string Secret { get; set; }
        public string RedirectUrl { get; set; }
    }
}