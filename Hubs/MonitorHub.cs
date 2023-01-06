using Ancn.WbtGuardService;

using CliWrap;

using Microsoft.AspNetCore.SignalR;

using System;
using System.Threading.Tasks;

using WbtGuardService.Utils;

namespace WbtGuardService.Hubs;

public class MonitorHub : Hub<IMonitorClient>
{
    private static int _timeout = 30000;
    public async Task ClearLog(string processName, MessageQueueService service)
    {
        CancellationTokenSource timeoutSource = new CancellationTokenSource(_timeout);
        CancellationTokenSource waitSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, Context.ConnectionAborted);
        await service.SendCommandNoReturn(new Message { 
            ClientId = Context.ConnectionId,
            Command = "ClearLog",
            Content = processName,
            ProcessName = processName,
        }, waitSource.Token);

        if (waitSource.IsCancellationRequested)
        {
            await Clients.Caller.Status(new Message
            {
                Command = "ClearLog",
                ProcessName = processName,
                Content = "未知"
            });
        }
    }
    public List<string> GetServices(IConfiguration config)
    {
        var gsc = ParseGuardServiceConfig.Load(config);
        return gsc.Select(x=>x.Name).ToList();
    }
    public async Task LastLogs(string processName, MessageQueueService service)
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

    public async Task Restart(string processName, MessageQueueService service)
    {
        CancellationTokenSource timeoutSource = new CancellationTokenSource(_timeout);
        CancellationTokenSource waitSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, Context.ConnectionAborted);
       
        await Clients.Caller.Status(new Message
        {
            Command = "Restart",
            ProcessName = processName,
            Content = "重启中"
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

    public async Task Start(string processName, MessageQueueService service)
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

    public async Task Status(string processName, MessageQueueService service)
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

    public async Task Stop(string processName, MessageQueueService service)
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