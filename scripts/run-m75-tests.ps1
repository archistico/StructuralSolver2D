# M75: canonical JSON model contract, round-trip and input preflight (C#/.NET only).
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    Write-Host 'M75 1/4: clean Release compilation'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M75 Release build failed.' }

    Write-Host 'M75 2/4: focused JSON contract, compatibility and validation tests'
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~StructuralModelJsonContractM75Tests|FullyQualifiedName~StructuralModelJsonReaderTests|FullyQualifiedName~JsonInputRegressionTests' `
        --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M75 model contract gate FAILED.' }

    Write-Host 'M75 3/4: focused physical-unit conversion tests (C# only)'
    dotnet test --tl:off tests/StructuralSolver2D.Core.Tests/StructuralSolver2D.Core.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~StructuralUnitConversionsM75Tests' `
        --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M75 unit conversion gate FAILED.' }

    Write-Host 'M75 4/4: complete .NET solution regression (including M73)'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'M75 complete regression FAILED.' }

    Write-Host 'M75 tests PASSED. M75 contract is validated only for the cases covered by this suite.'
    Write-Host "Console log: $script:StructuralConsoleLogPath"
}
finally { Pop-Location }
