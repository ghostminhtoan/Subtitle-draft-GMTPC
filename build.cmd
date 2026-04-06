@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ============================================
echo   Build & Run - Subtitle draft GMTPC
echo ============================================
echo.

REM --- Cau hinh Visual Studio ---
set "DEVENV=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.com"
set "SLN_PATH=%~dp0Subtitle draft GMTPC.sln"

if not exist "%DEVENV%" (
    echo [LOI] Khong tim thay Visual Studio 2022!
    pause
    exit /b 1
)

if not exist "%SLN_PATH%" (
    echo [LOI] Khong tim thay solution file!
    pause
    exit /b 1
)

echo [1/3] Dang build project (Debug)...
"%DEVENV%" "%SLN_PATH%" /Build Debug
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [LOI] Build that bai! Hay kiem tra loi trong Visual Studio.
    pause
    exit /b 1
)

echo.
echo ============================================
echo   Build thanh cong!
echo ============================================
echo.
echo [2/4] Dang git push...
git add -A
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=!dt:~2,2!" & set "MM=!dt:~4,2!" & set "DD=!dt:~6,2!" & set "HH=!dt:~8,2!" & set "NN=!dt:~10,2!"
set "commit_msg=Auto build !DD!/!MM!/!YY! !HH!:!NN!"
git commit -m "!commit_msg!" >nul 2>&1
git push -u origin master >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [OK] Push thanh cong: !commit_msg!
) else (
    echo [WARN] Push that bai hoac khong co gi de push.
)

echo.
echo [3/4] Dang tim file exe...

REM --- Tim file exe trong thu muc bin/Debug ---
set "EXE_PATH="
for %%f in ("%~dp0bin\Debug\*.exe") do (
    if exist "%%f" (
        set "EXE_PATH=%%f"
        goto :found_exe
    )
)

REM --- Thu muc bin/Release ---
for %%f in ("%~dp0bin\Release\*.exe") do (
    if exist "%%f" (
        set "EXE_PATH=%%f"
        goto :found_exe
    )
)

:found_exe
if not defined EXE_PATH (
    echo.
    echo [LOI] Khong tim thay file exe trong bin\Debug hoac bin\Release.
    pause
    exit /b 1
)

echo [OK] Tim thay: !EXE_PATH!

echo.
echo [4/4] Dang chay ung dung va mo thu muc...
start "" "!EXE_PATH!"
for %%F in ("!EXE_PATH!") do (
    explorer /select,"%%~fF"
)

echo.
echo ============================================
echo   Hoan tat!
echo ============================================
timeout /t 2 >nul
