# M77 — first-order linear static elastic supports and support settlements.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M77 1/5: clean Release compilation'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M77 Release build FAILED.' }

    Write-Host 'M77 2/5: analytical FEM / spring / settlement gates (C# xUnit)'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~ElasticSupportSettlementM77Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M77 analysis gate FAILED.' }

    Write-Host 'M77 3/5: JSON contract and legacy M75/M76 gates'
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~ElasticSupportJsonM77Tests|FullyQualifiedName~SelfWeightJsonM76Tests|FullyQualifiedName~StructuralModelJsonContractM75Tests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M77 JSON gate FAILED.' }

    Write-Host 'M77 4/5: real CLI analysis on settlement / spring model'
    dotnet run --no-build -c Release --project src/StructuralSolver2D.Cli -- analyze docs/structural/fixtures/m77-elastic-support-settlement.json
    if ($LASTEXITCODE -ne 0) { throw 'M77 CLI analysis FAILED.' }

    Write-Host 'M77 5/5: full .NET 8 solution regression (includes M75 and M76)'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M77 full regression FAILED.' }
    Write-Host 'M77 tests PASSED. The M75/M76/M77 Windows validation gate has completed.'
    Write-Host "Console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
