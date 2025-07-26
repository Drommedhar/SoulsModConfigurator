using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using SoulsModConfigurator;

namespace SoulsModConfigurator.Helpers
{
    /// <summary>
    /// Helper class for capturing process output and updating UI in real-time
    /// </summary>
    public class ProcessOutputCapture
    {
        private readonly Action<string> _statusUpdater;
        private readonly StringBuilder _outputBuffer;

        public ProcessOutputCapture(Action<string> statusUpdater)
        {
            _statusUpdater = statusUpdater;
            _outputBuffer = new StringBuilder();
        }

        /// <summary>
        /// Runs a process and captures its output, updating the UI in real-time
        /// </summary>
        public async Task<(bool Success, string Output)> RunProcessWithOutputCaptureAsync(
            string fileName, 
            string arguments = "", 
            string workingDirectory = "",
            int timeoutMilliseconds = 300000) // 5 minutes default timeout
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = processInfo };
                
                // Set up output event handlers
                process.OutputDataReceived += OnOutputDataReceived;
                process.ErrorDataReceived += OnErrorDataReceived;

                _statusUpdater($"Starting {System.IO.Path.GetFileName(fileName)}...");
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for process completion with timeout
                bool processCompleted = await Task.Run(() => process.WaitForExit(timeoutMilliseconds));
                
                if (!processCompleted)
                {
                    _statusUpdater("Process timed out. Terminating...");
                    process.Kill();
                    return (false, "Process timed out");
                }

                var exitCode = process.ExitCode;
                var output = _outputBuffer.ToString();

                _statusUpdater($"Process completed with exit code: {exitCode}");
                
                return (exitCode == 0, output);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error running process: {ex.Message}";
                _statusUpdater(errorMessage);
                return (false, errorMessage);
            }
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _outputBuffer.AppendLine(e.Data);
                
                // Update UI with the output line (truncate if too long for display)
                var displayText = e.Data.Length > 100 ? e.Data.Substring(0, 97) + "..." : e.Data;
                _statusUpdater($"Output: {displayText}");
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _outputBuffer.AppendLine($"ERROR: {e.Data}");
                
                // Update UI with the error line (truncate if too long for display)
                var displayText = e.Data.Length > 100 ? e.Data.Substring(0, 97) + "..." : e.Data;
                _statusUpdater($"Error: {displayText}");
            }
        }

        /// <summary>
        /// Runs a process without output capture (for UI processes)
        /// </summary>
        public async Task<bool> RunProcessAsync(
            string fileName, 
            string arguments = "", 
            string workingDirectory = "",
            bool useShellExecute = true)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                    UseShellExecute = useShellExecute,
                    CreateNoWindow = !useShellExecute
                };

                _statusUpdater($"Running {System.IO.Path.GetFileName(fileName)}...");
                
                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    _statusUpdater("Failed to start process");
                    return false;
                }

                await Task.Run(() => process.WaitForExit());
                
                var exitCode = process.ExitCode;
                _statusUpdater($"Process completed with exit code: {exitCode}");
                
                return exitCode == 0;
            }
            catch (Exception ex)
            {
                _statusUpdater($"Error running process: {ex.Message}");
                return false;
            }
        }
    }
}