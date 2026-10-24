# M83-A: validate numerical behavior, full regression, then collect end-to-end
# Frame2D scalability timings on this Windows machine. No sparse solver changes.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M83-A 1/4: clean Release build'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "M83-A Release build FAILED. Console log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M83-A 2/4: focused M73, M82 and M83 C# numerical gates'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~PerformanceBaselineM73Tests|FullyQualifiedName~Frame2DGlobalSecondOrderM82Tests|FullyQualifiedName~Frame2DScalabilityM83Tests' `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) {
        throw "M83-A numerical gate FAILED. Console log: $script:StructuralConsoleLogPath"
    }

    Write-Host 'M83-A 3/4: complete regression, independently by test project'
    $projects = @(
        @{ Name = 'Core.Tests'; Path = 'tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj' },
        @{ Name = 'Analysis.Tests'; Path = 'tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj' },
        @{ Name = 'Reporting.Tests'; Path = 'tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj' },
        @{ Name = 'Cli.Tests'; Path = 'tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj' }
    )
    $failed = New-Object 'System.Collections.Generic.List[string]'
    foreach ($item in $projects) {
        Write-Host ("M83-A regression: {0}" -f $item.Name)
        dotnet test --tl:off $item.Path -c Release --no-build --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) {
            $failed.Add($item.Name)
            Write-Host ("FAILED: {0}" -f $item.Name) -ForegroundColor Red
        } else {
            Write-Host ("PASSED: {0}" -f $item.Name) -ForegroundColor Green
        }
    }
    if ($failed.Count -gt 0) {
        throw ("M83-A regression FAILED in: {0}. Log: {1}" -f ($failed -join ', '), $script:StructuralConsoleLogPath)
    }

    Write-Host 'M83-A 4/4: reproducible performance sampling (no timing pass/fail threshold)'
    $output = Join-Path $root 'artifacts/benchmarks/m83'
    dotnet run --no-build --project tools/StructuralSolver2D.PerformanceM73/StructuralSolver2D.PerformanceM73.csproj `
        -c Release -- --m83 --repetitions 3 --output $output
    if ($LASTEXITCODE -ne 0) {
        throw "M83-A benchmark validation FAILED. Log: $script:StructuralConsoleLogPath"
    }
    foreach ($name in @('m83-scalability.csv', 'm83-scalability.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $output $name))) {
            throw "M83-A expected benchmark report missing: $name"
        }
    }
    Write-Host 'M83-A PASSED: all regressions and benchmark numerical checks.' -ForegroundColor Green
    Write-Host 'IMPORTANT: numbers describe local performance; M83 sparse solver remains conditional.'
    Write-Host "Benchmark reports: $output"
    Write-Host "Console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
