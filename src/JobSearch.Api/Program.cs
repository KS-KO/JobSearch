using JobSearch.Api.Services;
using JobSearch.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
var configuredUrls = builder.Configuration["ASPNETCORE_URLS"]
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var canRedirectToHttps = configuredUrls?
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) == true;
var resolvedConnectionString = ResolveSqliteConnectionString(
    builder.Environment.ContentRootPath,
    builder.Configuration.GetConnectionString("DefaultConnection"));

// Add Services
builder.Services.AddControllers();
builder.Services.AddDbContext<JobSearchDbContext>(options =>
    options.UseSqlite(resolvedConnectionString));

builder.Services.AddScoped<RecommendationQueryService>();

var app = builder.Build();

// Ensure Database is Created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<JobSearchDbContext>();
    context.Database.EnsureCreated();
}

// Desktop 클라이언트는 로컬 HTTP 주소로 API를 기동하므로, 실제 HTTPS 엔드포인트가
// 구성된 경우에만 리다이렉션을 활성화해 조회 요청이 중간에 끊기지 않도록 보호합니다.
if (canRedirectToHttps)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

static string ResolveSqliteConnectionString(string contentRootPath, string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("SQLite 연결 문자열이 비어 있습니다. appsettings.json 구성을 확인해주세요.");
    }

    var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(connectionStringBuilder.DataSource))
    {
        throw new InvalidOperationException("SQLite 데이터베이스 경로가 비어 있습니다. ConnectionStrings:DefaultConnection을 확인해주세요.");
    }

    if (Path.IsPathRooted(connectionStringBuilder.DataSource))
    {
        return connectionStringBuilder.ToString();
    }

    // 솔루션 루트의 DB를 우선 선택해 빌드 출력 폴더에 생성된 빈 DB를 잘못 읽지 않도록 합니다.
    var currentDirectory = new DirectoryInfo(contentRootPath);
    while (currentDirectory is not null)
    {
        var solutionFilePath = Path.Combine(currentDirectory.FullName, "JobSearch.slnx");
        var rootDatabasePath = Path.Combine(currentDirectory.FullName, "JobSearch.db");
        if (File.Exists(solutionFilePath) && File.Exists(rootDatabasePath))
        {
            connectionStringBuilder.DataSource = rootDatabasePath;
            return connectionStringBuilder.ToString();
        }

        currentDirectory = currentDirectory.Parent;
    }

    // 솔루션 루트를 찾지 못한 경우에만 원래 상대 경로를 기준으로 후보를 탐색합니다.
    currentDirectory = new DirectoryInfo(contentRootPath);
    while (currentDirectory is not null)
    {
        var candidatePath = Path.GetFullPath(Path.Combine(currentDirectory.FullName, connectionStringBuilder.DataSource));
        if (File.Exists(candidatePath))
        {
            connectionStringBuilder.DataSource = candidatePath;
            return connectionStringBuilder.ToString();
        }

        currentDirectory = currentDirectory.Parent;
    }

    connectionStringBuilder.DataSource = Path.GetFullPath(
        Path.Combine(contentRootPath, connectionStringBuilder.DataSource));
    return connectionStringBuilder.ToString();
}
