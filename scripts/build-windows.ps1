# MifareOneTool — Windows Release Build Script
# Run from repo root: .\scripts\build-windows.ps1
param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$ProjectDir = Join-Path $RepoRoot "src\MifareOneTool.UI"
$OutDir = Join-Path $RepoRoot "publish\win-x64"
$ZipPath = Join-Path $RepoRoot "publish\MifareOneTool-$Version-win-x64.zip"

Write-Host "=== MifareOneTool Windows Build ===" -ForegroundColor Cyan
Write-Host "Version : $Version"
Write-Host "Output  : $OutDir"

# Check nfc-bin has tools
$NfcBin = Join-Path $ProjectDir "nfc-bin"
$RequiredTools = @("nfc-scan-device.exe","nfc-mfclassic.exe","nfc-list.exe","mfoc.exe","nfc-mfsetuid.exe")
$Missing = $RequiredTools | Where-Object { -not (Test-Path (Join-Path $NfcBin $_)) }
if ($Missing) {
    Write-Warning "Missing NFC tools in nfc-bin\: $($Missing -join ', ')"
    Write-Warning "The app will still run but NFC operations will fail without them."
}

# Publish
Write-Host "`nPublishing..." -ForegroundColor Yellow
dotnet publish $ProjectDir `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:PublishReadyToRun=true `
    --output $OutDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Pack into ZIP
Write-Host "`nCreating ZIP..." -ForegroundColor Yellow
if (Test-Path $ZipPath) { Remove-Item $ZipPath }
Compress-Archive -Path "$OutDir\*" -DestinationPath $ZipPath

$SizeMB = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
Write-Host "`n=== Done ===" -ForegroundColor Green
Write-Host "ZIP : $ZipPath ($SizeMB MB)"
Write-Host ""
Write-Host "Contents of publish\win-x64\:"
Get-ChildItem $OutDir | Format-Table Name, Length -AutoSize
