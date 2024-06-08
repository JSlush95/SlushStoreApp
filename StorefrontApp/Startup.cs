using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(StorefrontApp.Startup))]
namespace StorefrontApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
