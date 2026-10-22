# Master Templates (.docx)

Folder ini berisi master template Microsoft Word (.docx) untuk pengujian lokal.

## Cara Kerja Placeholder MiniWord

Sistem menggunakan pustaka **MiniSoftware.MiniWord** untuk memproses dokumen Word secara lokal tanpa membutuhkan Microsoft Word terinstal di komputer.

Placeholder di dalam file `.docx` dibuat menggunakan kurung kurawal ganda, contoh:

- `{{nomor_surat}}`
- `{{nama_pegawai}}`
- `{{nip}}`
- `{{jabatan}}`
- `{{unit_kerja}}`
- `{{tujuan_tugas}}`
- `{{keperluan_tugas}}`
- `{{tgl_mulai}}`
- `{{tgl_selesai}}`
- `{{transportasi}}`
- `{{beban_anggaran}}`
- `{{kota_terbit}}`
- `{{tgl_surat}}`
- `{{nama_pejabat}}`
- `{{jabatan_pejabat}}`

Setiap placeholder `{{variabel}}` akan secara otomatis digantikan dengan nilai input dari form dinamis di aplikasi desktop WPF.
Template juga dapat disimpan di Cloudinary dan diunduh secara on-demand oleh aplikasi.
