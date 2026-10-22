@echo off
echo ===================================================
echo INITIALIZING LOCAL SQL DATABASE & API NODE
echo ===================================================
cd backend
call npx wrangler d1 execute DB --local --file=../database/0001_init.sql
call npx wrangler d1 execute DB --local --file=../database/seed.sql
echo Database SQL Lokal berhasil diinisialisasi. Menjalankan Server API Lokal...
call npx wrangler dev --port 8787 --local
