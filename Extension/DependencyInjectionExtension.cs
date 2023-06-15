using DiscordBot.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot.Extension
{
    public static class DependencyInjectionExtension
    {

        public static void DependencyInjection(ServiceCollection services)
        {

            //DbContext
            services.AddTransient<FreeBeerdbTestContext>();

        }
    }
}
