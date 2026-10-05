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
- Riwayat inventaris menyebut target produk **IDM-like**.
- Fix commit historis: `31e335c`.
- Final portable build success historis: `da2aa9c`.
- Tests/artifacts pada tahap tersebut dilaporkan PASS.
- Referensi UI tersimpan:
  - `Download Aja Manager Interface.png`
  - `Download-Aja_UI_Reference.png`
- Master workflow ASTRA/SOL umum tersedia di Library.

### HISTORICAL CHAT — perlu diverifikasi ulang jika source ditemukan
Riwayat percakapan sebelumnya mencatat bahwa implementasi lama pernah memakai:
- C# / .NET 8 / WPF;
- aria2 portable sebagai engine download;
- Chrome MV3 extension + native bridge / local RPC;
- download HTTP/HTTPS segmented;
- pause / resume / stop / delete;
- progress, speed, ETA;
- queue/scheduler dan browser integration pada tahap pengembangan berikutnya;
- Windows build workflow dan portable artifact.

Riwayat chat juga menyebut beberapa pekerjaan lanjutan dan PR setelah baseline. Informasi ini dipakai sebagai petunjuk pemulihan, **bukan bukti source**.

## Klasifikasi file selama recovery

- `ORIGINAL` — source/file yang benar-benar berasal dari repo lama.
- `RECOVERED` — dipulihkan dari artifact/build/cache/bytecode atau sumber teknis lain.
- `RECONSTRUCTED` — dibuat ulang dari perilaku, UI reference, test evidence, atau dokumentasi.
- `NEW` — fitur/implementasi baru setelah operasi pemulihan.

## Recovery yang sudah dilakukan pada repo baru

Semua bagian berikut berstatus **RECONSTRUCTED**, bukan ORIGINAL:

### R1 — Solution skeleton
- solution `.NET 8`;
- WPF desktop shell;
- `DownloadAja.Core`;
- `DownloadAja.Infrastructure`;
- `DownloadAja.Persistence`;
- `DownloadAja.BrowserBridge`;
- Windows GitHub Actions CI;
- shell UI berdasarkan referensi Library tanpa dummy download rows.

### R2 — Download domain
- DownloadItem identity + URL/destination;
- state machine lengkap;
- progress/speed/ETA/error/timestamp;
- file category inference;
- versioned snapshot schema v1;
- restore path untuk persisted items;
- complete transition-matrix test.

### R3 — Persistence
- atomic download-history JSON store;
- queue-state store + persisted ordering;
- app-settings store;
- shared atomic JSON helper;
- recovery dari valid `.tmp` jika primary corrupt;
- serialization lock untuk writer ke file yang sama;
- max connections per download divalidasi `1..20` mengikuti historical fix evidence.

Windows CI checkpoint run #22:

- Restore: PASS
- Build: PASS
- Test: PASS

## Status saat ini

- [x] Repo baru diamankan dan diinisialisasi.
- [x] Library ChatGPT dicari untuk DOWNLOAD-AJA.
- [x] Dua referensi UI ditemukan dan diperiksa.
- [x] Commit/build historical anchors dicatat.
- [x] Repo migrasi lama dicoba diakses; saat ini 404.
- [x] Audit recovery tahap 1 dibuat.
- [x] Rekonstruksi source dimulai dengan provenance `RECONSTRUCTED`.
- [x] R1 solution skeleton selesai + CI PASS.
- [x] R2 download domain selesai + tests PASS.
- [x] R3 persistence foundation selesai + tests PASS.
- [ ] Source ZIP lama ditemukan.
- [ ] Portable DOWNLOAD-AJA lama ditemukan di Library.
- [ ] Git history/bundle lama ditemukan.
- [ ] Source ORIGINAL dipulihkan.
- [ ] R4 aria2 engine adapter nyata dipulihkan/direkonstruksi.
- [ ] R5 queue execution core selesai.
- [ ] Browser integration dipulihkan/direkonstruksi.

## Next ready step

**R4 — aria2 engine adapter.**

Implementasi berikutnya harus membuat protocol/process boundary dan unit tests lebih dulu. Real download baru boleh dinyatakan PASS setelah binary aria2 benar-benar tersedia untuk test/integration execution.

Detail: `docs/recovery/03_RECONSTRUCTION_BACKLOG.md`.
