# M72 experimental Frame2D end-hinges (C#/xUnit only).
# ALWAYS remove local bin/obj first. M71 is an UNVALIDATED prerequisite.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    foreach ($relative in @(
        'src/StructuralSolver2D.Analysis/Frame2D/Nonlinear/BilinearRotationalHinge.cs',
        'src/StructuralSolver2D.Analysis/Frame2D/Nonlinear/ElastoplasticFrame2DElement.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Frame2D/M71/PlasticHingeFeasibilityM71Tests.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Frame2D/M72/ElastoplasticFrame2DM72Tests.cs',
        'docs/structural/m72-first-concentrated-plastic-frame-element.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative))) {
            throw "M72 prerequisite missing: $relative"
        }
    }

    Write-Host 'M72 1/3: full clean Release compilation'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M72 Release build failed.' }

    Write-Host 'M72 2/3: M71 reference gate AND M72 element benchmarks (C#)'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~PlasticHingeFeasibilityM71Tests|FullyQualifiedName~ElastoplasticFrame2DM72Tests' `
        --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M71/M72 numerical gate FAILED; M72 not validated.' }

    Write-Host 'M72 3/3: complete regression of all projects'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M72 complete regression FAILED.' }

    Write-Host 'M72 automated checks PASSED (research-only element; no public nonlinear Frame2D solver).'
    Write-Host "Console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
