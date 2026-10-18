#! /usr/bin/env pwsh

# Builds the MS-DIAL console release archives:
#   MSDIAL.console.v<version>-windows-net48.zip   Release (vendor formats supported)
#   MSDIAL.console.v<version>-linux-net8.zip      Release vendor unsupported, self-contained
#   MSDIAL.console.v<version>-osx-net8.zip        Release vendor unsupported, self-contained
#
# The Windows build is .NET Framework (net48) because the vendor readers are found through the
# probing paths in App.config, which only .NET Framework honours. It is published WITHOUT a runtime
# identifier on purpose: with --runtime win-x64 the publish puts System.Data.SQLite's native
# SQLite.Interop.dll (1.0.112) in the root folder, where it shadows x64\SQLite.Interop.dll (1.0.109)
# that the SCIEX API needs, and every .wiff2 file then fails with "file is not a database".
#
# The Linux and macOS builds follow the publish-msdial5-console job in .github/workflows/dotnet_master.yml:
# unlocked restore (--runtime asks for a runtime identifier the lock files do not list, NU1004) that
# leaves the checked-in lock files alone, and RawDataHandler-Vendor-UnSupported taken from ./Assemblies.

Param(
    [string]$OutputPath = './artifacts/console',
    [ValidateSet('windows-net48', 'linux-net8', 'osx-net8')]
    [string[]]$Targets = @('windows-net48', 'linux-net8', 'osx-net8')
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'tests/MSDIAL5/MsdialCoreTestApp/MsdialCoreTestApp.csproj'
$licenseFiles = @('LGPL.txt', 'THIRD-PARTY-LICENSE-README.md', 'README.md') | ForEach-Object { Join-Path $PSScriptRoot $_ }

# The same version the binaries report: MsdialMainVersion from Directory.Build.props plus the build date.
$mainVersion = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))).Project.PropertyGroup.MsdialMainVersion |
    Where-Object { $_ } | Select-Object -First 1
$version = "$mainVersion.$(Get-Date -Format 'yyMMdd')"

$vendorUnsupported = @(
    '--configuration', 'Release vendor unsupported',
    '--framework', 'net8',
    '--self-contained',
    '-p:RestoreLockedMode=false',
    '-p:DownloadLibrary=false',
    "-p:RestoreAdditionalProjectSources=$(Join-Path $PSScriptRoot 'Assemblies')"
)
$publishOptions = @{
    'windows-net48' = @('--configuration', 'Release', '--framework', 'net48')
    'linux-net8'    = $vendorUnsupported + @('--runtime', 'linux-x64')
    'osx-net8'      = $vendorUnsupported + @('--runtime', 'osx-x64')
}

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

# The unlocked restore rewrites the vendor-unsupported lock files (and creates them for projects that have
# none). Keep the checked-in ones as they were: remember them now, put them back when the builds are done.
$lockFileName = 'packages.lock.ReleaseVendorUnsupported.json'
$lockFiles = @{}
Get-ChildItem -Path (Join-Path $PSScriptRoot 'src'), (Join-Path $PSScriptRoot 'tests') -Recurse -File -Filter $lockFileName |
    ForEach-Object { $lockFiles[$_.FullName] = [IO.File]::ReadAllBytes($_.FullName) }

try {
    foreach ($target in $Targets) {
        $tempPath = Join-Path $OutputPath "temp-$target"
        if (Test-Path $tempPath) { Remove-Item -Recurse -Force -Path $tempPath }

        dotnet publish $project `
            -p:DebugType=None `
            -p:ErrorOnDuplicatePublishOutputFiles=false `
            -o $tempPath `
            @($publishOptions[$target])
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $target" }

        Copy-Item -Path $licenseFiles -Destination $tempPath

        $archive = Join-Path $OutputPath "MSDIAL.console.v$version-$target.zip"
        Compress-Archive -Path (Join-Path $tempPath '*') -DestinationPath $archive -Force
        Remove-Item -Recurse -Force -Path $tempPath
        Write-Host "Created $archive"
    }
}
finally {
    Get-ChildItem -Path (Join-Path $PSScriptRoot 'src'), (Join-Path $PSScriptRoot 'tests') -Recurse -File -Filter $lockFileName |
        Where-Object { -not $lockFiles.ContainsKey($_.FullName) } |
        Remove-Item
    foreach ($path in $lockFiles.Keys) { [IO.File]::WriteAllBytes($path, $lockFiles[$path]) }
}

Write-Host "Complete!"
