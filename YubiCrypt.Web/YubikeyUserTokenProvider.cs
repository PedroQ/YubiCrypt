using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using YubiCrypt.Web.Models;

namespace YubiCrypt.Web
{
    public class YubikeyUserTokenProvider : IUserTokenProvider<ApplicationUser, string>
    {

        public YubikeyUserTokenProvider()
        {

        }

        public async Task<string> GenerateAsync(string purpose, UserManager<ApplicationUser, string> manager, ApplicationUser user)
        {
            return string.Empty;
        }

        public async Task<bool> IsValidProviderForUserAsync(UserManager<ApplicationUser, string> manager, ApplicationUser user)
        {
            ApplicationUser usr = await manager.FindByIdAsync(user.Id);
            return usr.Yubikeys.Any();
        }

        public async Task NotifyAsync(string token, UserManager<ApplicationUser, string> manager, ApplicationUser user)
        {
            
        }

        public async Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser, string> manager, ApplicationUser user)
        {
            return OTPValidation.Validate(token, user.Id) == OTPValidation.Status.OK;
        }
    }
}