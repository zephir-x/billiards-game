<#
.SYNOPSIS
    Builds and packages a 100% standalone, self-contained Windows release of Billiards 2D.
.DESCRIPTION
    Compiles the application with all .NET 9 runtimes and native libraries embedded into a
    single self-contained executable. Copies all required Audio assets (BGM/SFX) and native
    dependencies directly into a distribution folder ready for direct zip compression or store release.
#>

param(
    [string]$OutputDir = "dist/BilliardsGame-win-x64",
    [switch]$CreateZip = $true
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Billiards 2D: Standalone Portable Packaging Builder   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Clean previous build artifacts
if (Test-Path $OutputDir) {
    Write-Host "[1/5] Cleaning existing output directory '$OutputDir'..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $OutputDir
}

$zipPath = "$OutputDir.zip"
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

# 2. Execute dotnet publish (Self-Contained Single-File)
Write-Host "[2/5] Publishing Self-Contained win-x64 Release..." -ForegroundColor Green
dotnet publish App/BilliardsGame.App.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# 3. Clean any lingering PDBs from distribution folder
Get-ChildItem -Path $OutputDir -Filter *.pdb -Recurse | Remove-Item -Force

# 4. Guarantee Audio and Asset directories are present and complete
Write-Host "[3/5] Verifying Audio & Asset structures..." -ForegroundColor Green
$audioSource = "Assets/Audio"
$bgmDest = Join-Path $OutputDir "Audio/BGM"
$sfxDest = Join-Path $OutputDir "Audio/SFX"

if (-not (Test-Path $bgmDest)) {
    New-Item -ItemType Directory -Path $bgmDest -Force | Out-Null
}
if (-not (Test-Path $sfxDest)) {
    New-Item -ItemType Directory -Path $sfxDest -Force | Out-Null
}

Copy-Item -Path "$audioSource/BGM/*" -Destination $bgmDest -Recurse -Force
Copy-Item -Path "$audioSource/SFX/*" -Destination $sfxDest -Recurse -Force

# Verify icon
$iconSource = "Assets/icon.png"
if (Test-Path $iconSource) {
    Copy-Item -Path $iconSource -Destination $OutputDir -Force
}

# 5. Verify integrity of output
Write-Host "[4/5] Verifying package integrity..." -ForegroundColor Green
$exeFile = Join-Path $OutputDir "BilliardsGame.App.exe"
if (-not (Test-Path $exeFile)) {
    Write-Error "CRITICAL: Executable '$exeFile' not found!"
    exit 1
}

$exeSizeMB = [math]::Round((Get-Item $exeFile).Length / 1MB, 2)
Write-Host "       Executable: $exeFile ($exeSizeMB MB)" -ForegroundColor White

$bgmCount = (Get-ChildItem -Path $bgmDest -Filter *.mp3).Count
$sfxCount = (Get-ChildItem -Path $sfxDest -Filter *.mp3).Count
Write-Host "       Audio Files: $bgmCount BGM tracks, $sfxCount SFX clips" -ForegroundColor White

# 6. Optionally create ready-to-distribute ZIP archive
if ($CreateZip) {
    Write-Host "[5/5] Creating release zip archive '$zipPath'..." -ForegroundColor Green
    Compress-Archive -Path "$OutputDir/*" -DestinationPath $zipPath -Force
    $zipSizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
    Write-Host "       ZIP created: $zipPath ($zipSizeMB MB)" -ForegroundColor Yellow
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  BUILD SUCCESSFUL! Portable release is ready at:         " -ForegroundColor Green
Write-Host "  Folder: $OutputDir" -ForegroundColor White
if ($CreateZip) {
    Write-Host "  Zip:    $zipPath" -ForegroundColor White
}
Write-Host "==========================================================" -ForegroundColor Cyan
