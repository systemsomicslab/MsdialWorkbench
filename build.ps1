Param(
	[string]$OutputPath=''
)

dotnet restore
dotnet build .\src\MSDIAL5\MsdialGuiApp\MsdialGuiApp.csproj --no-restore --configuration Release -p:OutputPath=$OutputPath -p:DebugType=none
dotnet build .\src\RawDataApp\RawDataViewer\RawDataViewer.csproj --no-restore --configuration Release -p:OutputPath=$OutputPath -p:DebugType=none
