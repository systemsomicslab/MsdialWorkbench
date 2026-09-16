#! /usr/bin/env pwsh

Param(
	[string]$OutputPath='',
	[ValidateSet('Release', 'Debug', 'Release vendor unsupported', 'Debug vendor unsupported')]
	[string]$Configuration='Release',
	[ValidateSet('net472', 'net48', 'net481')]
	[string]$Framework='net472'
)

dotnet build .\src\MSDIAL5\MsdialGuiApp\MsdialGuiApp.csproj --configuration $Configuration -p:OutputPath=$OutputPath -p:DebugType=none --framework $Framework
dotnet build .\src\RawDataApp\RawDataViewer\RawDataViewer.csproj --configuration $Configuration -p:OutputPath=$OutputPath -p:DebugType=none --framework $Framework
Copy-Item -Path .\LGPL.txt,.\THIRD-PARTY-LICENSE-README.md,.\README.md -Destination $OutputPath
