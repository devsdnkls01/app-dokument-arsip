@echo off
setlocal
echo ========================================================
echo  DOCUFLOW - LOCAL ONE-CLICK BUILD SCRIPT (.EXE STANDALONE)
echo ========================================================
echo.

cd /d "%~dp0"

echo [1/3] Menyiapkan environment .NET 8...
set "PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%"
set "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"

echo [2/3] Mengompilasi Single-File Executable mandiri...
dotnet publish desktop\SuratApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist\desktop

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Kompilasi gagal! Periksa pesan kesalahan di atas.
    pause
    exit /b %ERRORLEVEL%
)

echo [3/3] Selesai! File SuratApp.exe siap digunakan.
echo.
echo Lokasi output: %~dp0dist\desktop\SuratApp.exe
echo.
explorer "%~dp0dist\desktop"
pause
