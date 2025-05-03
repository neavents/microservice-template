using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Threading; // Add CancellationToken namespace
using System.Threading.Tasks; // Add Task namespace

namespace TemporaryName.Tools.Persistence.Migrations.Implementations.Runners.EfCore;

public class ProcessRunner(ILogger<ProcessRunner> logger)
{
    private readonly ILogger<ProcessRunner> _logger = logger;
    private static readonly TimeSpan DefaultProcessTimeout = TimeSpan.FromMinutes(5); // Set a reasonable default timeout

    public async Task<bool> RunProcessAsync(string command, string arguments, string workingDirectory, TimeSpan? timeout = null)
    {
        timeout ??= DefaultProcessTimeout; // Use default timeout if none provided
        _logger.LogInformation("Executing command: {Command} {Arguments} in {WorkingDirectory} (Timeout: {Timeout})",
            command, arguments, workingDirectory, timeout);

        using CancellationTokenSource cts = new(timeout.Value); // Create CTS with timeout

        ProcessStartInfo startInfo = new(command, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        StringBuilder outputBuilder = new();
        StringBuilder errorBuilder = new();
        bool success = false;
        int exitCode = -1;

        try
        {
            using Process process = new() { StartInfo = startInfo };

            // Use TaskCompletionSource to properly handle async reading completion
            TaskCompletionSource<bool> tcsOutput = new();
            TaskCompletionSource<bool> tcsError = new();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    _logger.LogTrace("STDOUT: {OutputData}", e.Data);
                    outputBuilder.AppendLine(e.Data);
                }
                else
                {
                    // End of stream
                    tcsOutput.TrySetResult(true);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                     _logger.LogTrace("STDERR: {ErrorData}", e.Data);
                     errorBuilder.AppendLine(e.Data);
                }
                else
                {
                    // End of stream
                    tcsError.TrySetResult(true);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for the process to exit OR the timeout CancellationToken to trigger
            await process.WaitForExitAsync(cts.Token);

             // Ensure output/error streams are fully processed after exit
            await Task.WhenAll(tcsOutput.Task, tcsError.Task);

            exitCode = process.ExitCode; // Get exit code AFTER waiting

            if (exitCode == 0)
            {
                _logger.LogInformation("Command executed successfully.");
                if (errorBuilder.Length > 0)
                {
                    _logger.LogInformation("Standard Error Output (Exit Code 0):\n{StandardError}", errorBuilder.ToString());
                }
                success = true;
            }
            else
            {
                _logger.LogError("Command failed with exit code {ExitCode}.", exitCode);
                _logger.LogError("Standard Output:\n{StandardOutput}", outputBuilder.ToString());
                _logger.LogError("Standard Error:\n{StandardError}", errorBuilder.ToString());
                success = false;
            }
        }
        catch (OperationCanceledException ex) // Catch cancellation due to timeout
        {
            _logger.LogError(ex, "The process {Command} {Arguments} timed out after {Timeout}.", command, arguments, timeout);
            // Log any partial output collected before timeout
            if(outputBuilder.Length > 0) _logger.LogError("Partial Standard Output:\n{StandardOutput}", outputBuilder.ToString());
            if(errorBuilder.Length > 0) _logger.LogError("Partial Standard Error:\n{StandardError}", errorBuilder.ToString());

            success = false;
             // Optionally try to kill the process if it timed out
            // try { process?.Kill(true); } catch { /* Ignore errors trying to kill */ }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred while running the process {Command} {Arguments}.", command, arguments);
            success = false;
        }

        return success;
    }
}