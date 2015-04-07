using System;

namespace YubiCrypt.ClientLibrary
{
    public class YubiCryptOAuth2Token
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public DateTime Expires { get; set; }
        public string RefreshToken { get; set; }
    }

    public class YubiCryptOAuth2TokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
    }
}
