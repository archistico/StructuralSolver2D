# M65: research-only nonlinear Truss2D time response; all numerical benchmarks are C#/xUnit.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'M65 requires the .NET 8 SDK (dotnet not found).'
    }
    $required = @(
        'src/StructuralSolver2D.Analysis/Truss2D/Nonlinear/NonlinearTruss2DNewmark.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Truss2D/NonlinearTruss2DNewmarkM65Tests.cs',
        'docs/structural/m65-nonlinear-truss-dynamics.md')
    foreach ($f in $required) {
        if (-not (Test-Path (Join-Path $root $f))) {
            throw "M65 source missing ($f). Extract the FULL updated ZIP into a clean folder."
        }
    }
    Write-Host 'M65 1/3: Release build after mandatory bin/obj cleanup'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M65 Release build failed.' }

    Write-Host 'M65 2/3: focused C# nonlinear time-history and predecessor M63/M64 regressions'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~NonlinearTruss2DNewmarkM65Tests|FullyQualifiedName~IncrementalElastoplasticTruss2DM64Tests|FullyQualifiedName~ElastoplasticTruss2DM63Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M65 nonlinear numeric tests failed.' }

    Write-Host 'M65 3/3: full regression suite (all .NET solution projects)'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M65 full regression failed.' }
    Write-Host 'M65 automated C# checks PASSED. Research-only nonlinear Truss2D integrator; no public API.'
}
finally { Pop-Location }
