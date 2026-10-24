# M56: complete solution build, analytical modal benchmarks, prior dynamic tests,
# and full static/reporting/CLI regressions. Requires .NET SDK 8.x.
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

    Write-Host 'M56 1/4: Release build of complete solution'
    dotnet build --tl:off StructuralSolver2D.sln --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'M56 build failed.' }

    Write-Host 'M56 2/4: independent analytic modal benchmark + convergence tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        --configuration Release --no-build --filter 'FullyQualifiedName~ModalBenchmarkM56Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M56 analytic benchmark tests failed.' }

    Write-Host 'M56 3/4: full dynamics M52–M56 regression tests'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        --configuration Release --no-build --filter 'FullyQualifiedName~Dynamics'
    if ($LASTEXITCODE -ne 0) { throw 'M56 dynamics regression tests failed.' }

    Write-Host 'M56 4/4: complete solution regression suite'
    dotnet test --tl:off StructuralSolver2D.sln --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M56 full regression suite failed.' }

    Write-Host 'Independent reference values are embedded in the C# benchmark suite; no Python is run.'
    Write-Host 'M56 .NET regression gate passed. M57/public modal capability remains separate.'
}
finally { Pop-Location }
