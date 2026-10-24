# Builds all projects, runs focused M52 contract tests, then runs the entire .NET solution suite.
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
    Write-Host 'M52 1/3: Build complete solution'
    dotnet build --tl:off StructuralSolver2D.sln --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    Write-Host 'M52 2/3: Focused Core, Analysis and CLI dynamic contract regressions'
    dotnet test --tl:off tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj `
        --configuration Release --no-build --filter 'FullyQualifiedName~Dynamics'
    if ($LASTEXITCODE -ne 0) { throw 'Core dynamic-unit and validation tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        --configuration Release --no-build --filter 'FullyQualifiedName~Dynamics'
    if ($LASTEXITCODE -ne 0) { throw 'Analysis dynamic-contract tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj `
        --configuration Release --no-build --filter 'FullyQualifiedName~DynamicsJsonContractTests'
    if ($LASTEXITCODE -ne 0) { throw 'CLI JSON regression tests failed.' }

    Write-Host 'M52 3/3: Entire solution regression suite'
    dotnet test --tl:off StructuralSolver2D.sln --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Complete regression suite failed.' }
    Write-Host 'M52 .NET gate passed.'
}
finally {
    Pop-Location
}
