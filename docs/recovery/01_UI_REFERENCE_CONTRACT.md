# 01 — UI Reference Contract

Status: **VISUAL BASELINE**

Sumber visual yang ditemukan di Library:

- `Download Aja Manager Interface.png`
- `Download-Aja_UI_Reference.png`

Kedua gambar menunjukkan layout yang sama dan dijadikan acuan visual pemulihan.

## Tujuan

Dokumen ini mengunci bagian UI yang benar-benar terlihat pada referensi. Bagian yang tidak terlihat tidak boleh dianggap pasti sebagai desain lama.

## Struktur layar utama

### 1. Title bar

Nama aplikasi: **Download Aja**.

### 2. Menu bar

Urutan yang terlihat:

`Tugas | Unduh | Lihat | Jadwal | Kategori | Pilihan | Bantuan`

### 3. Toolbar besar

Urutan tombol yang terlihat:

1. Tambah URL
2. Mulai
3. Jeda
4. Hentikan
5. Hapus
6. Jadwal
7. Ambil dari Browser
8. Opsi
9. Bantuan

Karakter visual:

- icon besar;
- label teks di bawah icon;
- separator vertikal antar kelompok;
- gaya desktop Windows klasik/IDM-like;
- area putih/abu terang.

### 4. Sidebar kiri — Kategori

Urutan yang terlihat:

1. Semua Unduhan
2. Selesai
3. Belum Selesai
4. Antrean
5. Video
6. Audio
7. Dokumen
8. Arsip
9. Program

Sidebar menggunakan icon kecil dan selection highlight.

### 5. Tabel utama

Kolom terlihat:

1. Nama File
2. Ukuran
3. Status
4. Kecepatan
5. Sisa Waktu
6. Tanggal
7. Folder

Perilaku visual yang terlihat:

- baris terpilih memiliki highlight;
- status download aktif dapat menampilkan persentase + progress bar;
- download selesai menggunakan status teks;
- download menunggu/jeda dibedakan lewat status teks;
- file memiliki icon sesuai tipe.

### 6. Panel detail bawah

Tab terlihat:

- Detail Unduhan
- Progress
- Log

Pada tab Detail Unduhan terlihat field:

- Nama File
- Status
- Ukuran
- Kecepatan
- Sisa Waktu
- Alamat
- Folder

Alamat ditampilkan seperti hyperlink.

### 7. Status bar

Bagian bawah menampilkan:

- status aplikasi di kiri;
- jumlah unduhan aktif;
- total kecepatan;
- indikator aktivitas di kanan.

## State minimum yang harus didukung saat rekonstruksi

Agar UI tidak sekadar mock, tabel dan detail minimal harus mampu menampilkan state:

- Menunggu
- Mengunduh
- Jeda
- Selesai
- Dihentikan/Gagal

Nama final state internal boleh berbeda, tetapi presentasi pengguna harus konsisten dengan baseline.

## Aturan rekonstruksi UI

1. Jangan redesign sebelum baseline lama berhasil direplikasi secara fungsional.
2. Gunakan referensi screenshot sebagai master visual utama.
3. Jangan menambahkan panel AI, dashboard modern, atau layout baru ke baseline recovery.
4. Fitur baru nanti harus dipisahkan dari fase recovery.
5. UI harus terhubung ke state download nyata, bukan data contoh statis.
6. Semua tombol toolbar yang diaktifkan harus mempunyai aksi nyata dan testable.

## Acceptance UI tahap awal

Recovery UI dianggap cukup untuk masuk ke core integration jika:

- menu bar ada dan urutannya benar;
- toolbar utama ada dan urutannya benar;
- sidebar kategori ada;
- tabel utama dengan tujuh kolom ada;
- panel detail tiga tab ada;
- status bar ada;
- selection tabel mengubah panel detail;
- state download dapat terlihat di tabel;
- layout tetap usable pada Windows scaling umum.

## Yang belum boleh diasumsikan

Screenshot tidak cukup untuk membuktikan:

- isi setiap menu dropdown;
- dialog pengaturan;
- dialog scheduler;
- dialog tambah URL;
- exact keyboard shortcut;
- exact icon asset lama;
- detail progress tab;
- detail log tab;
- sorting/filter behavior;
- context menu;
- theme alternate.

Bagian tersebut harus direkonstruksi dari bukti lain atau ditandai `RECONSTRUCTED`.
