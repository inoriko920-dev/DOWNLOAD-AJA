# 03 — Reconstruction Backlog

Status: **R1–R7 COMPLETE / R8 READY**

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

Status: READY

Target:

- single-instance desktop handoff;
- `--add-url` compatible entry path;
- local protocol/RPC contract;
- reuse R7 `AddDownloadService` validation and persistence path;
- success only after URL is accepted/persisted by desktop app;
- startup URL and running-instance URL tests;
- no browser-specific logic inside WPF ViewModel.

Regression targets:

- startup/browser URL handling;
- async single-instance dispatch;
- premature browser success.

## R9 — Chrome MV3 extension

Target:

- context menu download;
- optional interception;
- native/local bridge;
- install/registration helper without admin where feasible;
- protocol version check;
- graceful fallback when desktop app is not running.

## R10 — Scheduler / queue UX

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
- dependency inventory;
- SHA-256;
- fresh-folder startup test;
- no admin required for portable baseline unless an integration feature explicitly requires it.

## Release rule

Do not label the project `recovered stable` before R0–R10 are implemented and verified. Reconstructed code remains `RECONSTRUCTED` unless old source evidence is actually recovered.
