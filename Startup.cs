using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Topshelf.Logging;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using WbtGuardService.Hubs;
using WbtGuardService.Utils;

namespace WbtGuardService
{
    public class Startup
    {
        private readonly LogWriter _logger;
        public Startup(IWebHostEnvironment env)
        {
            this._logger = HostLogger.Current.Get("GuardService");
            HostingEnvironment = env;
        }

        public IWebHostEnvironment HostingEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddSingleton<MessageQueueService>();
            services.AddRazorPages();
            services.AddHostedService<DaemonService>();
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            env.ContentRootPath = AppContext.BaseDirectory;
            var config = app.ApplicationServices.GetService<IConfiguration>();
            bool.TryParse(config["EnableWeb"], out var bEnable);
            var url = config["Kestrel:Endpoints:Http:Url"] ?? config["Kestrel:Endpoints:Https:Url"];
            //var addresses = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
            _logger.Info(bEnable ? $"激活web服务 {url}" : "未激活web服务");
            if (bEnable)
            {
                try
                {
                    // Configure the HTTP request pipeline.
                    if (!env.IsDevelopment())
                    {
                        app.UseExceptionHandler("/Error");
                    }
                    app.UseStaticFiles();

                    app.UseRouting();

                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<MonitorHub>("/monitor");
                        endpoints.MapRazorPages();
                    });
                    _logger.Info($"web服务: {string.Join(",", url)}");

                }
                catch (Exception ex)
                {
                    _logger.Error($"激活web服务 {string.Join(",", url)} 失败,", ex);
                }
            }
        }
    }
}
