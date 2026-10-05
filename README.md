# DOWNLOAD-AJA

Repository pemulihan untuk proyek **DOWNLOAD-AJA**, aplikasi download manager Windows bergaya IDM.

## Status

**Recovery aktif — R1 sampai R7 terverifikasi; R8–R10 serta R11A/R11B sudah diimplementasikan dan menunggu satu final Windows CI pada HEAD terbaru.**

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
- persisted daily queue scheduler, including overnight windows
- live search/filter by filename, URL, folder, state, category, and error
- `Pilihan` dialog for default folder, connections per download, simultaneous downloads, and global speed limit
- global aria2 speed limiter with live RPC update when aria2 is running
- persisted speed limit with backward-compatible schema-v1 settings loading
- browser handoff reads the current persisted default download folder instead of a startup-only copy

Latest fully verified checkpoint remains **Windows CI run #82 — PASS**. Newer waves have passed build/unit/browser-validation checkpoints during intermediate CI runs, but a final HEAD run is still required before R8–R11B are marked VERIFIED.

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

Selesaikan satu Windows CI penuh pada HEAD terbaru. Setelah R8–R11B terverifikasi, lanjutkan wave parity berikutnya seperti refresh URL kedaluwarsa / retry-integrity sebelum masuk portable acceptance R12.
