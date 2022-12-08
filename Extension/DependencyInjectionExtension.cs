using DiscordBot.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

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
