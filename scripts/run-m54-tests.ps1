# Run after extracting the M54 COMPLETE ZIP; requires the .NET 8 SDK on Windows.
$ErrorActionPreference = 'Stop'
# Mandatory clean preflight: remove stale bin/obj for every .NET project before any build/test.
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 8 SDK not found. Install it and rerun this script.'
}
Push-Location $root
try {
    Write-Host 'M54 1/3: Build complete solution'
    dotnet build --tl:off StructuralSolver2D.sln --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    Write-Host 'M54 2/3: Focused boundary condition reduction and dynamic assembly tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        --configuration Release --no-build `
        --filter 'FullyQualifiedName~Dynamics'
    if ($LASTEXITCODE -ne 0) { throw 'M54 focused dynamic tests failed.' }

    Write-Host 'M54 3/3: Complete solution regression tests'
    dotnet test --tl:off StructuralSolver2D.sln --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Complete regression suite failed.' }
    Write-Host 'M54 .NET gate passed.'
}
finally { Pop-Location }
