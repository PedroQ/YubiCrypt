using CloudPTNet.Auth;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CloudPTNet
{
    public class CloudPTClient
    {
        private readonly string _consumerKey;
        private readonly string _consumerSecret;
        private AuthToken _authToken;

        private RestClient _apiRestClient;

        private RequestBuilder _reqBuilder;


        public AuthToken AuthenticationToken
        {
            get
            {
                return _authToken;
            }
            set
            {
                _authToken = value;
                SetRestClientsAuthenticator();
            }
        }

        public AccessRestrictions Restrictions
        {
            get;
            private set;
        }

        public CloudPTClient(string consumerKey, string secret, AccessRestrictions accessMode)
        {
            _consumerKey = consumerKey;
            _consumerSecret = secret;
            Restrictions = accessMode;

            InitializeRestClients();
        }

        public CloudPTClient(string consumerKey, string secret, string authToken, string authSecret, AccessRestrictions accessMode)
        {
            _consumerKey = consumerKey;
            _consumerSecret = secret;
            Restrictions = accessMode;

            InitializeRestClients();

            AuthenticationToken = new AuthToken(authToken, authSecret);
        }

        private void InitializeRestClients()
        {
            _apiRestClient = new RestClient();
            _reqBuilder = new RequestBuilder(Restrictions); //Convert to Singleton?

        }

        private void SetRestClientsAuthenticator()
        {
            _apiRestClient.Authenticator = RestSharp.Authenticators.OAuth1Authenticator.ForProtectedResource(_consumerKey, _consumerSecret, AuthenticationToken.Token, AuthenticationToken.Secret);

        }

        public enum AccessRestrictions
        {
            Sandbox,
            CloudPT
        }

        #region OAuth Stuff

        public AuthToken GetToken(string callback)
        {
            _apiRestClient.Authenticator = RestSharp.Authenticators.OAuth1Authenticator.ForRequestToken(_consumerKey, _consumerSecret, callback);

            RestRequest request = _reqBuilder.BuildOAuthTokenRequest();


            IRestResponse response = _apiRestClient.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();

            return ParseToken(response.Content);
        }

        public AuthToken GetAccessToken(AuthToken token, string verifier)
        {
            _apiRestClient.Authenticator = RestSharp.Authenticators.OAuth1Authenticator.ForAccessToken(_consumerKey, _consumerSecret, token.Token, token.Secret, verifier);

            RestRequest request = _reqBuilder.BuildOAuthAccessTokenRequest();


            IRestResponse response = _apiRestClient.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new CloudPTNetException(); //Wrong PIN

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();

            AuthenticationToken = ParseToken(response.Content);
            return _authToken;
        }

        public string GetOAuthAuthorizeUrl(AuthToken token, string callback)
        {
            return _reqBuilder.BuildOAuthAuthorizeUrl(token.Token, callback);
        }

        private AuthToken ParseToken(string p)
        {
            AuthToken token = new AuthToken();
            foreach (string s in p.Split('&'))
            {
                var urlParam = s.Split('=');

                if (urlParam[0] == "oauth_token")
                    token.Token = urlParam[1];

                if (urlParam[0] == "oauth_token_secret")
                    token.Secret = urlParam[1];
            }

            return token;
        }



        #endregion

        #region Account

        public AccountInfo AccountInfo()
        {
            RestRequest request = _reqBuilder.BuildAccountRequest();
            request.OnBeforeDeserialization = UidFix;
            var response = _apiRestClient.Execute<AccountInfo>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();

            return response.Data;
        }

        //Forces the RestSharp Json parser to parse the uid field as a string by surrounding it with quotation marks,
        //otherwise it will try to parse it as a long (and will fail).
        //To be parsed as a double it should be in scientific notation format
        private void UidFix(IRestResponse response)
        {
            if (response.StatusCode != System.Net.HttpStatusCode.OK || string.IsNullOrWhiteSpace(response.Content))
                return;

            var content = response.Content;

            var regexPattern = "(\"uid\"): (?<uid>\\d+)";

            var replacePattern = "\"uid\": \"${uid}\"";

            content = Regex.Replace(content, regexPattern, replacePattern, RegexOptions.IgnoreCase);
            response.Content = content;
        }

        #endregion

        #region Metadata

        public Metadata GetMetadata(string path, bool listContents, int fileLimit, bool includeDeleted)
        {
            if (fileLimit > 25000 || fileLimit < 1)
                throw new ArgumentOutOfRangeException("filelimit", "filelimit parameter must be between 1 and 25000");

            var request = _reqBuilder.BuildMetadataRequest(path, listContents, fileLimit, includeDeleted);

            //var u = _apiRestClient.BuildUri(request);

            var response = _apiRestClient.Execute<Metadata>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();

            return response.Data;
        }

        public Metadata GetMetadata(string path, bool listContents, int fileLimit)
        {
            return GetMetadata(path, listContents, fileLimit, false);
        }

        public Metadata GetMetadata(string path, bool listContents)
        {
            return GetMetadata(path, listContents, 10000, false);
        }

        public Metadata GetMetadata(string path)
        {
            return GetMetadata(path, true, 10000, false);
        }



        #endregion

        #region MetadataShare

        public enum MetadataShareOrderBy
        {
            ModifiedDate,
            MimeType,
            Size,
            Name,
            Folder
        }

        public MetadataShare GetMetadataShare(string shareId, string path, int fileLimit, MetadataShareOrderBy sortOrder, bool orderAscending, string cursor, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(shareId))
                throw new ArgumentNullException("shareId");

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException("path");

            if (fileLimit > 25000)
                fileLimit = 25000;
            if (fileLimit < 1)
                fileLimit = 1;

            var request = _reqBuilder.BuildMetadataShareRequest(shareId, path, fileLimit, sortOrder, orderAscending, cursor, mimeType);

            var response = _apiRestClient.Execute<MetadataShare>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();

            return response.Data;
        }

        public MetadataShare GetMetadataShare(string shareId, string path, int fileLimit, MetadataShareOrderBy sortOrder, bool orderAscending)
        {
            return GetMetadataShare(shareId, path, fileLimit, sortOrder, orderAscending, null, null);
        }

        public MetadataShare GetMetadataShare(string shareId, string path)
        {
            return GetMetadataShare(shareId, path, 10000, MetadataShareOrderBy.Name, true);
        }

        #endregion

        #region Public Links

        public List<PublicLinkListEntry> GetPublicLinks()
        {
            var request = _reqBuilder.BuildListLinksRequest();

            var response = _apiRestClient.Execute<List<PublicLinkListEntry>>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();

            return response.Data;
        }

        public void DeletePublicLink(string shareId)
        {
            if (string.IsNullOrWhiteSpace(shareId))
                throw new ArgumentNullException("shareId");

            var request = _reqBuilder.BuildDeleteLinkRequest(shareId);

            var response = _apiRestClient.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();
        }

        public PublicLink CreatePublicLink(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException("path");

            if (path.StartsWith("/"))
                path = path.Substring(1);

            var request = _reqBuilder.BuildCreatePublicLinkRequest(path);

            var response = _apiRestClient.Execute<PublicLink>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();

            return response.Data;
        }

        #endregion

        #region Shared Folders

        public object ListSharedFolders()
        {
            var request = _reqBuilder.BuildListSharedFoldersRequest();

            var response = _apiRestClient.Execute<SharedFolder>(request);

            return response;
        }

        #endregion

        public byte[] File(string filePath)
        {
            var request = _reqBuilder.BuildFileRequest(filePath);

            //var u = _apiRestClient.BuildUri(request);

            var response = _apiRestClient.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();

            return response.RawBytes;
        }
        //ToDO: Partial Classes (Sync & Async), this goes there.

        public void UploadFile(string filename, byte[] fileData)
        {
            var request = _reqBuilder.BuildUploadFileRequest(filename);
            request.AddParameter("file", filename, ParameterType.UrlSegment);

            request.AddParameter("file", fileData, ParameterType.RequestBody);

            var response = _apiRestClient.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();
        }

        public void DeleteFile(string filename)
        {
            var request = _reqBuilder.BuildDeleteFileRequest(filename);

            var response = _apiRestClient.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new CloudPTNetException();
        }
    }
}
