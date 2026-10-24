# M85-B: static end-to-end verification of the FEM public API, CLI and exports.
# No changes to the production FEM kernel. Optional longer M83 benchmark sampling.
param([switch]$WithBenchmark)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'repair-m82-legacy-source.ps1')
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M85-B 1/3: clean Release build'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw "M85-B build FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M85-B 2/3: cross-layer results and independent numerical gates'
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~IntegratedResultsM85BTests' `
        --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "M85-B integrated validation FAILED. Log: $script:StructuralConsoleLogPath" }

    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~IndependentStaticInvariantsM85Tests|FullyQualifiedName~Frame2DGlobalSecondOrderM82Tests|FullyQualifiedName~Frame2DScalabilityM83Tests' `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw "M85-B numerical regression FAILED. Log: $script:StructuralConsoleLogPath" }

    Write-Host 'M85-B 3/3: all existing test projects'
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
    if ($failed.Count -gt 0) { throw "M85-B regression FAILED: $($failed -join ', '). Log: $script:StructuralConsoleLogPath" }

    if ($WithBenchmark) {
        # Separate folder: do not overwrite the user's original M83 measurements.
        $output = Join-Path $root 'artifacts/benchmarks/m85b'
        Write-Host 'Optional: 20-repetition M83 benchmark (diagnostic only, no timing gate)'
        dotnet run --no-build --project tools/StructuralSolver2D.PerformanceM73/StructuralSolver2D.PerformanceM73.csproj `
            -c Release -- --m83 --repetitions 20 --output $output
        if ($LASTEXITCODE -ne 0) { throw "M85-B optional benchmark FAILED. Log: $script:StructuralConsoleLogPath" }
        foreach ($name in @('m83-scalability.csv', 'm83-scalability.json')) {
            if (-not (Test-Path -LiteralPath (Join-Path $output $name))) { throw "Missing benchmark file: $name" }
        }
        Write-Host "Updated benchmark samples: $output"
    }

    Write-Host 'M85-B PASSED: integrated exports, numerical invariants and complete regression.' -ForegroundColor Green
    Write-Host 'Scope: first-order static elastic 2D results; NOT certification or normative design checks.'
    Write-Host "Full console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
