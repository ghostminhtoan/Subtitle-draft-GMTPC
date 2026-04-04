@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ============================================
echo   Git Push - Subtitle draft GMTPC
echo ============================================
echo.

REM --- Cấu hình Git remote ---
set "REMOTE_URL=https://github.com/ghostminhtoan/Subtitle-draft-GMTPC.git"

echo [1/5] Dang cau hinh remote origin...
git remote remove origin 2>nul
git remote add origin %REMOTE_URL%
echo [OK] Remote origin da duoc cau hinh.

echo.
echo [2/5] Dang git status...
git status --short

echo.
echo [3/5] Dang git add...
git add -A

echo.
echo [4/5] Nhap noi dung commit:
set /p "commit_msg=Commit message: "

if "!commit_msg!"=="" (
    for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
    set "YY=!dt:~2,2!"
    set "MM=!dt:~4,2!"
    set "DD=!dt:~6,2!"
    set "HH=!dt:~8,2!"
    set "NN=!dt:~10,2!"
    set "commit_msg=Update !DD!/!MM!/!YY! !HH!:!NN!"
    echo [INFO] Khong nhap, su dung mac dinh: !commit_msg!
)

git commit -m "!commit_msg!"
if %ERRORLEVEL% NEQ 0 (
    echo [INFO] Khong co gi de commit.
) else (
    echo [OK] Commit thanh cong: !commit_msg!
)

echo.
echo [5/5] Dang git push...
git push -u origin master
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [LOI] Push that bai! Co the ban can xac thuc tai khoan GitHub.
    pause
    goto :open_app
)

echo.
echo ============================================
echo   Push thanh cong len GitHub!
echo ============================================

:open_app
echo.
echo [INFO] Mo ung dung da build...

REM --- Tim file exe trong thu muc bin/Debug ---
set "EXE_PATH="
for %%f in ("bin\Debug\*.exe") do (
    if exist "%%f" (
        set "EXE_PATH=%%f"
        goto :found_exe
    )
)

REM --- Thu muc bin/Release ---
for %%f in ("bin\Release\*.exe") do (
    if exist "%%f" (
        set "EXE_PATH=%%f"
        goto :found_exe
    )
)

:found_exe
if defined EXE_PATH (
    echo [OK] Tim thay: !EXE_PATH!
    start "" "!EXE_PATH!"
) else (
    echo.
    echo [CANH BAO] Khong tim thay file exe trong bin\Debug hoac bin\Release.
    echo [INFO] Hay build project trong Visual Studio truoc khi chay script nay.
)

echo.
echo ============================================
echo   Hoan tat!
echo ============================================
timeout /t 3 >nul
