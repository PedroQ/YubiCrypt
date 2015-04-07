using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using YubiCrypt.Web.Models;
using System.Threading.Tasks;

namespace YubiCrypt.Web.Controllers
{
    public class FilesController : Controller
    {
        // GET: Files
        public async Task<ActionResult> Index()
        {
            var usrId = User.Identity.GetUserId();
            string token;
            string secret;
            using (var db = new ApplicationDbContext())
            {
                var usr = db.Users.Where(u => u.Id == usrId).Single();
                token = usr.MeoCloudAPIToken;
                secret = usr.MeoCloudAPISecret;
            }

            var files = await new MeoCloudProvider(token, secret).GetFiles();
            
            return View(files);
        }
    }
}