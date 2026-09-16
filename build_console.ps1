#! /usr/bin/env pwsh

Param(
    [string]$OutputPath='',
    [ValidateSet('Release', 'Debug', 'Release vendor unsupported', 'Debug vendor unsupported')]
    [string]$Configuration='Debug',
    [ValidateSet('net472', 'net48', 'net8')]
    [string]$Framework='net8',
    [ValidateSet('windows-x64', 'osx-x64', 'linux-x64')]
    [string]$Runtime='linux-x64'
)

$datetime = Get-Date -Format "yyyyMMdd_HHmmss"

$tempPath = Join-Path $OutputPath "temp"

dotnet publish ./tests/MSDIAL5/MsdialCoreTestApp/MsdialCoreTestApp.csproj `
    --configuration $Configuration `
    -p:DebugType=None `
    -p:ErrorOnDuplicatePublishOutputFiles=false `
    -o $tempPath `
    --runtime $Runtime `
    --framework $Framework

Compress-Archive -Path "$tempPath\*" -DestinationPath (Join-Path $OutputPath "msdial5_$datetime.zip")

Remove-Item -Recurse -Force -Path $tempPath

Write-Host "Complete!"

