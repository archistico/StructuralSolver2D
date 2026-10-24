# M76 - explicit self weight on mixed 2D static structures, independent C#/xUnit benchmarks.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M76 1/5: clean Release compilation'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M76 Release build FAILED.' }

    Write-Host 'M76 2/5: self-weight FEM analytic and equivalence benchmarks (C#)'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~SelfWeightM76Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M76 numerical gate FAILED.' }

    Write-Host 'M76 3/5: model preflight, input round-trip and compatibility'
    dotnet test --tl:off tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~StructuralModelLoadValidatorTests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M76 core gate FAILED.' }
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~SelfWeightJsonM76Tests|FullyQualifiedName~StructuralModelJsonContractM75Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M76 JSON gate FAILED.' }

    Write-Host 'M76 4/5: real CLI analysis from the M76 JSON example'
    dotnet run --no-build -c Release --project src/StructuralSolver2D.Cli -- analyze docs/structural/fixtures/m76-self-weight-beam.json
    if ($LASTEXITCODE -ne 0) { throw 'M76 CLI analysis FAILED.' }

    Write-Host 'M76 5/5: full solution regression, including M75'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M76 regression FAILED.' }
    Write-Host 'M76 tests PASSED. Numerical C# tests and regressions completed.'
    Write-Host "Console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
