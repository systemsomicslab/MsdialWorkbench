#! /usr/bin/env pwsh

# Builds the MS-DIAL console release archives:
#   MSDIAL.console.v<version>-windows-net48.zip
#   MSDIAL.console.v<version>-linux-net8.zip
#   MSDIAL.console.v<version>-osx-net8.zip
#
# The Windows build is published WITHOUT a runtime identifier on purpose. With --runtime win-x64 the
# publish flattens System.Data.SQLite's native SQLite.Interop.dll (1.0.112) into the root folder,
# where it shadows x64\SQLite.Interop.dll (1.0.109) that the SCIEX API needs, and every .wiff2 file
# then fails with "file is not a database". The GUI build has the same layout as this one.
# It is also .NET Framework (net48) because the vendor readers are found through the probing paths
# in App.config, which only .NET Framework honours.

Param(
    [string]$OutputPath = './artifacts/console',
    [ValidateSet('windows-net48', 'linux-net8', 'osx-net8')]
    [string[]]$Targets = @('windows-net48', 'linux-net8', 'osx-net8'),
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'tests/MSDIAL5/MsdialCoreTestApp/MsdialCoreTestApp.csproj'

# The same version the binaries report: MsdialMainVersion from Directory.Build.props plus the build date.
$mainVersion = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))).Project.PropertyGroup.MsdialMainVersion |
    Where-Object { $_ } | Select-Object -First 1
$version = "$mainVersion.$(Get-Date -Format 'yyMMdd')"

$publishOptions = @{
    'windows-net48' = @('--framework', 'net48')
    'linux-net8'    = @('--framework', 'net8', '--runtime', 'linux-x64')
    'osx-net8'      = @('--framework', 'net8', '--runtime', 'osx-x64')
}

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

foreach ($target in $Targets) {
    $tempPath = Join-Path $OutputPath "temp-$target"
    if (Test-Path $tempPath) { Remove-Item -Recurse -Force -Path $tempPath }

    dotnet publish $project `
        --configuration $Configuration `
        -p:DebugType=None `
        -p:ErrorOnDuplicatePublishOutputFiles=false `
        -o $tempPath `
        @($publishOptions[$target])
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $target" }

    $archive = Join-Path $OutputPath "MSDIAL.console.v$version-$target.zip"
    Compress-Archive -Path (Join-Path $tempPath '*') -DestinationPath $archive -Force
    Remove-Item -Recurse -Force -Path $tempPath
    Write-Host "Created $archive"
}

Write-Host "Complete!"
