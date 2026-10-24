# StructuralSolver2D M86 RC1: clean-build, 4-project regression, CLI smoke and exports.
# Windows PowerShell 5.1+; .NET SDK 8.0; run from an extracted project folder.
# Usage: .\scripts\run-m86-release-gate.ps1 [-SkipClean]
[CmdletBinding()]
param([switch]$SkipClean)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M86 RC1 1/5: prepare pristine build'
    & (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
    if (-not $SkipClean) { & (Join-Path $PSScriptRoot 'clean-project-build.ps1') }
    . (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

    Write-Host 'M86 RC1 2/5: Release build, warning-as-error'
    dotnet build StructuralSolver2D.sln -c Release --tl:off -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "Build FAILED: $script:StructuralConsoleLogPath" }

    Write-Host 'M86 RC1 3/5: full regression (Core / Analysis / Reporting / Cli)'
    $projects = @(
        'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj',
        'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj',
        'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj',
        'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj'
    )
    $failed = @()
    foreach ($project in $projects) {
        Write-Host "Regression: $project"
        dotnet test --tl:off $project -c Release --no-build --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { $failed += $project }
    }
    if ($failed.Count -gt 0) { throw "Regression FAILED: $($failed -join ', '). Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M86 RC1 4/5: CLI end-to-end smoke (analysis, combination, reports)'
    $output = Join-Path $root 'artifacts/m86-rc1-smoke'
    if (-not (Test-Path -LiteralPath $output)) { New-Item -ItemType Directory -Path $output -Force | Out-Null }
    $cli = 'src/StructuralSolver2D.Cli/StructuralSolver2D.Cli.csproj'
    $cantilever = 'examples/beams/cantilever-point-load.json'
    $truss = 'examples/trusses/simple-truss.json'
    $combination = 'examples/combinations/load-combination.json'

    function Invoke-M86Cli([string[]]$CliArguments, [string]$ExpectedText) {
        # The stable-dotnet-console.ps1 wrapper preserves the native exit code and full log.
        dotnet run --project $cli -c Release --no-build -- @CliArguments
        if ($LASTEXITCODE -ne 0) {
            throw "M86 CLI FAILED: $($CliArguments -join ' '). Log: $script:StructuralConsoleLogPath"
        }
        if ($ExpectedText) {
            $logText = Get-Content -LiteralPath $script:StructuralConsoleLogPath -Raw -ErrorAction Stop
            if (-not $logText.Contains($ExpectedText)) {
                throw "M86 CLI output expected '$ExpectedText' not found in $script:StructuralConsoleLogPath"
            }
        }
    }
    Invoke-M86Cli -CliArguments @('analyze', $cantilever) -ExpectedText 'Analysis completed.'
    Invoke-M86Cli -CliArguments @('analyze', $truss) -ExpectedText 'Analysis completed.'
    Invoke-M86Cli -CliArguments @('analyze', $combination, 'ULS1') -ExpectedText 'Analysis completed.'
    Invoke-M86Cli -CliArguments @('report', $cantilever, (Join-Path $output 'cantilever.md')) -ExpectedText ''
    Invoke-M86Cli -CliArguments @('export-csv', $cantilever, (Join-Path $output 'csv')) -ExpectedText ''
    Invoke-M86Cli -CliArguments @('export-xlsx', $cantilever, (Join-Path $output 'cantilever.xlsx')) -ExpectedText ''
    Invoke-M86Cli -CliArguments @('export-pdf', $cantilever, (Join-Path $output 'cantilever.pdf')) -ExpectedText ''

    Write-Host 'M86 RC1 5/5: check report artifacts'
    $required = @(
        (Join-Path $output 'cantilever.md'),
        (Join-Path $output 'cantilever.xlsx'),
        (Join-Path $output 'cantilever.pdf')
    )
    foreach ($file in $required) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or (Get-Item -LiteralPath $file).Length -le 8) {
            throw "M86 missing/empty report: $file"
        }
    }
    $csvFiles = @(Get-ChildItem -LiteralPath (Join-Path $output 'csv') -File -Filter '*.csv' -ErrorAction Stop)
    if ($csvFiles.Count -lt 6) { throw "M86 CSV export incomplete: $($csvFiles.Count) files, expected >= 6" }
    $pdf = [System.IO.File]::ReadAllBytes((Join-Path $output 'cantilever.pdf'))
    if ([System.Text.Encoding]::ASCII.GetString($pdf, 0, 5) -ne '%PDF-') { throw 'M86 PDF magic header invalid.' }
    $xlsx = [System.IO.File]::ReadAllBytes((Join-Path $output 'cantilever.xlsx'))
    if ($xlsx[0] -ne 0x50 -or $xlsx[1] -ne 0x4B) { throw 'M86 XLSX ZIP header invalid.' }

    Write-Host 'M86 RC1 GATE PASSED: clean build + regression + CLI/export smoke.' -ForegroundColor Green
    Write-Host "Artifacts: $output"
    Write-Host "Console log: $script:StructuralConsoleLogPath"
    Write-Host 'LIMITS: no external multi-model FEM cross-check, full PDF numerical audit, normative checks or structural certification.'
}
finally { Pop-Location }
