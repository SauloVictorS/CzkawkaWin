using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CzkawkaWin.Models;

namespace CzkawkaWin.Services
{
    /// <summary>
    /// Service responsible for executing Czkawka CLI operations.
    /// </summary>
    public class CzkawkaService : IDisposable
    {
        private Process? _currentProcess;
        private readonly string _czkawkaExePath;
        private bool _disposed;

        /// <summary>
        /// Raised when output is received from the CLI process.
        /// </summary>
        public event EventHandler<string>? OutputReceived;
        
        /// <summary>
        /// Raised when an error is received from the CLI process.
        /// </summary>
        public event EventHandler<string>? ErrorReceived;
        
        /// <summary>
        /// Raised when the CLI process exits.
        /// </summary>
        public event EventHandler<int>? ProcessExited;

        /// <summary>
        /// Gets whether a scan is currently running.
        /// </summary>
        public bool IsRunning => _currentProcess != null && !_currentProcess.HasExited;

        /// <summary>
        /// Gets the path to the Czkawka CLI executable.
        /// </summary>
        public string ExecutablePath => _czkawkaExePath;

        /// <summary>
        /// Gets whether the Czkawka CLI executable exists.
        /// </summary>
        public bool IsAvailable => File.Exists(_czkawkaExePath);

        /// <summary>
        /// Creates a new instance of CzkawkaService.
        /// </summary>
        /// <param name="customExePath">Optional custom path to czkawka_cli.exe</param>
        public CzkawkaService(string? customExePath = null)
        {
            if (!string.IsNullOrEmpty(customExePath))
            {
                _czkawkaExePath = customExePath;
            }
            else
            {
                // Default: look for czkawka_cli.exe in the application directory
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                _czkawkaExePath = Path.Combine(exeDir, "czkawka_cli.exe");
            }
        }

        /// <summary>
        /// Executes a duplicate file scan with the given configuration.
        /// </summary>
        /// <param name="config">Scan configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the scan operation</returns>
        public async Task<ScanResult> ExecuteScanAsync(
            ScanConfiguration config,
            CancellationToken cancellationToken = default)
        {
            // Validate CLI availability
            if (!IsAvailable)
            {
                return ScanResult.Failed($"Czkawka CLI not found at: {_czkawkaExePath}");
            }

            // Validate configuration
            if (!config.IsValid())
            {
                return ScanResult.Failed(config.GetValidationError());
            }

            // Generate temp path for JSON output if not specified
            var outputJsonPath = config.OutputJsonPath
                ?? Path.Combine(Path.GetTempPath(), $"czkawka_scan_{Guid.NewGuid():N}.json");

            // Build CLI arguments
            var arguments = BuildArguments(config, outputJsonPath);

            OnOutputReceived($"Executing: czkawka_cli.exe {arguments}");

            try
            {
                var exitCode = await RunProcessAsync(arguments, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return ScanResult.Cancelled();
                }

                // Check if output file was created
                if (!File.Exists(outputJsonPath))
                {
                    return ScanResult.Failed($"Output JSON file was not created. Exit code: {exitCode}");
                }

                // Read JSON content
                var jsonContent = await File.ReadAllTextAsync(outputJsonPath, cancellationToken);

                // Check for empty or invalid JSON
                if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent == "{}")
                {
                    return ScanResult.Success(jsonContent, outputJsonPath, isEmpty: true);
                }

                return ScanResult.Success(jsonContent, outputJsonPath);
            }
            catch (OperationCanceledException)
            {
                StopScan();
                return ScanResult.Cancelled();
            }
            catch (Exception ex)
            {
                return ScanResult.Failed($"Error executing scan: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the currently running scan.
        /// </summary>
        public void StopScan()
        {
            if (_currentProcess != null && !_currentProcess.HasExited)
            {
                try
                {
                    OnOutputReceived("Stopping scan process...");
                    _currentProcess.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    OnErrorReceived($"Error stopping process: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Builds CLI arguments from the configuration.
        /// </summary>
        private string BuildArguments(ScanConfiguration config, string outputPath)
        {
            var args = new StringBuilder("dup");

            // Directories
            foreach (var dir in config.SearchDirectories)
            {
                args.Append($" -d \"{dir}\"");
            }

            foreach (var dir in config.ExcludedDirectories)
            {
                args.Append($" -e \"{dir}\"");
            }

            foreach (var dir in config.ReferenceDirectories)
            {
                args.Append($" -r \"{dir}\"");
            }

            foreach (var item in config.ExcludedItems)
            {
                args.Append($" -E \"{item}\"");
            }

            // Extensions - must be comma-separated for Czkawka CLI
            if (config.AllowedExtensions.Count > 0)
            {
                args.Append($" -x \"{string.Join(",", config.AllowedExtensions)}\"");
            }

            if (config.ExcludedExtensions.Count > 0)
            {
                args.Append($" -P \"{string.Join(",", config.ExcludedExtensions)}\"");
            }

            // File size filters
            if (config.MinimalFileSize != 8192)
            {
                args.Append($" -m {config.MinimalFileSize}");
            }

            if (config.MaximalFileSize != long.MaxValue)
            {
                args.Append($" -i {config.MaximalFileSize}");
            }

            // Search method
            args.Append($" -s {config.Method.ToString().ToUpperInvariant()}");

            // Hash type (only relevant for Hash method)
            if (config.Method == SearchMethod.Hash)
            {
                args.Append($" -t {config.HashAlgorithm}");
            }

            // Performance options
            if (config.ThreadNumber > 0)
            {
                args.Append($" -T {config.ThreadNumber}");
            }

            if (!config.UseCache)
            {
                args.Append(" -H");
            }

            if (config.UsePrehashCache)
            {
                args.Append(" -u");
            }

            if (config.MinimalCachedFileSize != 257144)
            {
                args.Append($" -c {config.MinimalCachedFileSize}");
            }

            if (config.MinimalPrehashCacheFileSize != 257144)
            {
                args.Append($" -Z {config.MinimalPrehashCacheFileSize}");
            }

            // Options
            if (!config.Recursive)
            {
                args.Append(" -R");
            }

            if (config.CaseSensitive)
            {
                args.Append(" -l");
            }

            if (config.AllowHardLinks)
            {
                args.Append(" -L");
            }

            // Output - use pretty JSON for better readability
            args.Append($" -p \"{outputPath}\"");

            // Don't print results to console (we capture via JSON)
            args.Append(" -N");

            return args.ToString();
        }

        /// <summary>
        /// Runs the CLI process and captures output.
        /// </summary>
        private async Task<int> RunProcessAsync(string arguments, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<int>();

            _currentProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _czkawkaExePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            _currentProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OnOutputReceived(e.Data);
                }
            };

            _currentProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OnErrorReceived(e.Data);
                }
            };

            _currentProcess.Exited += (sender, e) =>
            {
                var exitCode = _currentProcess?.ExitCode ?? -1;
                ProcessExited?.Invoke(this, exitCode);
                tcs.TrySetResult(exitCode);
            };

            _currentProcess.Start();
            _currentProcess.BeginOutputReadLine();
            _currentProcess.BeginErrorReadLine();

            // Register cancellation
            await using (cancellationToken.Register(() =>
            {
                StopScan();
                tcs.TrySetCanceled(cancellationToken);
            }))
            {
                return await tcs.Task;
            }
        }

        /// <summary>
        /// Helper to raise OutputReceived event.
        /// </summary>
        private void OnOutputReceived(string message)
        {
            OutputReceived?.Invoke(this, message);
        }

        /// <summary>
        /// Helper to raise ErrorReceived event.
        /// </summary>
        private void OnErrorReceived(string message)
        {
            ErrorReceived?.Invoke(this, message);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            StopScan();
            _currentProcess?.Dispose();
            _currentProcess = null;
            _disposed = true;
            
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Result of a scan operation.
    /// </summary>
    public class ScanResult
    {
        /// <summary>
        /// Whether the scan completed successfully.
        /// </summary>
        public bool IsSuccess { get; init; }
        
        /// <summary>
        /// Whether the scan was cancelled.
        /// </summary>
        public bool IsCancelled { get; init; }
        
        /// <summary>
        /// Whether the scan found no duplicates.
        /// </summary>
        public bool IsEmpty { get; init; }
        
        /// <summary>
        /// JSON content of the scan results.
        /// </summary>
        public string? JsonContent { get; init; }
        
        /// <summary>
        /// Path to the JSON output file.
        /// </summary>
        public string? JsonFilePath { get; init; }
        
        /// <summary>
        /// Error message if the scan failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static ScanResult Success(string jsonContent, string jsonPath, bool isEmpty = false)
        {
            return new ScanResult
            {
                IsSuccess = true,
                JsonContent = jsonContent,
                JsonFilePath = jsonPath,
                IsEmpty = isEmpty
            };
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static ScanResult Failed(string errorMessage)
        {
            return new ScanResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Creates a cancelled result.
        /// </summary>
        public static ScanResult Cancelled()
        {
            return new ScanResult
            {
                IsCancelled = true
            };
        }
    }
}
