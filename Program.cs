using WbtGuardService;

using Microsoft.AspNetCore;

using System;
using Topshelf.Logging;
using Topshelf;
using System.Security.Principal;

var identity = WindowsIdentity.GetCurrent();
var principal = new WindowsPrincipal(identity);
if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
{
    Console.WriteLine("����װ��Ҫʹ�ù���ԱȨ�����У�");
    Console.WriteLine("���Ҽ�ʹ�ù���ԱȨ���������У�");
    Console.WriteLine("��������˳�����");
    Console.ReadKey();
    Environment.Exit(exitCode: 0);
}
var logger = HostLogger.Current.Get("UseWindowService");
var rc = HostFactory.Run(x =>
{
    x.UseNLog();
    x.Service<GuardService>(s =>
    {
        s.ConstructUsing(name => new GuardService(args));
        s.WhenStarted(tc => tc.Start());
        s.WhenStopped(tc => tc.Stop());
        s.WhenShutdown(tc => tc.Shutdown());       
    });
    x.RunAsLocalSystem();
    x.EnablePowerEvents();
    

    x.SetDescription("the common guard service, same as  supervisor on linux");
    x.SetDisplayName("Guard Service");
    x.SetServiceName("WbtGuard");
});

var exitCode = (TypeCode)Convert.ChangeType(rc, rc.GetTypeCode());
Environment.ExitCode = (int)exitCode;

if (exitCode != 0)
{
    logger.Error($"����������˳��� {exitCode.ToString()}");
}

