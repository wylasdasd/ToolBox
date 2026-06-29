# Build and run ToolBox Web in Docker.
# Usage:
#   .\scripts\deploy-web-docker.ps1              # build + start on port 18488
#   .\scripts\deploy-web-docker.ps1 -Port 3000   # custom host port
#   .\scripts\deploy-web-docker.ps1 -Down          # stop and remove container
#   .\scripts\deploy-web-docker.ps1 -Logs          # follow logs
#   .\scripts\deploy-web-docker.ps1 -NoBuild       # start without rebuild

param(
    [int]$Port = 18488,
    [switch]$Down,
    [switch]$Logs,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker is not installed or not in PATH."
}

Push-Location $RepoRoot
try {
    if ($Down) {
        docker compose down
        Write-Host "ToolBox Web container stopped."
        return
    }

    if ($Logs) {
        docker compose logs -f toolbox-web
        return
    }

    $env:TOOLBOX_WEB_PORT = "$Port"

    if ($NoBuild) {
        docker compose up -d
    }
    else {
        docker compose up -d --build
    }

    if ($LASTEXITCODE -ne 0) {
        throw "docker compose failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "ToolBox Web is running: http://localhost:$Port"
    Write-Host "Stop:  .\scripts\deploy-web-docker.ps1 -Down"
    Write-Host "Logs:  .\scripts\deploy-web-docker.ps1 -Logs"
}
finally {
    Pop-Location
}
