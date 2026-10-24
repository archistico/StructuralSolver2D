# Runs M51 verification and the full regression suite. Execute from any working directory.
$ErrorActionPreference = 'Stop'
# Mandatory clean preflight: remove stale bin/obj for every .NET project before any build/test.
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK not found. Install .NET 8 SDK and rerun this script.'
}
Push-Location $root
try {
    Write-Host 'Step 1/3: Build StructuralSolver2D'
    dotnet build --tl:off StructuralSolver2D.sln --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    Write-Host 'Step 2/3: M51 verification tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        --configuration Release --no-build `
        --filter 'FullyQualifiedName~Verification.StaticEnergyVerificationTests|FullyQualifiedName~Verification.ElementMatrixInvariantTests|FullyQualifiedName~Verification.ConstrainedStiffnessInspectorTests'
    if ($LASTEXITCODE -ne 0) { throw 'M51 verification tests failed.' }
    Write-Host 'Step 3/3: All solution tests'
    dotnet test --tl:off StructuralSolver2D.sln --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Full regression suite failed.' }
    Write-Host 'All .NET tests passed.'
}
finally {
    Pop-Location
}
