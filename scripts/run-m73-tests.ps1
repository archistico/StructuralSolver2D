# M73: clean numerical regression AND opt-in, machine-specific performance baseline.
# All numerical calculations and benchmarks run in C#/.NET 8, not Python.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    foreach ($relative in @(
        'tools/StructuralSolver2D.PerformanceM73/StructuralSolver2D.PerformanceM73.csproj',
        'tools/StructuralSolver2D.PerformanceM73/M73BenchmarkSuite.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Performance/M73/PerformanceBaselineM73Tests.cs',
        'docs/structural/m73-performance-baseline.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative))) {
            throw "M73 prerequisite missing: $relative"
        }
    }
    Write-Host 'M73 1/4: clean Release build of all solution projects'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M73 Release build failed.' }

    Write-Host 'M73 2/4: focused C# numerical/performance contract tests (NO time thresholds)'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~PerformanceBaselineM73Tests' `
        --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M73 benchmark numerical gate FAILED.' }

    Write-Host 'M73 3/4: complete regression including M71/M72 Frame2D tests'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M73 complete regression FAILED.' }

    Write-Host 'M73 4/4: C# performance baseline (CSV/JSON, descriptive measurements only)'
    $output = Join-Path $root 'artifacts/benchmarks/m73'
    dotnet run --no-build --project tools/StructuralSolver2D.PerformanceM73/StructuralSolver2D.PerformanceM73.csproj `
        -c Release -- --repetitions 3 --output $output
    if ($LASTEXITCODE -ne 0) { throw 'M73 baseline benchmark FAILED.' }
    if (-not (Test-Path -LiteralPath (Join-Path $output 'm73-baseline.csv')) -or
        -not (Test-Path -LiteralPath (Join-Path $output 'm73-baseline.json'))) {
        throw 'M73 baseline report files missing.'
    }
    Write-Host 'M73 automated numerical checks PASSED. Performance results describe this machine, not a pass/fail speed claim.'
    Write-Host "Benchmark reports: $output"
    Write-Host "Console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
