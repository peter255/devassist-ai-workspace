$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not (Test-Path "frontend/devassist-ui/node_modules")) {
    Write-Host "Installing frontend dependencies..."
    npm install --prefix frontend/devassist-ui
}

Write-Host "Starting DevAssist (API + SPA proxy, Visual Studio style)..."
dotnet run --project src/DevAssist.Api --launch-profile http
