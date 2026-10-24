# M55: build, focused internal modal eigenproblem tests, full regression suite.
# Requires .NET SDK 8.x; execute from any PowerShell working directory.
$ErrorActionPreference = 'Stop'
# Mandatory clean preflight: remove stale bin/obj for every .NET project before any build/test.
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'NET SDK not found. Install .NET SDK 8 and run again.'
}
Push-Location $root
try {
    $sdk = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to query the .NET SDK version.' }
    Write-Host "Default .NET SDK: $sdk (project targets net8.0)"
    Write-Host 'M55 1/3: build entire solution in Release configuration'
    dotnet build --tl:off StructuralSolver2D.sln --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'M55 build failed.' }
    Write-Host 'M55 2/3: focused modal eigenanalysis and M52-M54 dynamics regressions'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        --configuration Release --no-build --filter 'FullyQualifiedName~Dynamics'
    if ($LASTEXITCODE -ne 0) { throw 'M55 focused dynamics tests failed.' }
    Write-Host 'M55 3/3: complete solution regression suite'
    dotnet test --tl:off StructuralSolver2D.sln --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M55 full regression suite failed.' }
    Write-Host 'M55 .NET gate passed (M56 independent benchmark gate is still required).'
}
finally { Pop-Location }
