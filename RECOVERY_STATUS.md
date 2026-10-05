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

Tahap R1 sudah dibuat sebagai **RECONSTRUCTED**, bukan ORIGINAL:

- solution `.NET 8`;
- WPF desktop shell;
- `DownloadAja.Core`;
- `DownloadAja.Infrastructure`;
- `DownloadAja.Persistence`;
- `DownloadAja.BrowserBridge`;
- xUnit core tests;
- Windows GitHub Actions CI;
- shell UI berdasarkan referensi Library tanpa dummy download rows.

CI Windows run #2 berhasil:

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
- [x] R1 solution skeleton dibuat.
- [x] R1 Windows build + unit test PASS.
- [ ] Source ZIP lama ditemukan.
- [ ] Portable DOWNLOAD-AJA lama ditemukan di Library.
- [ ] Git history/bundle lama ditemukan.
- [ ] Source ORIGINAL dipulihkan.
- [ ] aria2 engine adapter nyata dipulihkan/direkonstruksi.
- [ ] Persistence atomic + queue state selesai.
- [ ] Browser integration dipulihkan/direkonstruksi.

## Next ready step

**R2/R3: Download domain + persistence foundation**, lalu **R4: aria2 engine adapter**.

Detail audit: `docs/recovery/00_RECOVERY_AUDIT.md`.
