@echo off
echo ========================================
echo Building Subtitle draft GMTPC (C# Debug)
echo ========================================
echo.

:: Find the newest Visual Studio MSBuild first. SDK-style projects such as this one
:: need the matching MSBuild/.NET SDK resolver, so old Build Tools paths can fail.
set "MSBUILD="
set "VSCMD="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if exist "%VSWHERE%" (
    for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        if not defined MSBUILD set "MSBUILD=%%I"
    )
    for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -find Common7\Tools\VsDevCmd.bat`) do (
        if not defined VSCMD set "VSCMD=%%I"
    )
)

if not defined MSBUILD if exist "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
    set "VSCMD=C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\VsDevCmd.bat"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
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

if defined MSBUILD (
    echo Using MSBuild: %MSBUILD%
    echo.
    if defined VSCMD (
        call "%VSCMD%" >nul
    )
    "%MSBUILD%" "Subtitle draft GMTPC.csproj" /p:Configuration=Debug /t:Build /nologo /v:minimal
    set "BUILD_RESULT=%ERRORLEVEL%"
) else (
    where dotnet >nul 2>nul
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] Neither Visual Studio MSBuild nor dotnet CLI was found.
        echo Install Visual Studio 2026 or the .NET SDK, then try again.
        pause
        exit /b 1
    )

    echo Using dotnet build fallback
    echo.
    dotnet build "Subtitle draft GMTPC.csproj" -c Debug -v minimal
    set "BUILD_RESULT=%ERRORLEVEL%"
)

if %BUILD_RESULT% NEQ 0 (
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b %BUILD_RESULT%
)

echo.
echo ========================================
echo Build successful!
echo ========================================
echo.

:: Copy exe to root folder
echo Copying exe to root folder...
copy /Y "bin\Debug\net48\Subtitle draft GMTPC.exe" "Subtitle draft GMTPC.exe" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [WARNING] Failed to copy exe to root folder.
) else (
    echo [OK] Exe copied to root: Subtitle draft GMTPC.exe
)
echo.

:: Copy runtime dependencies to root so the app can run from a portable folder.
echo Copying runtime dependencies to root folder...
copy /Y "bin\Debug\net48\Microsoft.Web.WebView2.Core.dll" "Microsoft.Web.WebView2.Core.dll" >nul
copy /Y "bin\Debug\net48\Microsoft.Web.WebView2.WinForms.dll" "Microsoft.Web.WebView2.WinForms.dll" >nul
copy /Y "bin\Debug\net48\Microsoft.Web.WebView2.Wpf.dll" "Microsoft.Web.WebView2.Wpf.dll" >nul
copy /Y "bin\Debug\net48\WebView2Loader.dll" "WebView2Loader.dll" >nul
copy /Y "bin\Debug\net48\Subtitle draft GMTPC.exe.config" "Subtitle draft GMTPC.exe.config" >nul
copy /Y "bin\Debug\net48\Subtitle draft GMTPC.pdb" "Subtitle draft GMTPC.pdb" >nul
if exist "bin\Debug\net48\Subtitle draft GMTPC.exe.WebView2" (
    if exist "Subtitle draft GMTPC.exe.WebView2" rmdir /S /Q "Subtitle draft GMTPC.exe.WebView2"
    xcopy /E /I /Y "bin\Debug\net48\Subtitle draft GMTPC.exe.WebView2" "Subtitle draft GMTPC.exe.WebView2" >nul
)
if exist "bin\Debug\net48\runtimes" (
    if exist "runtimes" rmdir /S /Q "runtimes"
    xcopy /E /I /Y "bin\Debug\net48\runtimes" "runtimes" >nul
)
echo [OK] Runtime dependencies copied to root.
echo.
:: Append portable payload to the root exe so it can self-extract when copied alone.
echo Embedding portable payload into exe...
powershell -NoProfile -ExecutionPolicy Bypass -File "build_portable_package.ps1" -BuildDir "bin\Debug
et48" -SourceRoot "." -ExePath ".\Subtitle draft GMTPC.exe"
if %ERRORLEVEL% NEQ 0 (
    echo [WARNING] Failed to embed portable payload into exe.
) else (
    echo [OK] Portable payload embedded into Subtitle draft GMTPC.exe
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
