using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(YubiCrypt.Web.Startup))]
namespace YubiCrypt.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
