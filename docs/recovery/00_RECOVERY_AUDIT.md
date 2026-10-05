# 00 — Recovery Audit DOWNLOAD-AJA

Tanggal: 2026-10-06
Status: **AUDIT TAHAP 1 SELESAI**

## 1. Tujuan

Dokumen ini memetakan apa yang masih diketahui tentang DOWNLOAD-AJA sebelum source baru ditulis. Prinsip utama: **recovery dulu, rekonstruksi kemudian**.

Tidak boleh menyamakan planning, screenshot, riwayat chat, portable build, atau hasil rekonstruksi dengan source asli.

## 2. Kondisi repo aktif

Repo aktif: `inoriko920-dev/DOWNLOAD-AJA`

Saat audit dimulai repo baru belum memiliki commit/source. Repo kemudian hanya diinisialisasi untuk menyimpan dokumentasi recovery. Tidak ada source aplikasi baru yang dimasukkan pada tahap ini.

Repo migrasi lama yang tercatat dalam arsip:

`tonitaru6-cloud/Download-Aja-`

Percobaan akses pada 2026-10-06 menghasilkan 404, sehingga source lama belum dapat diambil langsung dari repo tersebut.

## 3. Bukti file-backed dari Library

Inventaris pemulihan ChatGPT mencatat:

- status pemulihan: riwayat implementasi ada, file Library terbatas;
- fix commit: `31e335c`;
- final portable build success: `da2aa9c`;
- tests/artifacts dilaporkan PASS;
- target produk: IDM-like;
- referensi UI: `Download Aja Manager Interface.png`;
- workflow ASTRA/SOL umum tersedia;
- plan IDM spesifik tidak ditemukan saat inventaris dibuat.

Pencarian ulang Library pada 2026-10-06 menemukan dua referensi UI:

1. `Download Aja Manager Interface.png`
2. `Download-Aja_UI_Reference.png`

Keduanya menunjukkan desain yang sama.

## 4. Kontrak visual yang terlihat dari referensi UI

Elemen berikut terlihat langsung pada referensi UI dan dapat dianggap sebagai **visual baseline** untuk rekonstruksi:

### Menu atas
- Tugas
- Unduh
- Lihat
- Jadwal
- Kategori
- Pilihan
- Bantuan

### Toolbar utama
- Tambah URL
- Mulai
- Jeda
- Hentikan
- Hapus
- Jadwal
- Ambil dari Browser
- Opsi
- Bantuan

### Sidebar kategori
- Semua Unduhan
- Selesai
- Belum Selesai
- Antrean
- Video
- Audio
- Dokumen
- Arsip
- Program

### Tabel download
Kolom yang terlihat:
- Nama File
- Ukuran
- Status
- Kecepatan
- Sisa Waktu
- Tanggal
- Folder

### Panel bawah
Tab:
- Detail Unduhan
- Progress
- Log

Detail yang terlihat:
- Nama File
- Status
- Ukuran
- Kecepatan
- Sisa Waktu
- Alamat
- Folder

### Status bar
Menampilkan setidaknya:
- status aplikasi (`Siap` pada contoh);
- jumlah unduhan aktif;
- total kecepatan;
- indikator aktivitas.

## 5. Riwayat implementasi dari percakapan lama

Bagian ini **bukan file-backed source**. Informasi di bawah berasal dari riwayat percakapan lama dan harus diberi status HISTORICAL sampai source atau artifact membuktikannya kembali.

Riwayat mencatat baseline teknis berikut:

- C# / .NET 8;
- WPF UI;
- aria2 1.37.0 portable engine;
- Chrome MV3 extension;
- native bridge / local RPC;
- segmented HTTP/HTTPS downloads;
- sekitar 8 koneksi pada baseline lama;
- pause / resume / stop / delete;
- progress / speed / ETA;
- Downloads folder sebagai output awal;
- single app window;
- browser integration melalui extension dan `--add-url`;
- registrasi HKCU tanpa admin;
- Windows Actions build;
- portable artifact pernah berhasil dibangun.

Riwayat berikutnya mencatat pengembangan menuju parity IDM yang lebih luas, antara lain persistent history, kategori, search, queue/scheduler, speed limiter, connection settings, browser/video handling, retry/integrity, refresh expired URL, batch/all-links, import/export, ZIP preview, dan offline-site storage. Status tiap fitur tersebut **tidak boleh dianggap final** tanpa source lama.

## 6. Historical anchors

Anchor yang berguna jika suatu saat source/cache ditemukan:

- `da2aa9c` — dilaporkan sebagai final portable build success / baseline stabil.
- `31e335c` — dilaporkan sebagai bugfix setelah baseline.
- `6f42176...` — riwayat menyebut ASTRA plan untuk IDM parity.
- `d36efe3` — riwayat menyebut audit/planning commit.
- PR #22 — riwayat menyebut pekerjaan refresh expired download address dan CI 11/11 PASS.

Commit/PR di atas adalah petunjuk pencarian. Hanya dua anchor pertama yang juga tercatat di file inventaris Library saat ini.

## 7. Apa yang masih hilang

Belum ditemukan sebagai file langsung di Library pada audit ini:

- source ZIP DOWNLOAD-AJA;
- Git bundle / `.git` history;
- portable DOWNLOAD-AJA lama;
- solution/project C# lama;
- Chrome extension source lama;
- workflow YAML lama;
- tests lama;
- plan IDM khusus sebagai dokumen tersendiri;
- release artifact asli.

## 8. Risiko rekonstruksi

Risiko utama jika langsung menulis ulang dari nol:

1. struktur source bisa berbeda dari implementasi lama;
2. bug yang sudah pernah diperbaiki dapat muncul lagi;
3. perilaku browser integration bisa berubah;
4. queue/scheduler semantics dapat tidak cocok;
5. UI dapat terlihat mirip tetapi alur tidak identik;
6. source hasil rekonstruksi bisa keliru dianggap sebagai source lama.

Karena itu, source baru harus dibangun dengan label `RECONSTRUCTED`, bukan `ORIGINAL`.

## 9. Keputusan recovery

Tahap berikutnya tidak dimulai dengan fitur baru. Urutannya:

1. bangun struktur repo recovery;
2. tulis kontrak UI dan historical architecture map;
3. buat testable reconstruction backlog;
4. mulai pondasi minimal C#/.NET/WPF hanya setelah kontrak recovery terkunci;
5. implementasikan core download lebih dulu;
6. browser extension dan parity fitur masuk setelah core terbukti;
7. setiap milestone wajib memiliki test + build evidence.

## 10. Definition of recovered

DOWNLOAD-AJA belum boleh disebut "pulih" hanya karena UI bisa dibuka. Minimal harus kembali mempunyai:

- source yang dapat dibangun;
- downloader nyata, bukan mock;
- pause/resume/stop;
- persisted state yang aman;
- queue behavior yang diuji;
- browser handoff yang diuji;
- referensi UI lama terjaga;
- automated tests;
- Windows build artifact;
- recovery provenance yang jelas untuk tiap bagian.

Lanjut ke:

- `01_UI_REFERENCE_CONTRACT.md`
- `02_HISTORICAL_ARCHITECTURE.md`
- `03_RECONSTRUCTION_BACKLOG.md`
