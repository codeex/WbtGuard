using Ancn.WbtGuardService.Utils;

using CliWrap;

using Microsoft.Extensions.Options;



using System;
using System.Diagnostics;
using System.Text;

using Topshelf.Logging;

namespace Ancn.WbtGuardService
{
    public class DaemonService : BackgroundService, IDisposable
    {


        private readonly LogWriter _logger;
        private readonly IConfiguration _config;
        private List<GuardServiceConfig> _gsc;

        public DaemonService(ILogger<DaemonService> logger, IConfiguration config)
        {
            _logger = HostLogger.Current.Get("DaemonService");
            this._config = config;
            _gsc = ParseGuardServiceConfig.Load(_config);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pes = _gsc.Select(x => new ProcessExecutor(x)).ToList();
            int.TryParse(_config["CheckInterval"], out var nInterval);
            if(nInterval <= 50)
            {
                nInterval = 50;
            }
            while (!stoppingToken.IsCancellationRequested)
            {    
                _logger.Info("检查配置的程序是否启动...");
                foreach (var c in pes)
                {
                    c.Execute();                                   
                }
                await Task.Delay(nInterval);
            }

            pes.ForEach(x => x.Dispose());
        }
       
    }

    public enum DaemonCommand { 
        Init = 0,
        Stop = 1,
        Restart = 2,
        Start = 3
    }
}
