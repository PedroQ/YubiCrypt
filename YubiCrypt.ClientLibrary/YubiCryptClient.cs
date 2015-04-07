using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using YubiCrypt.ClientLibrary.Models;

namespace YubiCrypt.ClientLibrary
{
    public class YubiCryptClient : IDisposable
    {
        private string _apiBaseUrl = "https://localhost:44300/";


        private string _clientId;
        private string _clientSecret;
        private string _redirectUri;

        private YubiCryptOAuth2Token _oAuth2Token;

        private HttpClient _httpClient;

        public string YubiCryptBaseUrl
        {
            get { return _apiBaseUrl; }
            set { _apiBaseUrl = value; InitHttpClient(); }
        }

        public YubiCryptOAuth2Token OAuth2Token
        {
            get { return _oAuth2Token; }

            set
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(value.TokenType, value.AccessToken);
                _oAuth2Token = value;
            }
        }

        public YubiCryptClient(string clientId, string clientSecret, string redirectURI)
        {
            InitHttpClient();

            _clientId = clientId;
            _clientSecret = clientSecret;
            _redirectUri = redirectURI;

        }

        public YubiCryptClient(string clientId, string clientSecret, YubiCryptOAuth2Token clientToken)
        {
            InitHttpClient();

            _clientId = clientId;
            _clientSecret = clientSecret;
            OAuth2Token = clientToken;

        }

        private void InitHttpClient()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_apiBaseUrl);
        }

        public string GetAuthorizationCodeRequestUrl(string[] scopes)
        {
            return string.Format("{0}/OAuth/Authorize?response_type=code&client_id={1}&redirect_uri={2}&scope={3}", _apiBaseUrl, _clientId, _redirectUri, string.Join(" ", scopes));
        }

        public async Task<YubiCryptOAuth2Token> GetOAuth2AccessToken(string authorizationCode)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/OAuth/Token");

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("redirect_uri", _redirectUri)
            };

            request.Content = new FormUrlEncodedContent(parameters);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new Exception(string.Format("An HTTP Error was returned: {0} {1}", response.StatusCode, response.ReasonPhrase));

            var resposeContent = await response.Content.ReadAsStringAsync();

            var token = JsonConvert.DeserializeObject<YubiCryptOAuth2TokenResponse>(resposeContent);

            var resultToken = new YubiCryptOAuth2Token()
            {
                AccessToken = token.access_token,
                Expires = DateTime.Now.AddSeconds(token.expires_in),
                RefreshToken = token.refresh_token,
                TokenType = token.token_type
            };

            return resultToken;
        }

        public bool OAuth2TokenIsValid()
        {
            if (OAuth2Token == null)
                return false;
            if (OAuth2Token.Expires < DateTime.Now)
                return false;

            return true;
        }

        public async Task<YubiCryptOAuth2Token> RefreshToken(string refreshToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            var request = new HttpRequestMessage(HttpMethod.Post, "/OAuth/Token");

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            };

            request.Content = new FormUrlEncodedContent(parameters);

            var response = await _httpClient.SendAsync(request);

            var resposeContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(string.Format("An HTTP Error was returned: {0} {1}", response.StatusCode, response.ReasonPhrase));


            var token = JsonConvert.DeserializeObject<YubiCryptOAuth2TokenResponse>(resposeContent);

            var resultToken = new YubiCryptOAuth2Token()
            {
                AccessToken = token.access_token,
                Expires = DateTime.Now.AddSeconds(token.expires_in),
                RefreshToken = token.refresh_token,
                TokenType = token.token_type
            };

            return resultToken;
        }

        public async Task<List<YubiCryptFile>> GetFiles()
        {
            if (OAuth2TokenIsValid())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/File/GetFiles");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    throw new Exception(string.Format("An HTTP Error was returned: {0} {1}", response.StatusCode, response.ReasonPhrase));

                var resposeContent = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<List<YubiCryptFile>>(resposeContent);
            }
            else
                throw new Exception("Invalid token");
        }

        public async Task GetFile(string p, Stream outputStream)
        {

            if (OAuth2TokenIsValid())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/File/Download?filename=" + p);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    throw new Exception(string.Format("An HTTP Error was returned: {0} {1}", response.StatusCode, response.ReasonPhrase));

                var resposeContent = await response.Content.ReadAsStreamAsync();

                await resposeContent.CopyToAsync(outputStream);

            }
            else
                throw new Exception("Invalid token");
        }

        public async Task UploadFile(string fileName, Stream fileStream)
        {
            var content = new MultipartFormDataContent();

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.Add("Content-Type", "application/octet-stream");
            content.Add(fileContent, "file", fileName);

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/File/Upload");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new Exception(string.Format("An HTTP Error was returned: {0} {1}", response.StatusCode, response.ReasonPhrase));

        }

        public async Task DeleteFile(string fileName)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/File/Delete?filename=" + fileName);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new Exception(string.Format("An HTTP Error was returned: {0} {1}", response.StatusCode, response.ReasonPhrase));

        }

        public async Task<YubiCryptKey> GetTokenStoredSecret(string yubikeyOTP)
        {
            if (OAuth2TokenIsValid())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/Keys/GetTokenStoredSecret?tokenOTP=" + yubikeyOTP);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    throw new Exception(string.Format("An HTTP Error was returned: {0} {1}", response.StatusCode, response.ReasonPhrase));

                var resposeContent = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<YubiCryptKey>(resposeContent);
            }
            else
                throw new Exception("Invalid token");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_httpClient != null) _httpClient.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
