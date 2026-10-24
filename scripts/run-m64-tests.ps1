# M64: small-strain incremental elastoplastic Truss2D, C#-only analytical benchmarks.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'M64 requires the .NET 8 SDK (dotnet not found).'
    }
    Write-Host 'M64 1/3: clean Release build (bin/obj removed at start)'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M64 Release build failed.' }

    Write-Host 'M64 2/3: C# analytical equilibrium, material-state, constraints and rollback tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~IncrementalElastoplasticTruss2DM64Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M64 incrementally assembled truss tests failed.' }

    Write-Host 'M64 3/3: full .NET 8 regression suite'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M64 complete .NET regression failed.' }

    Write-Host 'M64 automated C# tests PASSED. Internal load-controlled research solver only.'
    Write-Host 'No Python runtime or Python benchmark is required by this runner.'
}
finally { Pop-Location }
