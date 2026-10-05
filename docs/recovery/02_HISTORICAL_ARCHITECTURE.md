# 02 — Historical Architecture Map

Status: **HISTORICAL — NEEDS RE-VERIFICATION**

Dokumen ini merangkum arsitektur yang tercatat dalam riwayat percakapan lama. Ia berguna untuk rekonstruksi, tetapi tidak boleh dianggap sebagai source evidence sampai file lama ditemukan.

## Baseline teknis yang pernah dicatat

### Desktop app
- Bahasa/platform: C# / .NET 8.
- UI: WPF.
- Target: Windows desktop.
- Model distribusi pada baseline: portable artifact; installer ditunda sampai stabil.

### Download engine
- Engine eksternal: aria2 1.37.0 portable.
- Download HTTP/HTTPS segmented.
- Riwayat baseline menyebut sekitar 8 connections per download.
- Operasi inti: add, pause, resume, stop, delete.
- Telemetry pengguna: progress, speed, ETA.

### Browser integration
- Chrome extension MV3.
- Native bridge / local RPC.
- Handoff URL ke aplikasi, termasuk jalur `--add-url`.
- Registrasi HKCU tanpa admin pernah dicatat.
- Context-menu download dan opsi automatic interception pernah dicatat pada tahap baseline.

### App behavior
- Single application window / single-instance flow pernah dicatat.
- Downloads folder sebagai output awal pernah dicatat.
- Pengembangan berikutnya menambah queue/scheduler, persistence, kategori, dan integrasi browser yang lebih kuat.

## Anchor bugfix historis `31e335c`

Riwayat lama menyebut commit ini memperbaiki beberapa area berikut:

- queue-limit counting;
- batas aria2 max 20;
- save race;
- startup/browser URL handling;
- async single-instance dispatch;
- Space pause dinonaktifkan untuk FFmpeg/YouTube path tertentu.

Daftar ini adalah petunjuk penting untuk regression tests pada source rekonstruksi.

## Anchor build historis `da2aa9c`

Riwayat dan inventaris recovery sama-sama menempatkan commit ini sebagai baseline/final portable build yang berhasil, dengan tests/artifacts dilaporkan PASS.

Jika artifact/source lama ditemukan, pencarian pertama harus diarahkan ke commit ini.

## Perkembangan setelah baseline

Riwayat percakapan lama menyebut pekerjaan parity IDM yang lebih luas, termasuk beberapa commit UI dan PR bertahap. Beberapa area yang pernah dikerjakan/diaudit:

- WPF IDM-like toolbar/categories/table;
- dialogs dan progress;
- queue controls;
- browser integration;
- diagnostics;
- app version/title bar;
- persisted queue order;
- stop-all semantics;
- browser success timing;
- refresh expired URL;
- retry/integrity;
- batch/all-links;
- import/export;
- ZIP preview;
- offline-site storage.

Status final tiap bagian tidak bisa dipastikan tanpa source lama.

## Arsitektur rekonstruksi yang harus dipertahankan

Saat source ORIGINAL tidak ditemukan, rekonstruksi sebaiknya tetap menjaga boundary berikut agar kompatibel dengan sejarah proyek:

```text
WPF UI
  |
Application / Commands
  |
Download Domain + Queue/Scheduler
  |
Download Engine Adapter
  |-- aria2 process/RPC
  |-- optional specialized handlers
  |
Persistence
  |
Browser Bridge
      |-- Chrome MV3 extension
      |-- native/local handoff
```

## Prinsip implementasi ulang

1. aria2 harus diperlakukan sebagai adapter, bukan dicampur langsung ke semua ViewModel.
2. Download item/state harus mempunyai model domain yang dapat diuji tanpa UI.
3. Queue/scheduler harus deterministic dan mempunyai regression tests.
4. Persistence harus atomic untuk mencegah save race yang pernah muncul.
5. Browser handoff harus idempotent dan aman untuk single-instance.
6. UI hanya memproyeksikan state domain, bukan menjadi sumber kebenaran.
7. Komponen browser extension harus dipisahkan dari desktop source tetapi memiliki protocol contract yang versioned.

## Test targets dari riwayat bug lama

Rekonstruksi minimal harus mempunyai test untuk:

- queue active-count limit;
- max connections clamp <= 20;
- concurrent save / atomic persistence;
- startup dengan URL dari browser;
- second-instance URL dispatch;
- pause/resume correctness;
- stop-all tidak diam-diam melanjutkan queue;
- persisted queue order benar-benar digunakan setelah restart;
- browser handoff tidak mengirim success sebelum desktop menerima URL;
- expired URL refresh flow;
- retry tidak merusak partial file/integrity.

## Catatan provenance

Semua source yang dibangun dari dokumen ini harus diberi label `RECONSTRUCTED` sampai dibandingkan dengan source/artifact lama yang benar-benar ditemukan.
