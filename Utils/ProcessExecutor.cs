﻿using CliWrap;

using Microsoft.Win32.SafeHandles;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

using Topshelf.Logging;

namespace Ancn.WbtGuardService.Utils
{
    public class ProcessExecutor : IDisposable
    {

        private readonly GuardServiceConfig config;
        private Process nativeProcess;
        private int originPid;
        private FileStream stdoutStream;
        private FileStream stderrorStream;
        private readonly LogWriter _logger;

        public ProcessExecutor(GuardServiceConfig config)
        {
            _logger = HostLogger.Current.Get("ProcessExecutor");
            this.config = config;
            var redirectStdOut = !string.IsNullOrEmpty(this.config.StdOutFile);
            var redirectStdErr = !string.IsNullOrEmpty(this.config.StdErrorFile);

            if (redirectStdOut)
            {
                try
                {
                    stdoutStream = new FileStream(this.config.StdOutFile, FileMode.OpenOrCreate | FileMode.Append,
                    FileAccess.Write, FileShare.ReadWrite);
                }
                catch
                {
                    _logger.Warn($"打开文件 {this.config.StdOutFile} 失败，禁用输出日志");
                    stdoutStream = null;
                }

            }
            if (redirectStdErr)
            {
                try
                {
                    stderrorStream = new FileStream(this.config.StdErrorFile, FileMode.OpenOrCreate | FileMode.Append,
                    FileAccess.Write, FileShare.ReadWrite);
                }
                catch
                {
                    _logger.Warn($"打开文件 {this.config.StdErrorFile} 失败，禁用错误输出日志");
                    stderrorStream = null;
                }
            }
        }

        public virtual Process Execute()
        {
            var bDir = !string.IsNullOrEmpty(this.config.Directory);
            Process p = Process.GetProcessesByName(config.Name)?.FirstOrDefault();
            if (p == null)
            {
                p = StartProcess();            

                originPid = p.Id;
            }
            else if (originPid == 0 || originPid != p.Id)
            {
                if (stdoutStream == null && stderrorStream == null)
                {//不需要日志
                    _logger.Info($"程序 {this.config.Name} 已经在运行中...");
                    originPid = p.Id;                   
                }
                else
                {
                    try
                    {
                        _logger.Info($"关闭程序 {this.config.Name} ...");
                        // kill & restart
                        p.Kill(true);
                        _logger.Info($"关闭程序 {this.config.Name} 成功");

                        p = StartProcess();
                        originPid = p.Id;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"执行启动 {this.config.Name} 时失败! ", ex);
                    }
                }
            }
            else
            {// do nothing

            }
            nativeProcess = p;
            return p;
        }

        private Process StartProcess()
        {
            _logger.Info($"开始程序 {this.config.Name}...");
            var bDir = !string.IsNullOrEmpty(this.config.Directory);
            Process p = Process.GetProcessesByName(config.Name)?.FirstOrDefault();
            if (p == null)
            {
                var startInfo =  new ProcessStartInfo
                {
                    FileName = this.config.Command,
                    UseShellExecute = false,
                    RedirectStandardOutput = stdoutStream != null,
                    RedirectStandardError = stderrorStream != null,
                    WorkingDirectory = bDir ? this.config.Directory : AppDomain.CurrentDomain.BaseDirectory,
                    Arguments = this.config.Arguments,
                    CreateNoWindow = true,                   
                    //StandardOutputEncoding = Encoding.UTF8,
                    //StandardErrorEncoding = Encoding.UTF8,
                };
                foreach (var (key, value) in this.config.GetEnvironmentVariables())
                {
                    if (value is not null)
                    {
                        startInfo.Environment[key] = value;
                    }
                    else
                    {
                        // Null value means we should remove the variable
                        // https://github.com/Tyrrrz/CliWrap/issues/109
                        // https://github.com/dotnet/runtime/issues/34446
                        startInfo.Environment.Remove(key);
                    }
                }
                Console.WriteLine($"start {config.Name}  ....");
                p = new Process
                {
                    StartInfo =startInfo,
                };
                
                p.ErrorDataReceived += P_ErrorDataReceived;
                p.OutputDataReceived += P_OutputDataReceived;

                try
                {
                    if (!p.Start())
                    {
                        throw new InvalidOperationException(
                            $"Failed to start a process with file path '{p.StartInfo.FileName}'. " +
                            "Target file is not an executable or lacks execute permissions."
                        );
                    }

                    if (stdoutStream != null) p.BeginOutputReadLine();
                    if (stderrorStream != null) p.BeginErrorReadLine();
                    _logger.Info($"程序 {this.config.Name} 启动成功.");
                }
                catch (Win32Exception ex)
                {
                    throw new Win32Exception(
                        $"Failed to start a process with file path '{p.StartInfo.FileName}'. " +
                        "Target file or working directory doesn't exist, or the provided credentials are invalid.",
                        ex
                    );
                }
            }

            return p;
        }
        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e?.Data != null)
            {
                try
                {
                    stdoutStream.Write(Encoding.UTF8.GetBytes(e.Data));
                    stdoutStream.Write(Encoding.UTF8.GetBytes("\r\n"));
                    stdoutStream.Flush();
                }
                catch {
                    _logger.Warn($"程序 {this.config.Name} 输出日志失败! ");
                }
            }
        }

        private void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e?.Data != null)
            {
                try
                {
                    stderrorStream.Write(Encoding.UTF8.GetBytes(e.Data));
                    stdoutStream.Write(Encoding.UTF8.GetBytes("\r\n"));
                    stdoutStream.Flush();
                }
                catch { _logger.Warn($"程序 {this.config.Name} 输出错误日志失败! "); }
            }
        }

        public void Dispose()
        {
            if (stdoutStream != null)
            {
                stdoutStream.Close();
                stdoutStream.Dispose();
                _logger.Warn($"程序 {this.config.Name} 输出日志关闭! ");
            }
            if (stderrorStream != null)
            {
                stderrorStream.Close();
                stderrorStream.Dispose();
                _logger.Warn($"程序 {this.config.Name} 输出错误日志关闭! ");
            }
        }
    }
}