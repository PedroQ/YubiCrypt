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

namespace YubiCrypt.Web.Controllers
{
    public class FileController : ApiController
    {
        // GET api/<controller>
        [RequiredScopesAttribute("files")]
        [HttpGet]
        public async Task<IEnumerable<YubiCryptFile>> GetFiles()
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

            return await new MeoCloudProvider(token, secret).GetFiles();
        }

        [RequiredScopesAttribute("files")]
        [HttpGet]
        public async Task<HttpResponseMessage> Download([FromUri] string fileName)
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

            var rawFile = await new MeoCloudProvider(token, secret).GetFile(fileName);

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new ByteArrayContent(rawFile);
            result.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/octet-stream");

            return result;
        }


        // POST api/<controller>
        [HttpPost]
        public async Task<IHttpActionResult> Upload()
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

            if (!Request.Content.IsMimeMultipartContent())
                throw new Exception(); // divided by zero

            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);
            foreach (var file in provider.Contents)
            {
                var filename = file.Headers.ContentDisposition.FileName.Replace("\"", string.Empty);
                var buffer = await file.ReadAsByteArrayAsync();


                var result = await new MeoCloudProvider(token, secret).UploadFile(filename, buffer);
            }

            return Ok();
        }

        [RequiredScopesAttribute("files")]
        [HttpGet]
        public async Task<IHttpActionResult> Delete([FromUri] string fileName)
        {
            var usrId = User.Identity.GetUserId();
            string token;
            string secret;
            using (var db = new ApplicationDbContext())
            {
                var usr = db.Users.Where(u => u.Id == usrId).Single();
                token = usr.MeoCloudAPIToken;
                secret = usr.MeoCloudAPISecret;

                await new MeoCloudProvider(token, secret).DeleteFile(fileName);
            }


            return Ok();
        }
    }
}