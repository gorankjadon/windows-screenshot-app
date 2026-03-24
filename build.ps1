param(
    [string]$OutputPath = "dist\\GstackScreenshot.exe"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $root "src\GstackScreenshot\GstackScreenshot.csproj"
$output = Join-Path $root $OutputPath
$outputDir = Split-Path -Parent $output
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$dotnetInfo = & dotnet --list-sdks 2>$null
if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace(($dotnetInfo | Out-String))) {
    & dotnet build $projectPath -c Release -o $outputDir
    exit $LASTEXITCODE
}

$compiler = (Get-Command csc -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue)
if (-not $compiler) {
    $candidates = @(
        "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            $compiler = $candidate
            break
        }
    }
}

if (-not $compiler) {
    throw "Could not find a usable C# compiler. Install the .NET SDK or make csc.exe available on PATH."
}

$sources = Get-ChildItem -Path (Join-Path $root "src\GstackScreenshot") -Filter *.cs | ForEach-Object { $_.FullName }
& $compiler /nologo /target:winexe /out:$output /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Runtime.Serialization.dll /r:System.Windows.Forms.dll $sources
