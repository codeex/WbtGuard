using Ancn.WbtGuardService;

using Microsoft.AspNetCore;

using System;
using Topshelf.Logging;
using Topshelf;

var logger = HostLogger.Current.Get("UseWindowService");
var rc = HostFactory.Run(x =>
{
    x.UseNLog();
    x.Service<GuardService>(s =>
    {
        s.ConstructUsing(name => new GuardService(args));
        s.WhenStarted(tc => tc.Start());
        s.WhenStopped(tc => tc.Stop());
    });
    x.RunAsLocalSystem();

    x.SetDescription("the common guard service, same as  supervisor on linux");
    x.SetDisplayName("Guard Service");
    x.SetServiceName("WbtGuard");
});

var exitCode = (TypeCode)Convert.ChangeType(rc, rc.GetTypeCode());
Environment.ExitCode = (int)exitCode;

if (exitCode != 0)
{
    logger.Error($"程序非正常退出： {exitCode.ToString()}");
}

