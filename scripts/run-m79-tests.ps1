# M79 — analytical global Frame2D envelopes over explicit static cases/combinations.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M79 1/4: clean Release compilation'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M79 Release build FAILED.' }

    Write-Host 'M79 2/4: C# exact envelope + M78 reference benchmarks'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~Frame2DStaticForceEnvelopeM79Tests|FullyQualifiedName~Frame2DExactExtremaM78Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M79 analysis/benchmark gate FAILED.' }

    Write-Host 'M79 3/4: C# CSV/Markdown reporting tests'
    dotnet test --tl:off tests/StructuralSolver2D.Reporting.Tests/StructuralSolver2D.Reporting.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~StaticForceEnvelopeReportM79Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M79 reporting tests FAILED.' }

    Write-Host 'M79 4/4: full solution regression including M75-M78'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M79 full regression FAILED.' }
    Write-Host 'M79 tests PASSED. Gate includes pending M75-M78 checks.'
    Write-Host "Console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
