# M63: isolated small-strain elastoplastic Truss2D (research-only).
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'M63 requires the .NET 8 SDK (dotnet was not found).'
    }
    Write-Host 'M63 1/3: full Release build after mandatory bin/obj cleanup'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M63 Release build failed.' }

    Write-Host 'M63 2/3: isolated axial elastoplastic Truss2D material and element tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~ElastoplasticTruss2DM63Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M63 axial material/element benchmarks failed.' }

    Write-Host 'M63 3/3: complete solution regression suite'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M63 full regression suite failed.' }
    Write-Host 'M63 automated .NET checks PASSED. Single research-only element; no nonlinear structural solver enabled.'
}
finally { Pop-Location }
