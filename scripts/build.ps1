$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
Push-Location $workspace
try {
    dotnet restore Roselie.sln --disable-parallel -m:1 -nr:false
    if ($LASTEXITCODE -ne 0) { throw 'Dependency restore failed.' }
    dotnet build Roselie.sln -c Release --no-restore -m:1 -nr:false
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet test tests/Roselie.Tests/Roselie.Tests.csproj -c Release --no-build --no-restore -m:1 -nr:false --logger 'trx;LogFileName=verification.trx'
    if ($LASTEXITCODE -ne 0) { throw 'Automated tests failed.' }
} finally { Pop-Location }
