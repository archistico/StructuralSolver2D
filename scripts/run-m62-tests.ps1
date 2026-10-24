# M62 research prototype validation; no nonlinear frame or seismic capability is enabled.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'M62 requires the .NET 8 SDK (dotnet was not found).'
    }
    Write-Host 'M62 1/4: full Release build after mandatory bin/obj cleanup'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M62 Release build failed.' }

    Write-Host 'M62 2/4: isolated nonlinear SDOF benchmark tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~NonlinearSdofM62Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M62 analytical/rollback tests failed.' }

    Write-Host 'M62 3/4: full legacy and new regression suite'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M62 full regression suite failed.' }

    Write-Host 'M62 4/4: reference values already covered by independent C# xUnit tests (no Python dependency)'
    Write-Host 'M62 automated checks PASSED. This remains an isolated research-only SDOF prototype.'
}
finally { Pop-Location }
