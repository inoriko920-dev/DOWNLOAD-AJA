param(
    [string]$InstallRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$ExtensionSource = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..')).Path 'browser-extension\chrome-mv3')
)

$ErrorActionPreference = 'Stop'

$hostName = 'com.downloadaja.native_host'
$extensionId = 'noobgcmaelhpcooeoflkjnkolkhcpmcl'
$nativeHostExe = Join-Path $InstallRoot 'DownloadAja.NativeHost.exe'
$desktopExe = Join-Path $InstallRoot 'Download Aja.exe'

if (-not (Test-Path -LiteralPath $nativeHostExe -PathType Leaf)) {
    throw "DownloadAja.NativeHost.exe tidak ditemukan di: $nativeHostExe`nGunakan -InstallRoot yang menunjuk ke folder portable/publish DOWNLOAD-AJA."
}

if (-not (Test-Path -LiteralPath $desktopExe -PathType Leaf)) {
    throw "Download Aja.exe tidak ditemukan di: $desktopExe`nNative host dan aplikasi desktop harus berada dalam folder yang sama."
}

if (-not (Test-Path -LiteralPath $ExtensionSource -PathType Container)) {
    throw "Folder Chrome extension tidak ditemukan di: $ExtensionSource"
}

$integrationRoot = Join-Path $env:LOCALAPPDATA 'DownloadAja\BrowserIntegration'
$extensionTarget = Join-Path $integrationRoot 'chrome-mv3'
$manifestPath = Join-Path $integrationRoot "$hostName.json"

New-Item -ItemType Directory -Path $integrationRoot -Force | Out-Null

if (Test-Path -LiteralPath $extensionTarget) {
    Remove-Item -LiteralPath $extensionTarget -Recurse -Force
}
Copy-Item -LiteralPath $ExtensionSource -Destination $extensionTarget -Recurse -Force

$manifest = [ordered]@{
    name = $hostName
    description = 'DOWNLOAD-AJA Chrome native messaging host'
    path = (Resolve-Path -LiteralPath $nativeHostExe).Path
    type = 'stdio'
    allowed_origins = @("chrome-extension://$extensionId/")
}

$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

$registryKey = "HKCU:\Software\Google\Chrome\NativeMessagingHosts\$hostName"
New-Item -Path $registryKey -Force | Out-Null
Set-Item -Path $registryKey -Value $manifestPath

Write-Host ''
Write-Host 'Integrasi browser DOWNLOAD-AJA berhasil didaftarkan untuk user Windows saat ini.' -ForegroundColor Green
Write-Host "Native host : $nativeHostExe"
Write-Host "Manifest    : $manifestPath"
Write-Host "Extension   : $extensionTarget"
Write-Host ''
Write-Host 'Langkah Chrome:' -ForegroundColor Cyan
Write-Host '1. Buka chrome://extensions'
Write-Host '2. Aktifkan Developer mode'
Write-Host '3. Klik Load unpacked'
Write-Host "4. Pilih folder: $extensionTarget"
Write-Host "5. Extension ID yang diharapkan: $extensionId"
Write-Host '6. Buka Details > Extension options > Tes Koneksi'
