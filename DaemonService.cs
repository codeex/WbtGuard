using WbtGuardService.Utils;

using CliWrap;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;



using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using Topshelf.Logging;

using WbtGuardService.Hubs;


namespace WbtGuardService;

public class DaemonService : BackgroundService, IDisposable
{


    private readonly LogWriter _logger;
    private readonly IConfiguration _config;
    private readonly IHubContext<MonitorHub> hubContext;
    private readonly MessageQueueService queueService;
    private List<GuardServiceConfig> _gsc;

    public DaemonService(ILogger<DaemonService> logger, IConfiguration config, IHubContext<MonitorHub> hubContext, MessageQueueService queueService)
    {
        _logger = HostLogger.Current.Get("DaemonService");
        this._config = config;
        this.hubContext = hubContext;
        this.queueService = queueService;
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
        CancellationTokenSource timeoutSource = new CancellationTokenSource();
        CancellationTokenSource waitSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {    
            _logger.Info("检查配置的程序是否启动...");
            foreach (var c in pes)
            {
                var p = c.Execute();
                //await NotifyStatus(c.Name, p);
            }

            timeoutSource.TryReset();
            timeoutSource.CancelAfter(nInterval * 1000);
            var hasCommand = await queueService.Reader.WaitToReadAsync(waitSource.Token);
            if (hasCommand)
            {
                var command = await queueService.Reader.ReadAsync(waitSource.Token);
                {
                    await ExecuteCommand(command, pes);
                }
            }
            //await Task.Delay(nInterval);
        }

        pes.ForEach(x => x.Dispose());
    }

    private async Task NotifyStatus(string name, MyProcessInfo p)
    {
        ProcessRunStatus status;
        var s = p?.Id != null ? "运行" : "停止";        
        
        status = new ProcessRunStatus
        {
            Status = s,
            Pid = p?.Id,
            UpTime = (p?.Id !=null) ? (DateTime.Now - p.StartTime).ToString(@"dd\.hh\:mm\:ss") :"",
        };
       
        await hubContext.Clients.All.SendAsync("Status", new Message
        {
            Command = "Status",
            ProcessName = name ?? p?.ProcessName,
            Content = s,
            Status = status
        });
    }
    private async Task  ExecuteCommand(Message command, List<ProcessExecutor> pes)
    {
        //操作所有进程
        if (command.ProcessName == "[all]" || string.IsNullOrEmpty(command.ProcessName))
        {
            foreach(var pe in pes)
            {
                var p = pe.ExecuteCommand(command.Command, command.Content);

                if (command.Command == "LastLogs")
                {
                    await hubContext.Clients.Client(command.ClientId).SendAsync("LastLogs", new Message
                    {
                        Command = command.Command,
                        ProcessName = pe.Name,
                        Content = p?.ToString()
                    });
                }
                else if(command.Command == "ClearLogs")
                { }
                else if (command.Command == "Status")
                {
                    await NotifyStatus(pe.Name, p as MyProcessInfo);
                }
                else
                {
                    await NotifyStatus(pe.Name, p as MyProcessInfo);
                }
            }
        }
        else
        {
            var pe = pes.FirstOrDefault(x => x.Name == command.ProcessName);
            var p = pe.ExecuteCommand(command.Command, command.Content);

            if(command.Command == "LastLogs")
            {
                await hubContext.Clients.Client(command.ClientId).SendAsync("LastLogs", new Message
                {
                    Command = command.Command,
                    ProcessName = command.ProcessName,
                    Content = p?.ToString()
                });
            }
            else if (command.Command == "ClearLogs")
            { }
            else if (command.Command == "Status")
            {
                await NotifyStatus(pe.Name, p as MyProcessInfo);
            }
            else
            {
                await NotifyStatus(pe.Name, p as MyProcessInfo);
            }
        }
    }
}

public enum DaemonCommand { 
    Init = 0,
    Stop = 1,
    Restart = 2,
    Start = 3
}
