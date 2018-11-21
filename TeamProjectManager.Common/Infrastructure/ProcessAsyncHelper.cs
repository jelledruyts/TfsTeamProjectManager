using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Helper class to start asynchronous processes and monitor its work.
    /// </summary>
    public static class ProcessAsyncHelper
    {
        /// <summary>
        /// Starts an asynchronous process and waits for its completion.
        /// </summary>
        /// <param name="command">Executable to start in a new process.</param>
        /// <param name="arguments">Arguments for the new process.</param>
        /// <param name="logOutput">Log standard out to console.</param>
        /// <param name="timeout">Wait for this timeout.</param>
        /// <returns>A promise-like task that contains the result of the process.</returns>
        public static async Task<ProcessResult> ExecuteShellCommand(string command, string arguments,
            bool logOutput = false, int timeout = int.MaxValue)
        {
            var result = new ProcessResult();

            using (var process = new Process())
            {
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = false;

                var outputBuilder = new StringBuilder();
                var outputCloseEvent = new TaskCompletionSource<bool>();

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null)
                    {
                        outputCloseEvent.SetResult(true);
                    }
                    else
                    {
                        if (logOutput)
                        {
                            Console.WriteLine(e.Data);
                        }
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                var errorBuilder = new StringBuilder();
                var errorCloseEvent = new TaskCompletionSource<bool>();

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null)
                    {
                        errorCloseEvent.SetResult(true);
                    }
                    else
                    {
                        if (logOutput)
                        {
                            Console.WriteLine(e.Data);
                        }
                        errorBuilder.AppendLine(e.Data);
                    }
                };

                bool isStarted;

                try
                {
                    isStarted = process.Start();
                }
                catch (Exception error)
                {
                    result.Completed = true;
                    result.ExitCode = -1;
                    result.Output = error.Message;

                    isStarted = false;
                }

                if (!isStarted)
                {
                    return result;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var waitForExit = WaitForExitAsync(process, timeout);

                var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

                if (await Task.WhenAny(Task.Delay(timeout), processTask) == processTask && waitForExit.Result)
                {
                    result.Completed = true;
                    result.ExitCode = process.ExitCode;

                    if (process.ExitCode != 0)
                    {
                        result.Output = $"{outputBuilder}{errorBuilder}";
                    }
                }
                else
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            return result;
        }


        private static Task<bool> WaitForExitAsync(Process process, int timeout = int.MaxValue)
        {
            return Task.Run(() => process.WaitForExit(timeout));
        }


        /// <summary>
        /// The result of the started process.
        /// </summary>
        public struct ProcessResult
        {
            /// <summary>
            /// True, when the process is completed.
            /// </summary>
            public bool Completed;

            /// <summary>
            /// Holds the exit code of the completed process.
            /// </summary>
            public int? ExitCode;

            /// <summary>
            /// Displays the standard out and standard error logs if there was an error code; else string.Empty.
            /// </summary>
            public string Output;
        }
    }
}