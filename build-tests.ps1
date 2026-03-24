param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$testSource = Join-Path $root "tests\GstackScreenshot.Tests\Program.cs"
$testBinary = Join-Path $root "dist\GstackScreenshot.Tests.exe"

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
    throw "Could not find a usable C# compiler for tests."
}

$appSources = Get-ChildItem -Path (Join-Path $root "src\GstackScreenshot") -Filter *.cs |
    Where-Object { $_.Name -ne 'Program.cs' } |
    ForEach-Object { $_.FullName }

& $compiler /nologo /target:exe /out:$testBinary /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Runtime.Serialization.dll /r:System.Windows.Forms.dll $appSources $testSource
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $testBinary
exit $LASTEXITCODE
