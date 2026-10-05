# 03 — Reconstruction Backlog

Status: **READY AFTER RECOVERY AUDIT**

Backlog ini bukan daftar fitur baru. Ini adalah urutan aman untuk mengembalikan kemampuan DOWNLOAD-AJA berdasarkan bukti yang tersedia.

## R0 — Recovery foundation

Status: DONE

- [x] Inisialisasi repo recovery.
- [x] Catat provenance ORIGINAL / RECOVERED / RECONSTRUCTED / NEW.
- [x] Catat repo lama dan historical commit anchors.
- [x] Lock UI reference contract.
- [x] Buat historical architecture map.

Gate: dokumentasi recovery tersedia sebelum source baru ditulis.

## R1 — Solution skeleton

Status: READY

Target:

- .NET 8 solution;
- WPF desktop project;
- domain/core project;
- infrastructure/engine adapter project;
- persistence project;
- browser bridge contract project;
- test projects;
- clear provenance notes.

Belum boleh memasukkan fitur parity besar.

Acceptance:

- build bersih di Windows CI;
- app membuka shell kosong yang mengikuti layout reference;
- tests dapat dijalankan;
- tidak ada mock download yang diklaim sebagai implementasi final.

## R2 — Download domain

Target:

- DownloadItem identity;
- URL + destination;
- download state machine;
- progress bytes/percent;
- speed/ETA representation;
- timestamps;
- error model;
- category/type inference minimal.

Acceptance:

- unit tests untuk setiap state transition;
- invalid transition ditolak;
- serialization contract versioned.

## R3 — Persistence

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

Target langsung dari UI reference:

- menu bar;
- toolbar;
- category sidebar;
- seven-column download table;
- detail/progress/log tabs;
- status bar;
- selection/detail binding;
- real state binding.

Gate:

UI tidak boleh menggunakan dummy data untuk acceptance akhir R6.

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
