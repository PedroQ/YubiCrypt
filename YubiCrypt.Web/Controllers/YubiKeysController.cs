using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using YubiCrypt.Web.Models;
using Microsoft.AspNet.Identity;
using System.IO;
using Newtonsoft.Json;

namespace YubiCrypt.Web.Controllers
{
    [Authorize]
    public class YubikeysController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: YubiKeys
        public async Task<ActionResult> Index()
        {
            var userID = User.Identity.GetUserId();
            return View(await db.YubiKeys.Where(k => k.UserProfile.Id == userID).ToListAsync());
        }

        // GET: YubiKeys/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            YubiKey yubiKey = await db.YubiKeys.FindAsync(id);
            if (yubiKey == null)
            {
                return HttpNotFound();
            }
            return View(yubiKey);
        }

        // GET: YubiKeys/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: YubiKeys/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "YubiKeyUID,Privateidentity,AESKey,Active,Counter,Time,DateAdded")] YubiKey yubiKey)
        {
            if (ModelState.IsValid)
            {
                db.YubiKeys.Add(yubiKey);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(yubiKey);
        }

        // GET: YubiKeys/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            YubiKey yubiKey = await db.YubiKeys.FindAsync(id);
            if (yubiKey == null)
            {
                return HttpNotFound();
            }
            return View(yubiKey);
        }

        // POST: YubiKeys/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "YubiKeyUID,Privateidentity,AESKey,Active,Counter,Time,DateAdded")] YubiKey yubiKey)
        {
            if (ModelState.IsValid)
            {
                db.Entry(yubiKey).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(yubiKey);
        }

        // GET: YubiKeys/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            YubiKey yubiKey = await db.YubiKeys.FindAsync(id);
            if (yubiKey == null)
            {
                return HttpNotFound();
            }
            return View(yubiKey);
        }

        // POST: YubiKeys/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            YubiKey yubiKey = await db.YubiKeys.FindAsync(id);
            db.YubiKeys.Remove(yubiKey);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        //POST: YubiKeys/Validate
        public ActionResult Validate()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Validate([Bind(Include = "otp")] string otp)
        {
            if (ModelState.IsValid)
            {
                var status = OTPValidation.Validate(otp, User.Identity.GetUserId());
                TempData["status"] = status.ToString();
                return RedirectToAction("ValidationResult");
            }

            return View();
        }

        //POST: YubiKeys/Upload
        public ActionResult Upload()
        {
            return View();
        }

        public ActionResult SaveDropzoneJsUploadedFiles()
        {

            var currentUserId =  User.Identity.GetUserId();;

            foreach (string fileName in Request.Files)
            {
                HttpPostedFileBase file = Request.Files[fileName];

                string result = new StreamReader(file.InputStream).ReadToEnd();

                var config = JsonConvert.DeserializeObject<YubiCryptConfig>(result);

                YubiKey newToken = new YubiKey
                {
                    DateAdded = DateTime.Now,
                    Active = true,
                    Counter = 0,
                    Time = 0,
                    AESKey = Convert.ToBase64String(OTPValidation.StringToByteArray(config.OTPAESKey)),
                    Privateidentity = config.OTPPrivateID,
                    YubiKeyUID = config.OTPPublicID,
                    SerialNumber = config.YubikeySerialNumber,
                    YubikeyVersion = config.YubikeyVersion,
                    NDEFEnabled = config.NDEFEnabled,
                    UserProfile = db.Users.Where(u => u.Id == currentUserId).SingleOrDefault()
                };

                if (config.ChallengeResponseKey != null)
                {
                    var parts = config.ChallengeResponseKey.Split(':');
                    TFKDSecret secret = new TFKDSecret()
                    {
                        TFKDEncryptedSecret = parts[0],
                        TFKDEncryptionSalt = parts[1]
                    };

                    newToken.TFKDSecret = secret;
                }

                db.YubiKeys.Add(newToken);
                
            }
            db.SaveChanges();
            return Json(new { Message = string.Empty });
        }

        public ActionResult ValidationResult()
        {
            ViewBag.status = TempData["status"];
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
