# DOWNLOAD-AJA

Repository pemulihan untuk proyek **DOWNLOAD-AJA**, aplikasi download manager Windows bergaya IDM.

## Status

**Recovery audit tahap 1 selesai.**

Source lama belum berhasil dipulihkan, jadi repo ini memakai aturan provenance ketat:

- `ORIGINAL` — berasal dari source lama yang terbukti.
- `RECOVERED` — dipulihkan dari build/cache/artifact.
- `RECONSTRUCTED` — dibuat ulang dari bukti UI, perilaku, test, dan riwayat.
- `NEW` — pengembangan baru setelah recovery.

Jangan menyebut source hasil rekonstruksi sebagai source asli.

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

`R1 — Solution skeleton`: membangun pondasi .NET 8/WPF yang modular tanpa menambahkan fitur parity besar lebih dulu.
