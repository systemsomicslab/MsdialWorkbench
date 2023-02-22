#! /usr/bin/env pwsh

Param(
	[string]$OutputPath='',
	[ValidateSet('Release', 'Debug', 'Release vendor unsupported', 'Debug vendor unsupported')]
	[string]$Configuration='Release'
)

dotnet build .\src\MSDIAL5\MsdialGuiApp\MsdialGuiApp.csproj --configuration $Configuration -p:OutputPath=$OutputPath -p:DebugType=none
dotnet build .\src\RawDataApp\RawDataViewer\RawDataViewer.csproj --configuration $Configuration -p:OutputPath=$OutputPath -p:DebugType=none
