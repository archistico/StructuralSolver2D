# M66 numerical validation gate: ONLY C#/xUnit benchmarks; PowerShell orchestrates the run.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'M66 requires the .NET 8 SDK (dotnet not found).'
    }
    foreach ($relative in @(
        'src/StructuralSolver2D.Analysis/Truss2D/Nonlinear/NonlinearTruss2DNewmark.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Truss2D/NonlinearM66AlgebraicReference.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Truss2D/NonlinearValidationM66Tests.cs',
        'docs/structural/m66-nonlinear-validation-gate.md')) {
        if (-not (Test-Path (Join-Path $root $relative))) {
            throw "M66 source missing: $relative. Extract the FULL archive into a clean folder."
        }
    }

    Write-Host 'M66 1/3: Release build after mandatory per-project bin/obj cleanup'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M66 Release build failed.' }

    Write-Host 'M66 2/3: independent C# algebraic benchmarks and M63-M65 regression'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~NonlinearValidationM66Tests|FullyQualifiedName~NonlinearTruss2DNewmarkM65Tests|FullyQualifiedName~IncrementalElastoplasticTruss2DM64Tests|FullyQualifiedName~ElastoplasticTruss2DM63Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M66 focused nonlinear benchmark suite failed.' }

    Write-Host 'M66 3/3: full .NET regression across the entire solution'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M66 full regression failed.' }

    Write-Host 'M66 automated tests PASSED. This validates only the documented small-strain research model.'
    Write-Host 'Nonlinear public APIs, collapse analysis, large displacements and browser inspection are NOT accepted by this gate.'
}
finally {
    Pop-Location
}
