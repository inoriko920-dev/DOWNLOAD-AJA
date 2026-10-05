# 03 — Reconstruction Backlog

Status: **R1–R7 VERIFIED / R8–R9 IMPLEMENTED — FINAL CI PENDING / R10 NEXT**

Backlog ini bukan daftar fitur baru. Ini adalah urutan aman untuk mengembalikan kemampuan DOWNLOAD-AJA berdasarkan bukti yang tersedia.

## R0 — Recovery foundation

Status: DONE

- [x] Inisialisasi repo recovery.
- [x] Catat provenance ORIGINAL / RECOVERED / RECONSTRUCTED / NEW.
- [x] Catat repo lama dan historical commit anchors.
- [x] Lock UI reference contract.
- [x] Buat historical architecture map.

## R1 — Solution skeleton

Status: DONE

- [x] .NET 8 solution.
- [x] WPF desktop project.
- [x] Core / Application / Infrastructure / Persistence / BrowserBridge projects.
- [x] xUnit projects.
- [x] Windows CI.
- [x] shell UI mengikuti struktur referensi Library.

## R2 — Download domain

Status: DONE

- [x] DownloadItem identity, URL, destination.
- [x] Waiting / Downloading / Paused / Completed / Stopped / Failed.
- [x] progress, speed, ETA, timestamp, error.
- [x] category inference.
- [x] versioned snapshot schema v1.
- [x] restore path.
- [x] complete transition matrix tests.

## R3 — Persistence

Status: DONE

- [x] atomic download history store.
- [x] persisted queue state/order.
- [x] app-settings store.
- [x] temp-file recovery.
- [x] concurrent writer serialization.
- [x] queue/settings/history tests.
- [x] historical requested split/max setting `1..20` retained.

## R4 — aria2 engine adapter

Status: DONE — BASELINE HTTP/HTTPS VERIFIED

- [x] private local JSON-RPC client.
- [x] random RPC secret.
- [x] process lifecycle and health gate.
- [x] add URI.
- [x] pause/resume/remove.
- [x] status polling.
- [x] GID mapping.
- [x] progress/speed/ETA/error mapping.
- [x] graceful shutdown.
- [x] terminal GID cleanup.
- [x] actionable missing-binary diagnostics.
- [x] split count up to 20; per-server connection clamp 16.
- [x] RPC/unit tests.
- [x] real official aria2 1.37.0 Windows x64 fetched in CI.
- [x] real HTTPS download through reconstructed engine.

Verification: **Windows CI run #44 PASS** including real aria2 HTTPS integration.

Catatan: partial-download interruption/resume stress coverage dapat ditambah pada hardening wave; baseline `continue=true` sudah dikirim ke aria2.

## R5 — Queue core

Status: DONE

- [x] `DownloadAja.Application` orchestration layer.
- [x] deterministic dequeue from persisted order.
- [x] max simultaneous downloads.
- [x] active-count enforcement.
- [x] completion starts exactly next waiting item.
- [x] pause frees slot.
- [x] resume rejected if capacity is full.
- [x] individual stop does not silently auto-restart.
- [x] explicit restart/requeue.
- [x] stop-all disables scheduling before stopping active items.
- [x] completed items retained.
- [x] reorder persisted.
- [x] persisted order reused by a new coordinator instance.
- [x] transient Downloading/Paused state normalized safely after app restart while preserving progress bytes.
- [x] queue regression tests.

Historical regressions addressed:

- queue-limit counting;
- stop-all continuation;
- persisted queue order unused.

Verification: **Windows CI run #52 PASS**, including all solution tests and real aria2 HTTPS integration.

## R6 — WPF shell parity + live binding

Status: DONE

Implemented as `RECONSTRUCTED`:

- [x] explicit WPF application composition/bootstrap.
- [x] WPF ViewModel layer.
- [x] live queue collection binding.
- [x] selection → Detail Unduhan binding.
- [x] toolbar commands: Mulai, Jeda, Hentikan, Mulai Ulang.
- [x] Stop All + manual Refresh commands.
- [x] category filters: Semua, Selesai, Belum Selesai, Antrean, Video, Audio, Dokumen, Arsip, Program.
- [x] real progress/speed/ETA presentation from domain/aria2 state.
- [x] active-count + total-speed status bar.
- [x] periodic engine refresh without blocking the UI thread.
- [x] graceful WPF shutdown → owned aria2 shutdown.
- [x] future/unrecovered toolbar features remain visibly disabled instead of using fake behavior.

Verification: **Windows CI run #65 PASS**:

- Restore: PASS
- Build: PASS
- all solution tests: PASS
- official aria2 1.37.0 fetch: PASS
- real aria2 HTTPS integration: PASS

UI acceptance uses real queue/domain state; no static dummy download rows are used.

## R7 — Add URL flow

Status: DONE — HTTP/HTTPS BASELINE

Implemented as `RECONSTRUCTED`:

- [x] active `Tambah URL` toolbar button.
- [x] `Unduh > Tambah URL...` menu entry.
- [x] WPF dialog for URL, filename, destination folder, and start-queue option.
- [x] Windows folder picker.
- [x] HTTP/HTTPS URL validation.
- [x] deterministic filename inference from URL path.
- [x] optional user filename validation/sanitization.
- [x] default destination loaded from persisted app settings.
- [x] aria2 split count loaded from persisted app settings.
- [x] destination directory normalization/creation.
- [x] reject duplicate source URL.
- [x] reject duplicate destination path.
- [x] reject overwrite when target file already exists on disk.
- [x] persist added item into real queue/history store.
- [x] optional start queue immediately after add.
- [x] select newly added item in live UI.
- [x] application tests for valid add, filename inference, start behavior, invalid schemes, duplicates, and existing target files.

Verification: **Windows CI run #82 PASS**:

- Restore: PASS
- Build: PASS
- all solution tests including Add URL tests: PASS
- official aria2 1.37.0 Windows x64 fetch: PASS
- real aria2 HTTPS integration: PASS

## R8 — Browser handoff core

Status: IMPLEMENTED — FINAL CI PENDING

Implemented as `RECONSTRUCTED`:

- [x] per-user single-instance desktop guard.
- [x] secondary process forwards to primary and exits.
- [x] `--add-url <URL>` and `--add-url=<URL>` startup forms.
- [x] versioned local handoff protocol v1.
- [x] per-user named-pipe IPC.
- [x] pipe restricted with `CurrentUserOnly`.
- [x] reuse R7 `AddDownloadService` validation/persistence path.
- [x] success response only after desktop validation + queue persistence.
- [x] activation command brings existing WPF window forward.
- [x] startup URL and running-instance transport tests.
- [x] regression test proving client cannot receive success before handler completes.
- [x] single-instance regression test.

Historical regressions addressed in design:

- startup/browser URL handling;
- async single-instance dispatch;
- premature browser success acknowledgement.

## R9 — Chrome MV3 extension

Status: IMPLEMENTED — FINAL CI PENDING

Implemented as `RECONSTRUCTED`:

- [x] Chrome Manifest V3 extension skeleton.
- [x] deterministic unpacked extension ID.
- [x] context menu for link/media/page URL.
- [x] toolbar action for active tab URL.
- [x] optional browser-download interception.
- [x] interception OFF by default.
- [x] browser download is only cancelled after native acknowledgement success.
- [x] graceful fallback leaves Chrome download untouched when desktop/native host rejects or is unavailable.
- [x] options page for interception + start-queue behavior.
- [x] native-host connection test from extension options.
- [x] native messaging protocol v1 with message-size guard.
- [x] `DownloadAja.NativeHost.exe` console bridge.
- [x] native host forwards to existing desktop instance.
- [x] native host can launch sibling `Download Aja.exe` when desktop is not running, then retry handoff.
- [x] per-user Chrome native-host registration via HKCU; no Administrator required for baseline.
- [x] install + uninstall PowerShell helpers.
- [x] allowed Chrome origin locked to deterministic extension ID.
- [x] CI validation for MV3 JSON, JavaScript syntax, extension ID/origin agreement, and PowerShell syntax.
- [x] .NET tests for native-message framing and oversized payload rejection.

Current recovery distribution model: unpacked Chrome extension for local/portable use. Chrome Web Store packaging is not part of R9 baseline.

## R10 — Scheduler / queue UX

Status: NEXT AFTER R8/R9 CI VERIFICATION

Target:

- scheduler UI;
- queue start/stop;
- time-based start;
- queue state recovery after restart.

## R11 — IDM-parity recovery wave

Only after core recovery is stable. Historical candidates:

- search/filter;
- speed limiter;
- connection settings;
- refresh expired URL;
- retry/integrity;
- batch/all-links;
- import/export;
- ZIP preview;
- offline-site storage;
- improved media/browser detection.

Each item must keep provenance: HISTORICAL or NEW.

## R12 — Build & portable acceptance

Target:

- Windows CI green;
- full test suite green;
- portable artifact;
- bundled aria2 runtime;
- bundled native host + browser integration assets;
- dependency inventory;
- SHA-256;
- fresh-folder startup test;
- no admin required for portable baseline unless an integration feature explicitly requires it.

## Release rule

Do not label the project `recovered stable` before R0–R10 are implemented and verified. Reconstructed code remains `RECONSTRUCTED` unless old source evidence is actually recovered.
