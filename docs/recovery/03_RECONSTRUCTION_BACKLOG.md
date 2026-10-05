# 03 — Reconstruction Backlog

Status: **R1–R3 COMPLETE / R4 READY**

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

Selesai dibuat sebagai `RECONSTRUCTED`:

- [x] .NET 8 solution.
- [x] WPF desktop project.
- [x] domain/core project.
- [x] infrastructure/engine adapter project.
- [x] persistence project.
- [x] browser bridge contract project.
- [x] xUnit test projects.
- [x] shell UI mengikuti struktur referensi Library.
- [x] Windows CI workflow.
- [x] Restore PASS.
- [x] Build PASS.
- [x] Unit test PASS.

## R2 — Download domain

Status: DONE

Selesai sebagai `RECONSTRUCTED`:

- [x] DownloadItem identity.
- [x] URL + destination.
- [x] state machine Waiting / Downloading / Paused / Completed / Stopped / Failed.
- [x] progress bytes/percent.
- [x] speed/ETA representation.
- [x] timestamps.
- [x] error model.
- [x] category/type inference untuk Video, Audio, Document, Archive, Program, Other.
- [x] immutable restore path untuk persistence.
- [x] versioned `DownloadItemSnapshot` schema v1.
- [x] complete transition-matrix regression test.
- [x] invalid transitions rejected.
- [x] snapshot round-trip tests.

## R3 — Persistence

Status: DONE

Selesai sebagai `RECONSTRUCTED`:

- [x] atomic download history store.
- [x] versioned queue-state store.
- [x] persisted queue ordering.
- [x] persisted max simultaneous downloads + running flag.
- [x] versioned application-settings store.
- [x] default download-directory setting.
- [x] max connections per download setting.
- [x] historical aria2 max-connections clamp `1..20`.
- [x] same-directory temp write + atomic replacement.
- [x] corrupt-primary recovery from valid temp state.
- [x] in-process serialization for concurrent writers to the same file.
- [x] tests for history, queue order, settings, corruption recovery, and concurrent writers.

Windows CI run #22 for the R2/R3 checkpoint:

- Restore: PASS
- Build: PASS
- Test: PASS

Regression targets covered:

- historical save race: guarded by serialized writers + atomic replacement;
- persisted queue order: explicit versioned queue-state contract;
- historical aria2 max 20: settings validation prevents values >20.

## R4 — aria2 engine adapter

Status: READY

Target:

- process lifecycle;
- locate/bundle aria2 executable without assuming it is globally installed;
- private RPC endpoint/token;
- RPC health check;
- add URI;
- pause/resume/remove;
- status polling;
- map aria2 GID to DownloadItem id;
- max connection clamp 1..20;
- graceful shutdown;
- stale/orphan process handling;
- clear engine diagnostics.

Acceptance:

- unit tests for RPC payload/result mapping;
- connection clamp test;
- process lifecycle test where feasible;
- real HTTP/HTTPS download integration test once an aria2 binary is supplied/bundled;
- partial-download resume test once binary is available;
- no WPF dependency in engine layer.

Important: do not claim real aria2 download PASS until the binary is actually available to CI/test execution.

## R5 — Queue core

Target:

- waiting/running/paused/completed/failed;
- max simultaneous downloads;
- deterministic dequeue;
- stop-all semantics;
- retained completed items;
- reorder + persisted order.

Regression target:

- queue-limit counting;
- stop-all continuation;
- persisted queue order unused.

## R6 — WPF shell parity

Status: PARTIAL FOUNDATION EXISTS

Sudah ada struktur visual:

- [x] menu bar;
- [x] toolbar;
- [x] category sidebar;
- [x] seven-column download table;
- [x] detail/progress/log tabs;
- [x] status bar.

Belum:

- [ ] selection/detail binding;
- [ ] real state binding;
- [ ] toolbar commands;
- [ ] category filters;
- [ ] progress presentation nyata.

## R7 — Add URL flow

Target:

- Tambah URL dialog;
- destination folder;
- filename resolution;
- duplicate handling;
- start now / queue behavior;
- validation.

## R8 — Browser handoff core

Target:

- single-instance desktop handoff;
- `--add-url` compatible entry path;
- local protocol/RPC contract;
- success only after URL accepted;
- startup URL and running-instance URL tests.

Regression target:

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

Hanya dikerjakan setelah core stabil. Kandidat historis:

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

Setiap item harus mempunyai provenance: HISTORICAL atau NEW.

## R12 — Build & portable acceptance

Target:

- Windows CI build;
- test suite green;
- portable artifact;
- dependency inventory;
- SHA-256;
- clean-machine/fresh-folder startup test;
- no admin required untuk portable baseline kecuali fitur integrasi tertentu memang membutuhkan tindakan eksplisit.

## Release rule

Jangan memberi label "recovered stable" sebelum R0-R10 selesai dan diuji. R11 dapat berjalan bertahap setelah baseline recovery stabil.
