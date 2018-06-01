using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bracken_lrs.GraphQL.Services;
using bracken_lrs.Middleware;
using bracken_lrs.Services;
using bracken_lrs.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hangfire;
using System.Net.WebSockets;
using bracken_lrs.SignalR;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using bracken_lrs.Models.Json;
using MongoDB.Bson;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Schema;
using Serilog;
using bracken_lrs.Attributes;

namespace bracken_lrs
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddSingleton<IxApiService, xApiService>();
            services.AddSingleton<ILrsQueryService, LrsQueryService>();
            services.AddSingleton<IJobQueueService, JobQueueService>();
            services.AddSingleton<IViewModelService, ViewModelService>();
            //services.AddSingleton<IViewModelCreationService, UserViewModelCreationService>();
            services.AddSingleton<IViewModelCreationService, VmStatementCountService>();
            services.AddSingleton<IViewModelCacheService, ViewModelCacheService>();
            services.AddSingleton<IVmLastStatementsService, VmLastStatementsService>();
            services.AddSingleton<IVmVerbStatsService, VmVerbStatsService>();
            services.AddSingleton<IVmProgressService, VmProgressService>();
            services.AddSingleton<IRepositoryService, RepositoryService>();
            services.AddSingleton<ViewUpdateHub>();
            services.AddSingleton<IxApiValidationService, xApiValidationService>();
            services.AddSingleton<IMultipartStatementService, MultipartStatementService>();
            services.AddSingleton<IHttpService, HttpService>();
            services.AddScoped(typeof(TenantAttribute));

            services.AddSignalR();

            // Add Hangfire services. 
            // services.AddHangfire(x => x.UseSqlServerStorage(Configuration.GetConnectionString("HangfireDbConnection")));

            services
                .AddAuthentication("Basic")
                .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>("Basic", null);

            services.AddMvc
            (
                options =>
                options.ModelMetadataDetailsProviders.Add(
                    new SuppressChildValidationMetadataProvider(typeof(BsonDocument)))
            )
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.DateParseHandling  = DateParseHandling.None; // A DateTime stays as string and is validated with the format schema.
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var options = new BackgroundJobServerOptions
            {
                Queues = new[] { "states", "statements", "default" }
            };
            // app.UseHangfireDashboard();
            // app.UseHangfireServer(options);
            app.UseAuthentication();

//             var webSocketOptions = new WebSocketOptions()
//             {
//                 KeepAliveInterval = TimeSpan.FromSeconds(120),
//                 ReceiveBufferSize = 4 * 1024
//             };
//             app.UseWebSockets(webSocketOptions);

//             app.Use(async (context, next) =>
//             {
//                 if (context.Request.Path == "/ws")
//                 {
//                     if (context.WebSockets.IsWebSocketRequest)
//                     {
//                         var webSocket = await context.WebSockets.AcceptWebSocketAsync();
//                         var message = "Hello";
//                         ArraySegment<byte> returnBuffer = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
//  
//                         await webSocket.SendAsync(returnBuffer, WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
//                         //await Echo(context, webSocket);
//                     }
//                     else
//                     {
//                         context.Response.StatusCode = 400;
//                     }
//                 }
//                 else
//                 {
//                     await next();
//                 }

//             });

            app.UseCors(
                builder => builder.AllowAnyMethod()
                    //.AllowAnyOrigin()
                    .WithOrigins(new[] {"http://localhost:3000", "http://live.brackenlearning.com.satoshi.work"})
                    .AllowCredentials()
                    .AllowAnyHeader());

            app.UseSignalR(routes =>
            {
                routes.MapHub<ViewUpdateHub>("viewupdate");
            });

            app.UseMiddleware<HeaderValidationMiddleware>();

            app.UseMvc();

            RegisterJsonNetSchemaLicence();
        }

        private void RegisterJsonNetSchemaLicence()
        {
            string licenseKey = "3654-JivKKvJUxsFIMDlmPva2q8Y7iYGVhTVOYT4/hv2HY3BQDYUN4fCnEdTRjXgm36sIrH4k2ItvjdgVXP5PME3jtkDSZ7yQzzFaJdhdoT7U3gbmDXsgPVEu8pO433x7sluOaRycczvOhST7dfsdqfjxWrLUIPBxZ3Z5hL2SwHWA+Zx7IklkIjozNjU0LCJFeHBpcnlEYXRlIjoiMjAxOS0wNS0wNFQyMTozNToxMC44Mjg2ODI0WiIsIlR5cGUiOiJKc29uU2NoZW1hQnVzaW5lc3MifQ==";
            License.RegisterLicense(licenseKey);
        }
    }
}
