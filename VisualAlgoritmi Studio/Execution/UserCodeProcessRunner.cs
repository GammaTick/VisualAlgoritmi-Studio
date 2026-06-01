using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VisualAlgoritmi_Studio.Execution
{
    internal sealed class UserCodeProcessRunner
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false
        );

        private readonly string _runnerExecutablePath;
        private readonly SemaphoreSlim _standardInputLock = new(1, 1);

        private Process? _runningProcess;
        private bool _stopRequested;

        public event Action<string>? StandardOutputReceived;
        public event Action<string>? StandardErrorReceived;

        public bool IsCodeRunning
        {
            get
            {
                try
                {
                    return _runningProcess != null && !_runningProcess.HasExited;
                }
                catch
                {
                    return false;
                }
            }
        }

        public UserCodeProcessRunner(string? runnerExecutablePath = null)
        {
            _runnerExecutablePath = runnerExecutablePath ?? GetDefaultRunnerExecutablePath();
        }

        public async Task<UserCodeExecutionResult> RunAsync(
            string userAssemblyPath,
            string pipelineName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userAssemblyPath))
            {
                return UserCodeExecutionResult.FailedToStart("User assembly path is empty.");
            }

            if (!File.Exists(userAssemblyPath))
            {
                return UserCodeExecutionResult.FailedToStart("User assembly does not exist: " + userAssemblyPath);
            }

            if (!File.Exists(_runnerExecutablePath))
            {
                return UserCodeExecutionResult.FailedToStart("UserCodeRunner executable was not found: " + _runnerExecutablePath);
            }

            if (IsCodeRunning)
            {
                return UserCodeExecutionResult.FailedToStart("Another user program is already running.");
            }

            _stopRequested = false;

            var startInfo = new ProcessStartInfo
            {
                FileName = _runnerExecutablePath,

                UseShellExecute = false,
                CreateNoWindow = true,

                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,

                StandardInputEncoding = Utf8NoBom,
                StandardOutputEncoding = Utf8NoBom,
                StandardErrorEncoding = Utf8NoBom,

                WorkingDirectory = Path.GetDirectoryName(_runnerExecutablePath)
                                   ?? AppContext.BaseDirectory
            };

            startInfo.ArgumentList.Add(pipelineName);
            startInfo.ArgumentList.Add(userAssemblyPath);

            using var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            var standardOutputBuilder = new StringBuilder();
            var standardErrorBuilder = new StringBuilder();

            try
            {
                bool started = process.Start();

                if (!started)
                {
                    return UserCodeExecutionResult.FailedToStart("Failed to start UserCodeRunner process.");
                }

                _runningProcess = process;

                Task standardOutputTask = ReadConsoleStreamAsync(
                    process.StandardOutput,
                    standardOutputBuilder,
                    text => StandardOutputReceived?.Invoke(text));

                Task standardErrorTask = ReadConsoleStreamAsync(
                    process.StandardError,
                    standardErrorBuilder,
                    text => StandardErrorReceived?.Invoke(text));

                try
                {
                    await process.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _stopRequested = true;

                    await Task.Run(() =>
                    {
                        KillProcessTree(process);
                    }, CancellationToken.None);

                    await WaitForExitIgnoringErrors(process);

                    await WaitForTaskIgnoringErrors(standardOutputTask);
                    await WaitForTaskIgnoringErrors(standardErrorTask);

                    return UserCodeExecutionResult.Cancelled(
                        standardOutputBuilder.ToString(),
                        standardErrorBuilder.ToString());
                }

                await WaitForTaskIgnoringErrors(standardOutputTask);
                await WaitForTaskIgnoringErrors(standardErrorTask);

                string standardOutput = standardOutputBuilder.ToString();
                string standardError = standardErrorBuilder.ToString();

                if (_stopRequested)
                {
                    return UserCodeExecutionResult.Cancelled(
                        standardOutput,
                        standardError);
                }

                int exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    return UserCodeExecutionResult.Success(
                        exitCode,
                        standardOutput,
                        standardError);
                }

                return UserCodeExecutionResult.RuntimeError(
                    exitCode,
                    standardOutput,
                    standardError);
            }
            catch (Exception ex)
            {
                return UserCodeExecutionResult.FailedToStart(ex.ToString());
            }
            finally
            {
                if (ReferenceEquals(_runningProcess, process))
                {
                    _runningProcess = null;
                }

                _stopRequested = false;
            }
        }

        public async Task SendStandardInputLineAsync(string input)
        {
            Process? process = _runningProcess;

            if (process == null)
            {
                return;
            }

            try
            {
                if (process.HasExited)
                {
                    return;
                }

                await _standardInputLock.WaitAsync();

                try
                {
                    await process.StandardInput.WriteLineAsync(input);
                    await process.StandardInput.FlushAsync();
                }
                finally
                {
                    _standardInputLock.Release();
                }
            }
            catch
            {
                // The child process probably exited while input was being sent.
            }
        }

        public async Task StopAsync()
        {
            Process? process = _runningProcess;

            if (process == null)
            {
                return;
            }

            _stopRequested = true;

            await Task.Run(() =>
            {
                KillProcessTree(process);
            });

            await WaitForExitIgnoringErrors(process);

            if (ReferenceEquals(_runningProcess, process))
            {
                _runningProcess = null;
            }
        }

        private static async Task ReadConsoleStreamAsync(TextReader reader, StringBuilder destination, Action<string> onTextReceived)
        {
            char[] buffer = new char[1024];

            try
            {
                while (true)
                {
                    int read = await reader.ReadAsync(buffer, 0, buffer.Length);

                    if (read == 0)
                    {
                        break;
                    }

                    string text = new string(buffer, 0, read);

                    destination.Append(text);
                    onTextReceived(text);
                }
            }
            catch
            {
                // Process exited or stream was closed.
            }
        }

        private static void KillProcessTree(Process process)
        {
            if (OperatingSystem.IsWindows())
            {
                KillProcessTreeWindows(process);
            }
            else
            {
                KillProcessTreeUnix(process);
            }
        }

        private static void KillProcessTreeWindows(Process process)
        {
            int pid;

            try
            {
                pid = process.Id;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            using var killer = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            killer.StartInfo.ArgumentList.Add("/F");
            killer.StartInfo.ArgumentList.Add("/T");
            killer.StartInfo.ArgumentList.Add("/PID");
            killer.StartInfo.ArgumentList.Add(pid.ToString());

            try
            {
                if (killer.Start())
                {
                    killer.WaitForExit(3000);
                }
            }
            catch
            {
                // taskkill failed or process already exited.
            }
        }

        private static void KillProcessTreeUnix(Process process)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        }

        private static async Task WaitForExitIgnoringErrors(Process process)
        {
            try
            {
                await process.WaitForExitAsync(CancellationToken.None);
            }
            catch
            {
                // Ignore cleanup errors.
            }
        }

        private static async Task WaitForTaskIgnoringErrors(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // Ignore stream cleanup errors.
            }
        }

        private static string GetDefaultRunnerExecutablePath()
        {
            string runnerFileName = OperatingSystem.IsWindows()
                ? "UserCodeRunner.exe"
                : "UserCodeRunner";

            return Path.Combine(
                AppContext.BaseDirectory,
                "UserCodeRunner",
                runnerFileName);
        }
    }
}