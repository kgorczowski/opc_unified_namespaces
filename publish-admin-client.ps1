# publish-admin-client.ps1
# Publishes the WPF admin client as ClickOnce and copies it to the nginx static folder.
# Run from the repository root:
#   .\publish-admin-client.ps1 [-InstallUrl "http://your-server:8080/"]

param(
    [string]$InstallUrl = "http://localhost:8080/",
    [string]$Configuration = "Release",
    [string]$OutputDir = "publish\admin-client"
)

$ErrorActionPreference = "Stop"
$Project = "OPCGateway.Admin.Client.Wpf\OPCGateway.Admin.Client.Wpf.csproj"

Write-Host "Building OPCGateway Admin Client..." -ForegroundColor Cyan

dotnet publish $Project `
    --configuration $Configuration `
    --output $OutputDir `
    /p:PublishProfile=ClickOnce `
    /p:InstallUrl=$InstallUrl `
    /p:PublishUrl="$OutputDir\" `
    /p:ApplicationVersion="1.0.0.$((Get-Date).ToString('yyMMdd'))" `
    /p:MapFileExtensions=true `
    /p:UpdateEnabled=true `
    /p:UpdateMode=Foreground

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "ClickOnce package published to: $OutputDir" -ForegroundColor Green
Write-Host "Install URL: $InstallUrl" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Start docker compose:  docker compose up -d admin-client-host"
Write-Host "  2. Open browser:          $InstallUrl"
Write-Host "  3. Click the .application file to install"
