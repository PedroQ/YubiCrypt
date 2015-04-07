using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using YubiCrypt.Web.Filters;
using Microsoft.AspNet.Identity;
using YubiCrypt.Web.Models;
using YubiCrypt.Web.Models.API;
using System.IO;
using System.Net.Http.Headers;


namespace YubiCrypt.Web.Controllers.API
{
    public class KeysController : ApiController
    {
        [RequiredScopesAttribute("keys")]
        [HttpGet]
        public YubiCryptKey GetTokenStoredSecret([FromUri] string tokenOTP)
        {
            var usrId = User.Identity.GetUserId();

            var validationResult = OTPValidation.Validate(tokenOTP, usrId);
            if (validationResult != OTPValidation.Status.OK)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("The provided OTP token is invalid."),
                    ReasonPhrase = "Invalid authentication token"
                };
                throw new HttpResponseException(resp);
            }

            using (var db = new ApplicationDbContext())
            {
                var usr = db.Users.Where(u => u.Id == usrId).Single();
#if ALLOW_DUMMY_OTPS
                var userHardwareToken = usr.Yubikeys.FirstOrDefault();
                var tokenPublicID = "TESTTAG";
#else
                var tokenPublicID = OTPValidation.SplitOTP(tokenOTP).Item1;
                var userHardwareToken = usr.Yubikeys.Where(yk => yk.YubiKeyUID == tokenPublicID).SingleOrDefault();
#endif
                if (userHardwareToken == null)
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent(string.Format("User has no associated YubiKey with ID = {0}", tokenPublicID)),
                        ReasonPhrase = "YubiKey Not Found"
                    };
                    throw new HttpResponseException(resp);
                }
                var secretKey = userHardwareToken.TFKDSecret;
                if (secretKey == null)
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("This YubiKey does not have the secret key stored in the YubiCrypt platform."),
                        ReasonPhrase = "Key Not Found"
                    };
                    throw new HttpResponseException(resp);
                }
                return new YubiCryptKey(secretKey.TFKDEncryptedSecret, secretKey.TFKDEncryptionSalt, userHardwareToken.SerialNumber);
            }
        }
    }
}
