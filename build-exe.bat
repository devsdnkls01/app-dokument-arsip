@echo off
echo ===================================================
echo BUILDING ENTERPRISE SUITE (CLIENT AND PROVISIONING CONSOLE)
echo ===================================================
dotnet publish desktop/ClientApp/ClientApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -p:OptimizationPreference=Speed -o ./dist/ClientApp
dotnet publish desktop/CoreProvisioningConsole/CoreProvisioningConsole.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -p:OptimizationPreference=Speed -o ./dist/CoreProvisioningConsole
powershell -ExecutionPolicy Bypass -File ./sign-exe.ps1
echo Selesai! Kedua file .exe resmi tersedia di folder ./dist/
pause
