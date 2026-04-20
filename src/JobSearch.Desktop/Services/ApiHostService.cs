using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;

namespace JobSearch.Desktop.Services;

public sealed class ApiHostService : IDisposable
{
    private const string ApiBaseAddress = "http://localhost:5058/";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _startupGate = new(1, 1);
    private Process? _ownedApiProcess;
    private bool _isDisposed;

    public ApiHostService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseAddress)
        };
    }

    public async Task EnsureApiAvailableAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        await _startupGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (await IsApiHealthyAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            if (_ownedApiProcess is null || _ownedApiProcess.HasExited)
            {
                _ownedApiProcess = StartApiProcess();
            }

            await WaitForApiStartupAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _startupGate.Release();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        try
        {
            if (_ownedApiProcess is { HasExited: false })
            {
                // Desktop가 직접 띄운 API만 종료해서 외부에서 실행 중인 인스턴스는 건드리지 않습니다.
                _ownedApiProcess.Kill(entireProcessTree: true);
                _ownedApiProcess.WaitForExit(3000);
            }
        }
        catch (Exception exception)
        {
            Trace.TraceError($"API 프로세스 종료 중 오류가 발생했습니다: {exception}");
        }
        finally
        {
            _ownedApiProcess?.Dispose();
            _startupGate.Dispose();
            _httpClient.Dispose();
        }
    }

    private async Task WaitForApiStartupAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(StartupTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (!linkedCts.IsCancellationRequested)
            {
                if (await IsApiHealthyAsync(linkedCts.Token).ConfigureAwait(false))
                {
                    return;
                }

                await Task.Delay(HealthCheckInterval, linkedCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("JobSearch.Api가 제한 시간 안에 준비되지 않았습니다. API 빌드 상태와 포트 5058 사용 여부를 확인해주세요.");
        }
    }

    private async Task<bool> IsApiHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/health", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var payload = await response.Content
                .ReadFromJsonAsync<ServiceStatusResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return string.Equals(payload?.Status, "Healthy", StringComparison.OrdinalIgnoreCase);
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static Process StartApiProcess()
    {
        var executablePath = FindApiExecutablePath();

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new FileNotFoundException("JobSearch.Api 실행 파일을 찾지 못했습니다. 먼저 솔루션을 빌드한 뒤 다시 시도해주세요.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // launchSettings.json 없이 직접 실행되므로 고정 포트를 환경 변수로 명시합니다.
        startInfo.Environment["ASPNETCORE_URLS"] = ApiBaseAddress.TrimEnd('/');

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("JobSearch.Api 프로세스를 시작하지 못했습니다.");
    }

    private static string? FindApiExecutablePath()
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var binDirectory = baseDirectory
            .AncestorsAndSelf()
            .FirstOrDefault(directory => string.Equals(directory.Name, "bin", StringComparison.OrdinalIgnoreCase));

        if (binDirectory is null)
        {
            return null;
        }

#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif

        var configurationDirectory = Path.Combine(binDirectory.FullName, "JobSearch.Api", configuration);
        if (!Directory.Exists(configurationDirectory))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(configurationDirectory, "JobSearch.Api.exe", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    private sealed record ServiceStatusResponse(string Status, DateTimeOffset CheckedAtUtc);
}

internal static class DirectoryInfoExtensions
{
    public static IEnumerable<DirectoryInfo> AncestorsAndSelf(this DirectoryInfo directory)
    {
        for (var current = directory; current is not null; current = current.Parent)
        {
            yield return current;
        }
    }
}
