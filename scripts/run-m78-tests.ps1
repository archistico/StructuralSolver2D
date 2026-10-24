# M78 - exact piecewise N/V/M section-force extrema (no change to the FEM solver).
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M78 1/4: clean Release compilation'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M78 Release build FAILED.' }

    Write-Host 'M78 2/4: analytical N/V/M extrema benchmarks (C# xUnit)'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~Frame2DExactExtremaM78Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M78 analytical extrema gate FAILED.' }

    Write-Host 'M78 3/4: existing characteristic points and legacy M75-M77 gates'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~Frame2DInternalForceSamplerTests|FullyQualifiedName~Frame2DInternalForceCharacteristicFinderTests|FullyQualifiedName~SelfWeightM76Tests|FullyQualifiedName~ElasticSupportSettlementM77Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M78 legacy post-processing gate FAILED.' }

    Write-Host 'M78 4/4: full solution regression (includes M75, M76 and M77)'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M78 full regression FAILED.' }
    Write-Host 'M78 tests PASSED. Gate also covers M75/M76/M77.'
    Write-Host "Console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
