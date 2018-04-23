﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bracken_lrs.GraphQL.Services;
using bracken_lrs.Security;
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

namespace bracken_lrs
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
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

            services.AddSignalR();

            // Add Hangfire services. 
            services.AddHangfire(x => x.UseSqlServerStorage(Configuration.GetConnectionString("HangfireDbConnection"))); 

            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    //options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    //options.SerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All;
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
            app.UseHangfireDashboard();
            app.UseHangfireServer(options);

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

            app.UseMiddleware<AuthenticationMiddleware>();

            app.UseMvc();
        }
    }
}
