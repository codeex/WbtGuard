using CliWrap;

using Microsoft.Win32.SafeHandles;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.Unicode;

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
        private bool _isManualStop = false;

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

        public string Name => this.config.Name;
        /// <summary>
        /// 定时检查执行
        /// </summary>
        /// <returns></returns>
        public virtual Process Execute()
        {
            if (_isManualStop) return null;

            var bDir = !string.IsNullOrEmpty(this.config.Directory);
            Process p = Process.GetProcessesByName(config.Name)?.FirstOrDefault();
            nativeProcess = p;
            if (p == null)
            {
                p = StartProcess();  
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
                        this.RestartProcess();
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
            return p;
        }

        private  Process StopProcess()
        {
            Process p = Process.GetProcessesByName(config.Name)?.FirstOrDefault();
            if (p == null)
            {
                _logger.Info($" {this.config.Name}已经关闭.");
            }
            else 
            {
                try
                {
                    _logger.Info($"关闭程序 {this.config.Name} ...");
                    // kill & restart
                    p.Kill(true);
                    _logger.Info($"关闭程序 {this.config.Name} 成功");
                }
                catch (Exception ex)
                {
                    _logger.Error($"关闭程序 {this.config.Name} 时失败! ", ex);
                }
            }           
            nativeProcess = p;
            return p;
        }
        private Process RestartProcess()
        {
            Process p = null;
            try
            {
                p = this.StopProcess();
                p = this.StartProcess();                
            }
            catch (Exception ex)
            {
                _logger.Error($"执行启动 {this.config.Name} 时失败! ", ex);
            }           
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
            originPid = p?.Id ?? 0;
            nativeProcess = p;
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

        public object ExecuteCommand(string command, string content)
        {
            var redirectStdOut = !string.IsNullOrEmpty(this.config.StdOutFile);
            var redirectStdErr = !string.IsNullOrEmpty(this.config.StdErrorFile);
            Process p = null;
            if(command == "Start")
            {
                _isManualStop = false;
                p = this.StartProcess();
            }
            else if (command == "Stop")
            {
                _isManualStop = true;
                p = this.StopProcess();
            }
            else if (command == "Restart")
            {
                _isManualStop = true;
                p = this.RestartProcess();
            }
            else if(command == "LastLogs")
            {
                if (redirectStdOut)
                {
                    try
                    {         
                        byte[] buffer = new byte[4096];
                        using (var stream = new FileStream(this.config.StdOutFile, FileMode.Open,
                        FileAccess.Read, FileShare.ReadWrite))
                        {
                            long len = stream.Length;
                            stream.Seek(-4096, SeekOrigin.End);
                            stream.Read(buffer);
                            var log = Encoding.UTF8.GetString(buffer);
                            return log;
                        }
                        
                    }
                    catch
                    {
                        _logger.Warn($"打开文件 {this.config.StdOutFile} 失败");                        
                    }
                    return null;
                }
            }
            else if(command == "ClearLogs")
            {
                if (redirectStdOut)
                {
                    try
                    {
                        File.Delete(this.config.StdOutFile);
                        _logger.Warn($"清空文件 {this.config.StdOutFile} 内容");
                    }
                    catch
                    {
                        _logger.Warn($"清空文件 {this.config.StdOutFile} 失败");
                        
                    }

                }
                if (redirectStdErr)
                {
                    try
                    {
                        File.Delete(this.config.StdErrorFile);
                        _logger.Warn($"清空文件 {this.config.StdErrorFile} 内容");
                    }
                    catch
                    {
                        _logger.Warn($"清空文件 {this.config.StdErrorFile} 失败");

                    }
                }
            }
           
            return p;
        }
    }
}
