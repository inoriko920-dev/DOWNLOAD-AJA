# DOWNLOAD-AJA — Recovery Status

Tanggal audit awal: 2026-10-06

## Status keseluruhan

**RECOVERY IN PROGRESS — SOURCE LAMA BELUM TERPULIHKAN**

Repo baru ini sengaja dimulai dalam mode pemulihan. Jangan menambahkan fitur baru atau menganggap implementasi baru sebagai source lama sampai sumber historis berhasil dibedakan.

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

Semua hasil pemulihan berikutnya wajib diberi salah satu label:

- `ORIGINAL` — source/file yang benar-benar berasal dari repo lama.
- `RECOVERED` — dipulihkan dari artifact/build/cache/bytecode atau sumber teknis lain.
- `RECONSTRUCTED` — dibuat ulang dari perilaku, UI reference, test evidence, atau dokumentasi.
- `NEW` — fitur/implementasi baru setelah operasi pemulihan.

## Gate sebelum implementasi fitur

Recovery tahap 1 dianggap selesai jika minimal:

1. Bukti Library dan riwayat chat sudah dipetakan.
2. Kontrak UI lama sudah ditulis.
3. Baseline arsitektur lama sudah dibedakan antara VERIFIED vs HISTORICAL.
4. Daftar komponen yang hilang dan perlu direkonstruksi sudah ada.
5. Tidak ada source lama yang keliru diberi label ORIGINAL.

## Status saat ini

- [x] Repo baru diamankan dan diinisialisasi.
- [x] Library ChatGPT dicari untuk DOWNLOAD-AJA.
- [x] Dua referensi UI ditemukan dan diperiksa.
- [x] Commit/build historical anchors dicatat.
- [x] Repo migrasi lama dicoba diakses; saat ini 404.
- [x] Audit recovery tahap 1 dibuat.
- [ ] Source ZIP lama ditemukan.
- [ ] Portable DOWNLOAD-AJA lama ditemukan di Library.
- [ ] Git history/bundle lama ditemukan.
- [ ] Source ORIGINAL dipulihkan.
- [ ] Rekonstruksi source dimulai.

Detail: `docs/recovery/00_RECOVERY_AUDIT.md`.
