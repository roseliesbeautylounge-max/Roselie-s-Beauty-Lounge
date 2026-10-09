param([switch]$SkipTests, [switch]$Offline)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
Push-Location $workspace
try {
    if (-not $SkipTests) { & (Join-Path $PSScriptRoot 'build.ps1') }
    function Restore-ForWindows([string]$project) {
        $restoreArguments = @('restore', $project, '-r', 'win-x64', '--force', '--disable-parallel', '-m:1', '-nr:false')
        if ($Offline) { $restoreArguments += @('--ignore-failed-sources', '-p:NuGetAudit=false') }
        & dotnet @restoreArguments
        if ($LASTEXITCODE -ne 0) { throw "Windows dependency restore failed for $project." }
    }
    # Keep the verified release separate from a portable build the user may be running.
    $publishFolder = Join-Path $workspace 'artifacts\app-v0.2.1'
    Restore-ForWindows 'src/Roselie.App/Roselie.App.csproj'
    dotnet publish src/Roselie.App/Roselie.App.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=false -m:1 -nr:false -o $publishFolder
    if ($LASTEXITCODE -ne 0) { throw 'Application publishing failed.' }
    Copy-Item -LiteralPath (Join-Path $workspace 'README.md') -Destination $publishFolder
    Copy-Item -LiteralPath (Join-Path $workspace 'docs\ACCEPTANCE.md') -Destination $publishFolder
    Copy-Item -LiteralPath (Join-Path $workspace 'docs\UI-VALIDATION.md') -Destination $publishFolder
    Copy-Item -LiteralPath (Join-Path $workspace 'docs\INSTALLATION.md') -Destination $publishFolder
    Copy-Item -LiteralPath (Join-Path $workspace 'docs\THIRD-PARTY-NOTICES.md') -Destination $publishFolder
    $packageFolder = ($env:NUGET_PACKAGES)
    if ([string]::IsNullOrWhiteSpace($packageFolder)) { $packageFolder = Join-Path $env:USERPROFILE '.nuget\packages' }
    $runtimeLicense = Join-Path $packageFolder 'microsoft.netcore.app.runtime.win-x64\10.0.12\LICENSE.TXT'
    $runtimeNotices = Join-Path $packageFolder 'microsoft.netcore.app.runtime.win-x64\10.0.12\THIRD-PARTY-NOTICES.TXT'
    if (Test-Path -LiteralPath $runtimeLicense) { Copy-Item -LiteralPath $runtimeLicense -Destination $publishFolder }
    if (Test-Path -LiteralPath $runtimeNotices) { Copy-Item -LiteralPath $runtimeNotices -Destination $publishFolder }
    $payloadFiles = @{}
    Get-ChildItem -LiteralPath $publishFolder -File -Recurse | Where-Object { $_.Name -ne 'install-manifest.json' } | ForEach-Object {
        $relative = $_.FullName.Substring($publishFolder.Length).TrimStart('\', '/')
        $payloadFiles[$relative] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
    @{ Product = 'RoselieBeautyLoungePOS'; Files = $payloadFiles } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $publishFolder 'install-manifest.json') -Encoding utf8
    $payloadArchive = Join-Path $workspace 'artifacts\installer-payload.zip'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path -LiteralPath $payloadArchive) { Remove-Item -LiteralPath $payloadArchive }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($publishFolder, $payloadArchive, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    Restore-ForWindows 'src/Roselie.Setup/Roselie.Setup.csproj'
    dotnet publish src/Roselie.Setup/Roselie.Setup.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -m:1 -nr:false -o artifacts/installer
    if ($LASTEXITCODE -ne 0) { throw 'Installer publishing failed.' }
    Copy-Item -LiteralPath (Join-Path $workspace 'docs\INSTALLATION.md') -Destination (Join-Path $workspace 'artifacts\installer\INSTALLATION.md')
    Write-Host 'Installer: artifacts\installer\Roselie.Setup.exe'
} finally { Pop-Location }
