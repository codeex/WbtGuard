using WbtGuardService;

using CliWrap;

using Microsoft.AspNetCore.SignalR;

using System;
using System.Threading.Tasks;

using WbtGuardService.Utils;

namespace WbtGuardService.Hubs;

public class MonitorHub : Hub<IMonitorClient>
{
    public MonitorHub(MessageQueueService service, IConfiguration config)
    {
        this.service = service;
        this.config = config;
    }
    private static int _timeout = 30000;
    private readonly MessageQueueService service;
    private readonly IConfiguration config;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        //await this.Status(null);
    }

    public async Task ClearLogs(string processName)
    {
        CancellationTokenSource timeoutSource = new CancellationTokenSource(_timeout);
        CancellationTokenSource waitSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, Context.ConnectionAborted);
        await service.SendCommand(new Message { 
            ClientId = Context.ConnectionId,
            Command = "ClearLogs",
            Content = processName,
            ProcessName = processName,
        }, waitSource.Token);

        
    }
    public List<string> GetServices()
    {
        var gsc = ParseGuardServiceConfig.Load(config);
        return gsc.Select(x=>x.Name).ToList();
    }
    public async Task LastLogs(string processName)
    {
        CancellationTokenSource timeoutSource = new CancellationTokenSource(_timeout);
        CancellationTokenSource waitSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, Context.ConnectionAborted);
        await service.SendCommand(new Message
        {
            ClientId = Context.ConnectionId,
            Command = "LastLogs",
            Content = processName,
            ProcessName = processName,
        }, waitSource.Token);

        if (waitSource.IsCancellationRequested)
        {
            await Clients.Caller.LastLogs(new Message
            {
                Command = "ClearLog",
                ProcessName = processName,
                Content = "未知"
            });
        }

    }

    public async Task Restart(string processName)
    {
        CancellationTokenSource timeoutSource = new CancellationTokenSource(_timeout);
        CancellationTokenSource waitSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, Context.ConnectionAborted);
       
        await Clients.Caller.Status(new Message
        {
            Command = "Restart",
            ProcessName = processName,
            Content = "重启中",
            Status = new ProcessRunStatus
            {
                Status = "重启中",                
            }
        });        
       
        await service.SendCommand(new Message
        {
            ClientId = Context.ConnectionId,
            Command = "Restart",
            Content = processName,
            ProcessName = processName,
        }, waitSource.Token);

        if (waitSource.IsCancellationRequested)
        {
            await Clients.Caller.Status(new Message
            {
                Command = "Restart",
                ProcessName = processName,
                Content = "未知"
            });
        }
    }

    public async Task Start(string processName)
    {
        CancellationTokenSource timeoutSource = new CancellationTokenSource(_timeout);
        CancellationTokenSource waitSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, Context.ConnectionAborted);
        await service.SendCommand(new Message
        {
            ClientId = Context.ConnectionId,
            Command = "Start",
            Content = processName,
            ProcessName = processName,
        }, waitSource.Token);

    }

    public async Task Status(string processName)
    {
        CancellationTokenSource timeoutSource = new CancellationTokenSource(_timeout);
        CancellationTokenSource waitSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, Context.ConnectionAborted);
        await service.SendCommand(new Message
        {
            ClientId = Context.ConnectionId,
            Command = "Status",
            Content = processName,
            ProcessName = processName,
        }, waitSource.Token);

    }

    public async Task Stop(string processName)
    {
        CancellationTokenSource timeoutSource = new CancellationTokenSource(_timeout);
        CancellationTokenSource waitSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, Context.ConnectionAborted);
        await service.SendCommand(new Message
        {
            ClientId = Context.ConnectionId,
            Command = "Stop",
            Content = processName,
            ProcessName = processName,
        }, waitSource.Token);

    }
}