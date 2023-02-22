Param(
	[string]$OutputPath=''
)

dotnet msbuild /t:Build .\src\MSDIAL5\MsdialGuiApp\MsdialGuiApp.csproj /p:Configuration=Release /p:OutputPath=$OutputPath /p:DebugType=none