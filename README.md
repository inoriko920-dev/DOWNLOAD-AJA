# DOWNLOAD-AJA

Repository pemulihan untuk proyek **DOWNLOAD-AJA**, aplikasi download manager Windows bergaya IDM.

## Status

**Recovery aktif — R1 sampai R7 terverifikasi; R8 dan R9 sudah diimplementasikan dan menunggu final Windows CI.**

Source lama belum berhasil dipulihkan, jadi repo ini memakai provenance ketat:

- `ORIGINAL` — berasal dari source lama yang terbukti.
- `RECOVERED` — dipulihkan dari build/cache/artifact.
- `RECONSTRUCTED` — dibuat ulang dari bukti UI, perilaku, test, dan riwayat.
- `NEW` — pengembangan baru setelah recovery.

Jangan menyebut source hasil rekonstruksi sebagai source asli.

## Reconstructed baseline

- .NET 8 + WPF shell
- download domain + versioned persistence
- deterministic persisted queue core
- aria2 JSON-RPC engine adapter
- real HTTPS download integration through reconstructed `Aria2DownloadEngine`
- live WPF binding ke queue/domain/aria2 state
- Add URL dialog + HTTP/HTTPS validation + duplicate/no-overwrite protection
- per-user single-instance desktop handoff
- `--add-url` startup handoff
- current-user-only Named Pipe IPC
- acknowledgement only after desktop acceptance/persistence
- Chrome Manifest V3 integration
- deterministic unpacked extension ID
- context menu + toolbar send-to-DOWNLOAD-AJA
- optional Chrome download interception, OFF by default
- graceful fallback: Chrome keeps its own download if DOWNLOAD-AJA/native host fails
- `DownloadAja.NativeHost.exe`
- HKCU native-host registration helper without Administrator for baseline
- extension options page + native-host connection test

Latest fully verified checkpoint remains **Windows CI run #82 — PASS**. R8/R9 are implemented and waiting for the latest Windows runner to verify the combined code.

## Browser integration

See [`browser-extension/README.md`](browser-extension/README.md).

Local install helper:

```powershell
powershell -ExecutionPolicy Bypass -File tools/install-chrome-integration.ps1 -InstallRoot "C:\path\ke\DOWNLOAD-AJA"
```

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

## Next gate

Setelah final CI memverifikasi R8 + R9, tahap berikutnya adalah `R10 — Scheduler / Queue UX`.
