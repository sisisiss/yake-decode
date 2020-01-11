using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(yakencode.Startup))]
namespace yakencode
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
