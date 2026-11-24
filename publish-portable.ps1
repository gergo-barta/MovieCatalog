# Builds a self-contained copy of the app that runs on any 64-bit Windows PC
# WITHOUT the .NET SDK or runtime installed. Copy the "portable" folder to a USB stick,
# then on the demo machine double-click MovieCatalog.exe and open http://localhost:5000
dotnet publish MovieCatalog/MovieCatalog.csproj -c Release -r win-x64 --self-contained true -o portable
Write-Host ""
Write-Host "Done. Run portable\MovieCatalog.exe and open http://localhost:5000" -ForegroundColor Green
