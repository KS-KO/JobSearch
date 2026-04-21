using System.Diagnostics;
using System.IO;
using System.Text;

namespace JobSearch.Desktop.Services;

public sealed class CollectorHostService
{
    private static readonly TimeSpan CollectorTimeout = TimeSpan.FromMinutes(3);
    private readonly SemaphoreSlim _executionGate = new(1, 1);

    public async Task<CollectorRunResult> RunAsync(CancellationToken cancellationToken)
    {
        await _executionGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var collectorScriptPath = FindCollectorScriptPath();
            if (string.IsNullOrWhiteSpace(collectorScriptPath))
            {
                throw new FileNotFoundException("python-collector 스크립트를 찾지 못했습니다. tools/python-collector/main.py 경로를 확인해주세요.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                ArgumentList = { collectorScriptPath },
                WorkingDirectory = Path.GetDirectoryName(collectorScriptPath) ?? AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Python 수집기 프로세스를 시작하지 못했습니다.");

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeoutCts = new CancellationTokenSource(CollectorTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                TryTerminateProcess(process);
                throw new TimeoutException("사람인/잡코리아 수집이 제한 시간 안에 끝나지 않았습니다.");
            }

            var standardOutput = await standardOutputTask.ConfigureAwait(false);
            var standardError = await standardErrorTask.ConfigureAwait(false);

            return new CollectorRunResult(
                process.ExitCode == 0,
                process.ExitCode,
                standardOutput.Trim(),
                standardError.Trim());
        }
        finally
        {
            _executionGate.Release();
        }
    }

    private static string? FindCollectorScriptPath()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, "tools", "python-collector", "main.py");
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(3000);
            }
        }
        catch
        {
        }
    }
}

public sealed record CollectorRunResult(
    bool Succeeded,
    int ExitCode,
    string StandardOutput,
    string StandardError);
