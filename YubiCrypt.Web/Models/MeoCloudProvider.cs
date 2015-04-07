using CloudPTNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using YubiCrypt.Web.Models.API;

namespace YubiCrypt.Web
{
    public class MeoCloudProvider
    {
        private CloudPTClient _meoCloudClient;
        private readonly string _meoCloudClientId = "<YOUR ID HERE>";
        private readonly string _meoCloudClientSecret = "<YOUR SECRET HERE>";

        public MeoCloudProvider()
        {
            _meoCloudClient = new CloudPTClient(_meoCloudClientId, _meoCloudClientSecret, CloudPTClient.AccessRestrictions.Sandbox);
        }

        public MeoCloudProvider(string token, string secret)
        {
            _meoCloudClient = new CloudPTClient(_meoCloudClientId, _meoCloudClientSecret, token, secret, CloudPTClient.AccessRestrictions.Sandbox);
        }

        public async Task<Tuple<string, string, string>> Authenticate()
        {
            var requestToken = _meoCloudClient.GetToken("http://thiswebsiteurl.com/Manage/MeoCloudCallback");
            var authUri = _meoCloudClient.GetOAuthAuthorizeUrl(requestToken, "http://thiswebsiteurl.com/Manage/MeoCloudCallback");
            return new Tuple<string, string, string>(requestToken.Token, requestToken.Secret, authUri);

        }

        public async Task<Tuple<string, string>> GetAccessToken(string token, string secret, string verifier)
        {
            var accessToken = _meoCloudClient.GetAccessToken(new CloudPTNet.Auth.AuthToken(token, secret), verifier);
            return new Tuple<string, string>(accessToken.Token, accessToken.Secret);
        }

        public async Task<IList<YubiCryptFile>> GetFiles()
        {
            List<YubiCryptFile> fileList = new List<YubiCryptFile>();
            var files = _meoCloudClient.GetMetadata("");

            foreach (var f in files.contents.Where(f => f.path.EndsWith(".ybc")))
            {
                fileList.Add(new YubiCryptFile(f.path.Remove(0, 1), f.size, f.modified));
            }
            return fileList;
        }

        public async Task<byte[]> GetFile(string fileName)
        {
            var file = _meoCloudClient.File(fileName);
            return file;

        }

        internal async Task<bool> UploadFile(string filename, byte[] buffer)
        {
            _meoCloudClient.UploadFile(filename, buffer);
            return true;
        }

        internal async Task DeleteFile(string fileName)
        {
            _meoCloudClient.DeleteFile(fileName);
        }
    }
}