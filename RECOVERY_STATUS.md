# DOWNLOAD-AJA — Recovery Status

Tanggal audit awal: 2026-10-06

## Status keseluruhan

**RECOVERY IN PROGRESS — SOURCE LAMA BELUM TERPULIHKAN**

Repo baru ini sengaja dimulai dalam mode pemulihan. Jangan menganggap implementasi baru sebagai source lama sampai sumber historis berhasil dibedakan.

## Repo

- Repo aktif: `inoriko920-dev/DOWNLOAD-AJA`
- Repo migrasi lama yang tercatat: `tonitaru6-cloud/Download-Aja-`
- Repo lama saat audit: tidak dapat diakses melalui koneksi GitHub (404)

## Bukti yang sudah ditemukan

### VERIFIED — Library / arsip
- target produk **IDM-like**;
- fix commit historis `31e335c`;
- final portable build success historis `da2aa9c`;
- tests/artifacts pada tahap tersebut dilaporkan PASS;
- referensi UI `Download Aja Manager Interface.png` dan `Download-Aja_UI_Reference.png`;
- master workflow ASTRA/SOL umum tersedia di Library.

### HISTORICAL CHAT — perlu diverifikasi ulang jika source ditemukan
Riwayat percakapan lama mencatat C# / .NET 8 / WPF, aria2 portable, Chrome MV3 + bridge lokal, segmented HTTP/HTTPS, pause/resume/stop/delete, progress/speed/ETA, queue/scheduler, browser integration, Windows build, dan portable artifact.

## Klasifikasi file selama recovery

- `ORIGINAL` — source/file yang benar-benar berasal dari repo lama.
- `RECOVERED` — dipulihkan dari artifact/build/cache/bytecode atau sumber teknis lain.
- `RECONSTRUCTED` — dibuat ulang dari perilaku, UI reference, test evidence, atau dokumentasi.
- `NEW` — fitur/implementasi baru setelah operasi pemulihan.

## Recovery yang sudah dilakukan pada repo baru

Semua bagian berikut berstatus **RECONSTRUCTED**, bukan ORIGINAL.

### R1–R7 — VERIFIED

Sudah terverifikasi melalui Windows CI sampai checkpoint #82:

- .NET 8 + WPF shell;
- Core / Application / Infrastructure / Persistence / BrowserBridge;
- versioned download domain + atomic persistence;
- real aria2 HTTP/HTTPS engine baseline;
- deterministic queue core;
- live WPF binding;
- Add URL flow dengan validation, duplicate protection, no silent overwrite, queue persistence, dan start behavior.

### R8 — Browser handoff core — IMPLEMENTED, FINAL CI PENDING

- per-user single-instance guard;
- secondary instance meneruskan request lalu keluar;
- `--add-url <URL>` dan `--add-url=<URL>`;
- versioned local handoff protocol v1;
- per-user Named Pipe IPC;
- `PipeOptions.CurrentUserOnly`;
- acknowledgement hanya setelah request diproses desktop;
- URL browser tetap memakai R7 `AddDownloadService` sehingga validation + persistence tidak dibypass;
- activate command untuk membawa window utama ke depan;
- tests untuk startup parser, single instance, acknowledgement timing, persistence, dan protocol rejection.

### R9 — Chrome MV3 integration — IMPLEMENTED, FINAL CI PENDING

- Manifest V3 extension;
- deterministic unpacked extension ID `noobgcmaelhpcooeoflkjnkolkhcpmcl`;
- context menu link/media/page;
- toolbar action untuk active tab;
- optional Chrome download interception;
- interception OFF secara default;
- Chrome baru membatalkan download setelah acknowledgement DOWNLOAD-AJA sukses;
- graceful fallback: browser download tetap berjalan jika native host/desktop gagal atau menolak URL;
- options page + connection test;
- native messaging protocol v1;
- 4-byte little-endian native messaging framing + max message guard;
- `DownloadAja.NativeHost.exe`;
- native host meneruskan ke primary desktop instance;
- jika desktop belum aktif, native host dapat menjalankan sibling `Download Aja.exe` lalu retry handoff;
- HKCU registration helper tanpa Administrator untuk baseline;
- uninstall helper;
- CI validation untuk manifest, JavaScript, deterministic extension ID/origin, PowerShell syntax, dan .NET native-message tests.

## Status saat ini

- [x] R0 recovery audit.
- [x] R1 solution skeleton.
- [x] R2 download domain.
- [x] R3 persistence.
- [x] R4 real aria2 HTTP/HTTPS engine baseline.
- [x] R5 queue execution core.
- [x] R6 WPF shell live binding.
- [x] R7 Add URL flow.
- [~] R8 browser handoff core — implemented, CI pending.
- [~] R9 Chrome MV3 extension — implemented, CI pending.
- [ ] R10 scheduler/queue UX.
- [ ] Source ZIP lama ditemukan.
- [ ] Portable DOWNLOAD-AJA lama ditemukan di Library.
- [ ] Git history/bundle lama ditemukan.
- [ ] Source ORIGINAL dipulihkan.

## Next gate

R10 belum dikunci sebagai DONE sampai satu Windows CI terbaru memverifikasi gabungan R8 + R9. Setelah itu tahap berikutnya adalah **R10 — scheduler / queue UX**.

Detail: `docs/recovery/03_RECONSTRUCTION_BACKLOG.md`.
