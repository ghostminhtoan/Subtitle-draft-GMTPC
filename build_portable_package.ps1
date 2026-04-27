param(
    [Parameter(Mandatory = $true)]
    [string]$BuildDir,

    [Parameter(Mandatory = $true)]
    [string]$SourceRoot,

    [Parameter(Mandatory = $true)]
    [string]$ExePath
)

$ErrorActionPreference = "Stop"

$marker = [System.Text.Encoding]::ASCII.GetBytes("GMTPC_PAYLOAD_V1")
$payloadStage = Join-Path $env:TEMP "gmtpc-portable-stage"
$payloadZip = Join-Path $env:TEMP "gmtpc-portable-payload.zip"

if (Test-Path $payloadStage) {
    Remove-Item -Recurse -Force $payloadStage
}
if (Test-Path $payloadZip) {
    Remove-Item -Force $payloadZip
}

New-Item -ItemType Directory -Path $payloadStage -Force | Out-Null

function Copy-Folder {
    param(
        [string]$From,
        [string]$To
    )

    if (Test-Path $From) {
        New-Item -ItemType Directory -Path $To -Force | Out-Null
        Copy-Item -Path (Join-Path $From '*') -Destination $To -Recurse -Force
    }
}

Copy-Folder -From (Join-Path $SourceRoot 'Tutorials') -To (Join-Path $payloadStage 'Tutorials')
Copy-Folder -From (Join-Path $SourceRoot 'Prompt') -To (Join-Path $payloadStage 'Prompt')
Copy-Folder -From (Join-Path $SourceRoot 'english word rules karaoke') -To (Join-Path $payloadStage 'english word rules karaoke')

foreach ($file in @(
    'Microsoft.Web.WebView2.Core.dll',
    'Microsoft.Web.WebView2.WinForms.dll',
    'Microsoft.Web.WebView2.Wpf.dll',
    'WebView2Loader.dll',
    'Subtitle draft GMTPC.exe.config'
)) {
    $sourceFile = Join-Path $BuildDir $file
    if (Test-Path $sourceFile) {
        Copy-Item -Path $sourceFile -Destination (Join-Path $payloadStage $file) -Force
    }
}

if (Test-Path (Join-Path $BuildDir 'runtimes')) {
    Copy-Folder -From (Join-Path $BuildDir 'runtimes') -To (Join-Path $payloadStage 'runtimes')
}

Compress-Archive -Path (Join-Path $payloadStage '*') -DestinationPath $payloadZip -Force

$zipBytes = [System.IO.File]::ReadAllBytes($payloadZip)
$exeStream = [System.IO.File]::Open($ExePath, [System.IO.FileMode]::Append, [System.IO.FileAccess]::Write, [System.IO.FileShare]::Read)
try {
    $exeStream.Write($zipBytes, 0, $zipBytes.Length)
    $lengthBytes = [BitConverter]::GetBytes([int64]$zipBytes.Length)
    $exeStream.Write($lengthBytes, 0, $lengthBytes.Length)
    $exeStream.Write($marker, 0, $marker.Length)
}
finally {
    $exeStream.Close()
}

Remove-Item -Force $payloadZip -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force $payloadStage -ErrorAction SilentlyContinue
