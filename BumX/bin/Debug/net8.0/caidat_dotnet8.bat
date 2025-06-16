@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title Cài Đặt .NET-8
color 0A

set url=https://download.visualstudio.microsoft.com/download/pr/6f043b39-b3d2-4f0a-92bd-99408739c98d/fa16213ea5d6464fa9138142ea1a3446/dotnet-sdk-8.0.407-win-x64.exe

rem Tải tệp cài đặt xuống
echo Downloading .NET-8 exe from !url!
curl -L -o .NET-8.exe !url!

rem Cài đặt .NET-8 với tham số /quiet /norestart
echo Installing .NET-8...
.NET-8.exe /install /quiet /norestart
rem Xoá tệp cài đặt sau khi cài xong
del .NET-8.exe

echo Cài Đặt .NET 8.0 hoàn tất
pause
