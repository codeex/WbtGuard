# WbtGuar
微信搜索`webmote31`，进行沟通交流。 [En](./README.md)
这是一款在windwos运行的服务程序，它可以监视你配置的进程，并跟踪其输出或错误到日志文件。
它具有下列功能：
- 监视指定的进程名称(由配置文件的program:[进程名]指定，切记)
- 如果进程不存在，则启动指定的命令到子进程，可以携带参数、环境变量等。
- 每隔一定的事件就会扫描进程集，可由appsettings.json内的参数（CheckInterval）指定，默认为20s。
- 如果配置了路由进程的输出，则输出进程的控制台输出到指定文件。
- 服务本身的日志写在logs内
- 需要监视的进程配置在`wbtguard.ini`文件内，你可以修改并添加更多配置，配置节必须由`program:`开头。


# 监视配置文件
参数仿照supervisor的配置文件。
```
[program:WebTest]
# 运行目录
directory=C:\WebTest\bin\Debug\net6.0
# 监控的进程命令， 注意其进程名需与 上面配置的 /program: 后面相同，否则会重复启动多个程序。
command=C:\WebTest\bin\Debug\net6.0\WebTest.exe
stderr_logfile=d:\test_stderr.log
stdout_logfile=d:\test_stdout.log
arguments=
#多个环境变量用;分割。
env=
```
# 管理页面
默认启动管理页面 http://localhost:8088 , 可以控制服务重启、停止、清理日志或查看日志（8K）.
![image](https://user-images.githubusercontent.com/3210368/211051096-37f96786-f3d0-4537-bce2-5d5eb881b123.png)

# 运行程序
```shell
WbtGuardService.exe install --autostart 
WbtGuardService.exe start
```

运行命令必须以管理员身份执行。
运行后会作为服务安装在系统内，可以查找 guard service.
如果需要卸载，可以使用`WbtGuardService.exe uninstall`

# 常见问题
![image](https://user-images.githubusercontent.com/3210368/211055963-e13338e1-1628-49ba-8ab8-e8cac4013648.png)
运行命令必须以管理员身份执行。
# 任何意见
可以在issue内提，或联系微信。
