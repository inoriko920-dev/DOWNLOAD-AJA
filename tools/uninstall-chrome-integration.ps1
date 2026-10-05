$ErrorActionPreference = 'Stop'

$hostName = 'com.downloadaja.native_host'
$registryKey = "HKCU:\Software\Google\Chrome\NativeMessagingHosts\$hostName"
$integrationRoot = Join-Path $env:LOCALAPPDATA 'DownloadAja\BrowserIntegration'

if (Test-Path -LiteralPath $registryKey) {
    Remove-Item -LiteralPath $registryKey -Recurse -Force
}

if (Test-Path -LiteralPath $integrationRoot) {
    Remove-Item -LiteralPath $integrationRoot -Recurse -Force
}

Write-Host 'Registrasi native host DOWNLOAD-AJA sudah dihapus untuk user Windows saat ini.' -ForegroundColor Green
Write-Host 'Jika extension masih terpasang di Chrome, hapus dari chrome://extensions.'
