using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudPTNet.Auth
{
    public class AuthToken
    {
        public string Token { get; set; }
        public string Secret { get; set; }

        public AuthToken()
        {

        }

        public AuthToken(string authToken, string authSecret)
        {
            Token = authToken;
            Secret = authSecret;
        }

    }
}
