@echo off
echo ========================================
echo Building Subtitle draft GMTPC (C# Debug)
echo ========================================
echo.

:: Find MSBuild
set "MSBUILD="
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
)

if "%MSBUILD%"=="" (
    echo [ERROR] MSBuild not found! Please install Visual Studio or Build Tools.
    echo Download from: https://visualstudio.microsoft.com/downloads/
    pause
    exit /b 1
)

echo Using: %MSBUILD%
echo.

:: Build the C# project
"%MSBUILD%" "Subtitle draft GMTPC.csproj" /p:Configuration=Debug /t:Build /nologo /v:minimal
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo Build successful!
echo ========================================
echo.

:: Copy exe to root folder
echo Copying exe to root folder...
copy /Y "bin\Debug\Subtitle draft GMTPC.exe" "Subtitle draft GMTPC.exe" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [WARNING] Failed to copy exe to root folder.
) else (
    echo [OK] Exe copied to root: Subtitle draft GMTPC.exe
)
echo.

:: Push changes to git repository
echo.
echo Pushing changes to git repository...
echo Adding changes...
git add .
echo Added.
set /p COMMIT_MSG="Enter commit message: "
echo Committing with message: "%COMMIT_MSG%"
git commit -m "%COMMIT_MSG%" --allow-empty
if %ERRORLEVEL% EQU 0 (
    echo Commit successful, pushing...
    git push
    if %ERRORLEVEL% EQU 0 (
        echo Push successful.
    ) else (
        echo Push failed.
    )
) else (
    echo Commit failed, skipping push.
)

:: Run the executable
echo Starting application...
start "" "Subtitle draft GMTPC.exe"

echo.
echo Application launched.

pause
