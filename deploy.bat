@echo off
setlocal
echo ========================================================
echo  DOCUFLOW - ONE-CLICK DEPLOY BACKEND KE CLOUDFLARE
echo ========================================================
echo.

cd /d "%~dp0backend"

echo [1/3] Menjalankan migrasi database D1 ke Remote Cloudflare...
call npx wrangler d1 execute DB --remote --file=./migrations/0001_init.sql
call npx wrangler d1 execute DB --remote --file=./migrations/0002_features.sql
call npx wrangler d1 execute DB --remote --file=./migrations/seed.sql
call npx wrangler d1 execute DB --remote --file=./migrations/seed_features.sql

echo.
echo [2/3] Menerapkan aplikasi Cloudflare Workers (Hono)...
call npx wrangler deploy --minify

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Deployment gagal! Pastikan Anda sudah login via 'npx wrangler login'.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Deployment Cloudflare Workers Sukses 100%!
echo Backend Anda telah aktif 24 jam di edge network Cloudflare.
echo.
pause
