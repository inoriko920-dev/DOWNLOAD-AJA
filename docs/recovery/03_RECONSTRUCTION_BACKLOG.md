# 03 — Reconstruction Backlog

Status: **R1 COMPLETE / R2 READY**

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
- [x] xUnit test project.
- [x] shell UI mengikuti struktur referensi Library.
- [x] Windows CI workflow.
- [x] Restore PASS.
- [x] Build PASS.
- [x] Unit test PASS.

Catatan: run CI pertama gagal hanya karena `using Xunit;` belum ditambahkan pada test source. Defect tersebut diperbaiki dan run #2 PASS seluruh tahap.

## R2 — Download domain

Status: READY / PARTIAL FOUNDATION EXISTS

Sudah ada:

- [x] DownloadItem identity.
- [x] URL + destination.
- [x] download state machine dasar.
- [x] progress bytes/percent.
- [x] speed/ETA representation.
- [x] timestamps.
- [x] error model dasar.

Belum:

- [ ] category/type inference minimal.
- [ ] serialization contract versioned.
- [ ] seluruh transition matrix/regression cases.
- [ ] immutable restore constructor/DTO untuk persistence.

Acceptance:

- unit tests untuk setiap state transition;
- invalid transition ditolak;
- serialization contract versioned.

## R3 — Persistence

Status: READY

Target:

- history store;
- queue order store;
- settings store;
- atomic save;
- recovery dari corrupt/incomplete temp write.

Regression target dari riwayat lama:

- save race;
- queue order setelah restart.

## R4 — aria2 engine adapter

Target:

- process lifecycle;
- RPC health;
- add URI;
- pause/resume/remove;
- status polling;
- max connection clamp;
- graceful shutdown;
- orphan-process recovery.

Acceptance:

- real HTTP/HTTPS download integration test;
- partial download resume test;
- connection limit test;
- no UI dependency in engine layer.

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
