[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

Push-Location (Resolve-Path "$PSScriptRoot\..")
try {
    dotnet pack KoreForge.Logging.sln -c $Configuration
    Write-Host 'Packages written to artifacts/.' -ForegroundColor Green
} finally {
    Pop-Location
}