using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Topshelf.Logging;


namespace WbtGuardService
{
    
    public class GuardService 
    {
        private readonly LogWriter _logger;
        private readonly string[] args;
        private bool _stopRequested;
        private IHost _webHost;

        public GuardService(string[] args)
        {
            this._logger = HostLogger.Current.Get("GuardService");     
            this.args = args;
        }
        public void Start() 
        {
            this._logger.Log(LoggingLevel.Info, "开启服务...");
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(builder =>
                {
                    builder.SetBasePath(AppContext.BaseDirectory);
                    builder.AddIniFile("wbtguard.ini", false);                   
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {                    
                    var url = webBuilder.GetSetting("WebUrl");
                    webBuilder.UseStartup<Startup>().UseUrls();
                })
                .Build();

            var lifeTime = builder.Services.GetRequiredService<IHostApplicationLifetime>();
            lifeTime.ApplicationStopped.Register(() =>
               {
                   if (!_stopRequested)
                       Stop();
               });
            builder.Start();

            _webHost = builder;


        }
        public void Stop() {
            this._logger.Log(LoggingLevel.Info, "停止服务...");
            _stopRequested = true;
            _webHost?.Dispose();
        }
        public void Shutdown()
        {
            _stopRequested = true;
            _webHost?.Dispose();

            this._logger.Log(LoggingLevel.Warn, "计算机关机");
        }
    }
}
