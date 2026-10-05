# DOWNLOAD-AJA — Recovery Status

Tanggal audit awal: 2026-10-06

## Status keseluruhan

**RECOVERY IN PROGRESS — SOURCE LAMA BELUM TERPULIHKAN**

Repo baru ini sengaja dimulai dalam mode pemulihan. Implementasi hasil rekonstruksi tetap diberi provenance `RECONSTRUCTED`, bukan `ORIGINAL`.

## Historical anchors

- repo migrasi lama: `tonitaru6-cloud/Download-Aja-` — tidak dapat diakses saat audit;
- historical fix: `31e335c`;
- historical successful portable baseline: `da2aa9c`;
- target historis: IDM-like Windows download manager.

## Verified checkpoint lama

R1–R7 sudah pernah terverifikasi melalui Windows CI, termasuk real aria2 HTTPS integration. Checkpoint terakhir sebelum browser/scheduler wave: **Windows CI #82 PASS**.

## R8 — Browser handoff core

Status: **IMPLEMENTED — COMBINED CI PENDING**

- per-user single-instance;
- `--add-url` startup handoff;
- versioned local protocol;
- Named Pipe `CurrentUserOnly`;
- secondary instance → primary instance;
- acknowledgement hanya setelah desktop benar-benar menerima/persist URL;
- activation request untuk window yang sudah berjalan;
- regression tests untuk parser, single-instance, persistence, dan acknowledgement timing.

## R9 — Chrome MV3 integration

Status: **IMPLEMENTED — COMBINED CI PENDING**

- Chrome Manifest V3 extension;
- deterministic unpacked extension ID;
- context menu link/media/page;
- toolbar action;
- optional download interception, OFF secara default;
- browser download hanya dibatalkan setelah acknowledgement sukses;
- graceful fallback ketika native host/desktop tidak tersedia;
- options page + connection test;
- native messaging framing;
- `DownloadAja.NativeHost.exe`;
- native host dapat menjalankan sibling `Download Aja.exe` lalu retry;
- HKCU native-host registration helper tanpa Administrator untuk baseline;
- CI validation assets untuk manifest, JavaScript, PowerShell, extension ID/origin, dan native-message tests.

## R10 — Scheduler / queue UX

Status: **IMPLEMENTED — COMBINED CI PENDING**

- persisted `scheduler.json` dengan schema version 1;
- scheduler OFF secara default;
- konfigurasi jadwal harian start/stop berbasis jam lokal;
- dukungan jadwal normal, misalnya `08:00–22:00`;
- dukungan window melewati tengah malam, misalnya `22:00–06:00`;
- scheduler menerapkan state lagi setelah aplikasi restart;
- saat masuk active window, antrean otomatis dijalankan;
- saat keluar active window, penjadwalan item baru dihentikan tanpa memutus paksa download yang sedang aktif;
- dialog WPF `Jadwal Antrean` dengan validasi `HH:mm`;
- menu dan toolbar `Jadwal` sekarang aktif;
- status jadwal ditampilkan di UI;
- application tests untuk same-day/overnight windows, start/stop behavior, disabled behavior, dan restart recovery;
- persistence tests untuk scheduler JSON default + round-trip.

## Status milestone

- [x] R0 recovery audit
- [x] R1 solution skeleton
- [x] R2 download domain
- [x] R3 persistence
- [x] R4 aria2 engine baseline
- [x] R5 queue execution core
- [x] R6 WPF live binding
- [x] R7 Add URL flow
- [~] R8 browser handoff — implemented, combined CI pending
- [~] R9 Chrome MV3 — implemented, combined CI pending
- [~] R10 scheduler/queue UX — implemented, combined CI pending
- [ ] R11 IDM-parity recovery wave
- [ ] R12 portable/release acceptance
- [ ] source ORIGINAL lama ditemukan

## Current verification gate

Latest combined Windows CI for the current tree is **run #136**. Saat dokumen ini diperbarui, job masih menunggu runner. Jangan menandai R8–R10 sebagai VERIFIED sampai run terbaru benar-benar PASS.

## Next step after green CI

Masuk **R11 — IDM-parity recovery wave** dengan prioritas fitur recovery historis: search/filter, speed limiter, connection settings, refresh expired URL, retry/integrity, batch/all-links, import/export, ZIP preview, offline-site storage, dan improved media/browser detection.
