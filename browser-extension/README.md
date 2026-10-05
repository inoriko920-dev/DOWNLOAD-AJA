# DOWNLOAD-AJA Chrome MV3 Integration

Status: **RECONSTRUCTED recovery implementation**.

## Alur

```text
Chrome MV3 extension
  -> Chrome Native Messaging
  -> DownloadAja.NativeHost.exe
  -> per-user named pipe
  -> DOWNLOAD-AJA desktop
  -> AddDownloadService
  -> persisted queue
  -> aria2
```

Acknowledgement `accepted=true` dari extension baru diberikan setelah aplikasi desktop benar-benar menerima request. Untuk `add-url`, desktop melewati validasi R7 dan menyimpan item ke queue/history sebelum response sukses dikirim.

## Extension ID recovery baseline

Manifest memakai public `key` tetap agar ID unpacked konsisten:

`noobgcmaelhpcooeoflkjnkolkhcpmcl`

Native host hanya mengizinkan origin extension tersebut.

## Instalasi lokal

Setelah portable/publish berisi minimal:

- `Download Aja.exe`
- `DownloadAja.NativeHost.exe`
- folder `browser-extension/chrome-mv3`

jalankan:

```powershell
powershell -ExecutionPolicy Bypass -File tools/install-chrome-integration.ps1 -InstallRoot "C:\path\ke\DOWNLOAD-AJA"
```

Script mendaftarkan native host ke **HKCU**, jadi baseline ini tidak membutuhkan hak Administrator.

Kemudian:

1. buka `chrome://extensions`;
2. aktifkan **Developer mode**;
3. pilih **Load unpacked**;
4. pilih folder extension yang ditampilkan installer;
5. buka **Extension options**;
6. tekan **Tes Koneksi**.

## Fitur

- context menu untuk link/media/page;
- toolbar action untuk URL tab aktif;
- native messaging protocol v1;
- native host dapat menjalankan DOWNLOAD-AJA jika desktop belum aktif;
- URL diteruskan ke single desktop instance;
- optional browser-download interception;
- interception **OFF secara default**;
- Chrome tidak membatalkan download jika DOWNLOAD-AJA/native host gagal atau menolak URL;
- opsi start queue setelah URL diterima.

## Uninstall integrasi lokal

```powershell
powershell -ExecutionPolicy Bypass -File tools/uninstall-chrome-integration.ps1
```

Lalu hapus extension dari `chrome://extensions` jika masih terpasang.

## Batas recovery saat ini

Ini adalah integrasi Chrome lokal/unpacked untuk baseline recovery. Distribusi melalui Chrome Web Store atau managed enterprise policy belum termasuk R9.
