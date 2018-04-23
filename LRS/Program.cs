using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace bracken_lrs
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // var configuration = new ConfigurationBuilder()
            //     .AddCommandLine(args)
            //     .Build();
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                // .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("hosting.json", optional: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            return WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(config)
                .UseStartup<Startup>()
                .Build();
        }

        // public static IWebHost BuildWebHost(string[] args, IConfiguration config) =>
        //     WebHost.CreateDefaultBuilder(args)
        //         .UseConfiguration(config)
        //         .ConfigureAppConfiguration(ConfigConfiguration)
        //         .UseStartup<Startup>()
        //         .Build();

        // static void ConfigConfiguration(WebHostBuilderContext ctx, IConfigurationBuilder config)
        // {
        //     config.SetBasePath(Directory.GetCurrentDirectory())
        //         .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        //         .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true)
        //         .AddJsonFile("hosting.json", optional: false)
        //         .AddEnvironmentVariables();
        //     // config.SetBasePath(Directory.GetCurrentDirectory())
        //     //     .AddJsonFile("config.json", optional: false, reloadOnChange: true)
        //     //     .AddJsonFile($"config.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        // }
    }
}
