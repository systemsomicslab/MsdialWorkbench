#! /usr/bin/env pwsh

Param(
	[string]$OutputPath='',
	[ValidateSet('Release', 'Debug', 'Release vendor unsupported', 'Debug vendor unsupported')]
	[string]$Configuration='Release'
)

dotnet build .\src\MSDIAL5.Build.sln --configuration $Configuration -p:OutputPath=$OutputPath -p:DebugType=none
