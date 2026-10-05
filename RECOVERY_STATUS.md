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

### R1 — Solution skeleton — DONE
- solution `.NET 8`;
- WPF desktop shell;
- Core / Infrastructure / Persistence / BrowserBridge;
- Windows GitHub Actions CI;
- shell UI berdasarkan referensi Library.

### R2 — Download domain — DONE
- DownloadItem identity + URL/destination;
- state machine lengkap;
- progress/speed/ETA/error/timestamp;
- file category inference;
- versioned snapshot schema v1;
- restore path untuk persisted items;
- transition-matrix tests.

### R3 — Persistence — DONE
- atomic download-history JSON store;
- persisted queue ordering/state;
- app-settings store;
- corrupt-primary recovery dari valid `.tmp`;
- serialized writers untuk mencegah save race;
- historical requested split/max setting divalidasi `1..20`.

### R4 — aria2 engine adapter — DONE untuk baseline HTTP/HTTPS
- aria2 JSON-RPC 2.0 client;
- private localhost RPC + secret token;
- process lifecycle/health gate;
- addUri / pause / unpause / remove / tellStatus / shutdown;
- DownloadItem ↔ aria2 GID mapping;
- status/progress/speed/ETA/error mapping;
- app split count sampai 20, dengan aria2 per-server connection clamp 16;
- terminal GID cleanup;
- missing-binary diagnostics;
- unit tests untuk RPC payload, errors, status mapping, lifecycle, dan engine behavior.

**Windows CI run #44: PASS**, termasuk:
- Restore: PASS
- Build: PASS
- unit/persistence/infrastructure tests: PASS
- download official aria2 1.37.0 Windows x64: PASS
- menjalankan `aria2c --version`: PASS
- real HTTPS integration melalui reconstructed engine: PASS

Real integration benar-benar memulai aria2 dan mengunduh file HTTPS dari repo resmi aria2 melalui `Aria2DownloadEngine`.

### R5 — Queue execution core — DONE
- project `DownloadAja.Application`;
- deterministic persisted queue order;
- max simultaneous downloads;
- queue start/stop scheduling;
- pause frees a slot;
- resume respects max active limit;
- individual stop tidak auto-restart;
- explicit restart/requeue;
- stop-all turns scheduling off before stopping active downloads;
- completion fills exactly the next waiting slot;
- completed items retained;
- reorder persisted and reused after coordinator restart;
- transient Downloading/Paused state safely normalized to Waiting after app restart while preserving byte progress.

**Windows CI run #52: PASS**, termasuk:
- Restore: PASS
- Build: PASS
- seluruh solution tests termasuk queue tests: PASS
- official aria2 fetch: PASS
- real aria2 HTTPS integration: PASS

## Status saat ini

- [x] R0 recovery audit.
- [x] R1 solution skeleton.
- [x] R2 download domain.
- [x] R3 persistence.
- [x] R4 real aria2 HTTP/HTTPS engine baseline.
- [x] R5 queue execution core.
- [ ] R6 WPF shell dihubungkan ke state/commands nyata.
- [ ] R7 Add URL flow.
- [ ] R8 browser handoff core.
- [ ] R9 Chrome MV3 extension.
- [ ] R10 scheduler/queue UX.
- [ ] Source ZIP lama ditemukan.
- [ ] Portable DOWNLOAD-AJA lama ditemukan di Library.
- [ ] Git history/bundle lama ditemukan.
- [ ] Source ORIGINAL dipulihkan.

## Next ready step

**R6 — hidupkan WPF shell dengan data queue nyata, selection/detail binding, toolbar commands, kategori, progress, dan status bar.**

Setelah itu R7 membuat dialog `Tambah URL` dan menghubungkannya ke queue + aria2 nyata.

Detail: `docs/recovery/03_RECONSTRUCTION_BACKLOG.md`.
