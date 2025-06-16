@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title Trình Cài Đặt Node.js và Appium
color 0A

REM === Hiển thị tiêu đề ===
echo ===================================================
echo      Trình Cài Đặt Node.js v16.20.2 + Appium v1.22.3
echo ===================================================
echo.

REM === Kiểm tra phiên bản npm và appium hiện tại ===
set "npm_ok=0"
set "appium_ok=0"
set "npmv="
set "appiumv="

REM Kiểm tra npm
for /f %%i in ('npm -v 2^>nul') do set "npmv=%%i"
if "!npmv!"=="8.19.4" set "npm_ok=1"

REM Kiểm tra appium
for /f %%j in ('appium -v 2^>nul') do set "appiumv=%%j"
if "!appiumv!"=="1.22.3" set "appium_ok=1"

REM Nếu cả hai đều đúng phiên bản thì bỏ qua cài đặt
if "!npm_ok!"=="1" if "!appium_ok!"=="1" goto show_versions

REM === Tải và cài đặt Node.js nếu npm không đúng phiên bản ===
if not "!npm_ok!"=="1" (
    echo [•] Đang tải Node.js v16.20.2...
    curl -# -L -o "node-v16.20.2-x64.msi" https://nodejs.org/dist/v16.20.2/node-v16.20.2-x64.msi

    if exist "node-v16.20.2-x64.msi" (
        echo [+] Tải thành công!
    ) else (
        echo [!] Lỗi: Không thể tải Node.js. Kiểm tra kết nối mạng.
        pause
        exit /b
    )

    echo.
    echo [!] Mở trình cài đặt Node.js. Vui lòng hoàn tất cài đặt thủ công.
    start "" "node-v16.20.2-x64.msi"

    REM Chờ cài xong node
    set /a count=0
    :wait_node
    set /a count+=1
    echo [!count!] Đang đợi cài đặt Node.js hoàn tất...
    timeout /t 2 >nul

    where npm >nul 2>&1
    if %errorlevel% neq 0 goto wait_node

    echo.
    echo [✓] Node.js đã được cài đặt thành công!
    del /f /q "node-v16.20.2-x64.msi" >nul 2>&1
)

REM === Cài đặt Appium nếu chưa đúng phiên bản ===
if not "!appium_ok!"=="1" (
    echo.
    echo [•] Đang cài Appium 1.22.3...
    call npm install -g appium@1.22.3
    if %errorlevel% neq 0 (
        echo [!] Lỗi khi cài Appium!
        pause
        exit /b
    )
)

:show_versions
REM === Kiểm tra lại phiên bản sau khi cài ===
echo.
echo [✓] Kiểm tra phiên bản sau cài đặt:
echo ----------------------------
call npm -v
call appium -v
echo ----------------------------

echo.
echo [✓] Quá trình cài đặt hoàn tất!
echo Nhấn phím bất kỳ để thoát...
pause >nul
exit /b
