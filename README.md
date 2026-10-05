# DOWNLOAD-AJA

Repository pemulihan untuk proyek **DOWNLOAD-AJA**, aplikasi download manager Windows bergaya IDM.

## Status

**Recovery aktif — R1 sampai R5 sudah direkonstruksi dan terverifikasi.**

Source lama belum berhasil dipulihkan, jadi repo ini memakai provenance ketat:

- `ORIGINAL` — berasal dari source lama yang terbukti.
- `RECOVERED` — dipulihkan dari build/cache/artifact.
- `RECONSTRUCTED` — dibuat ulang dari bukti UI, perilaku, test, dan riwayat.
- `NEW` — pengembangan baru setelah recovery.

Jangan menyebut source hasil rekonstruksi sebagai source asli.

## Verified reconstructed baseline

- .NET 8 + WPF shell
- download domain + versioned persistence
- deterministic persisted queue core
- aria2 JSON-RPC engine adapter
- official aria2 1.37.0 Windows x64 fetched by CI
- **real HTTPS download integration PASS** through reconstructed `Aria2DownloadEngine`
- queue regression tests for limit counting, stop-all, persisted ordering, pause/resume, and restart behavior

Latest verified checkpoint: **Windows CI run #52 — PASS**, including real aria2 HTTPS integration.

## Dokumen recovery

- [`RECOVERY_STATUS.md`](RECOVERY_STATUS.md)
- [`00_RECOVERY_AUDIT.md`](docs/recovery/00_RECOVERY_AUDIT.md)
- [`01_UI_REFERENCE_CONTRACT.md`](docs/recovery/01_UI_REFERENCE_CONTRACT.md)
- [`02_HISTORICAL_ARCHITECTURE.md`](docs/recovery/02_HISTORICAL_ARCHITECTURE.md)
- [`03_RECONSTRUCTION_BACKLOG.md`](docs/recovery/03_RECONSTRUCTION_BACKLOG.md)

## Historical anchors

- old migration repo: `tonitaru6-cloud/Download-Aja-` — saat audit 2026-10-06 tidak dapat diakses (404)
- historical fix: `31e335c`
- historical successful portable baseline: `da2aa9c`

## Next ready step

`R6 — WPF shell parity + live binding`: hubungkan UI referensi ke queue/domain/aria2 nyata, selection detail, toolbar commands, category filter, progress, speed/ETA, dan status bar.
