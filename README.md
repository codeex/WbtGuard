# WbtGuar
You can follow me on WeChat(`webmote31`). [中文](./readme-zh.md)
The application is a guard application on windows, you can monitor those processes via the config file.
The config file is wbtguard.ini file.

# Config file
```
[program:WebTest]
# run directory
directory=C:\WebTest\bin\Debug\net6.0
# command
command=C:\WebTest\bin\Debug\net6.0\WebTest.exe
stderr_logfile=d:\test_stderr.log
stdout_logfile=d:\test_stdout.log
arguments=
```
# good luck.
