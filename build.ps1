$ErrorActionPreference = "Stop"

$env:DOTNET_CLI_HOME = "c:\Project\JobSearch\.dotnet"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

$projects = @(
    "src/JobSearch.Domain/JobSearch.Domain.csproj",
    "src/JobSearch.Application/JobSearch.Application.csproj",
    "src/JobSearch.Infrastructure/JobSearch.Infrastructure.csproj",
    "src/JobSearch.Contracts/JobSearch.Contracts.csproj",
    "src/JobSearch.Api/JobSearch.Api.csproj",
    "src/JobSearch.Desktop/JobSearch.Desktop.csproj",
    "tests/JobSearch.UnitTests/JobSearch.UnitTests.csproj"
)

foreach ($project in $projects) {
    Write-Host "Building $project"
    dotnet build $project -v minimal

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $project"
    }
}

Write-Host "Build completed successfully."
