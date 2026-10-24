# M67 — mandatory per-project bin/obj cleanup; C#/.NET tests only.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'clean-project-build.ps1')
# Route all dotnet build/test/run output through a plain-text stream (no progress redraw).
. (Join-Path $PSScriptRoot 'stable-dotnet-console.ps1')

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'M67 requires .NET 8 SDK. dotnet not found.'
    }
    foreach ($relative in @(
        'src/StructuralSolver2D.Analysis/PublicApi/StructuralDynamicForceRecoveryService.cs',
        'src/StructuralSolver2D.Analysis/Truss2D/Nonlinear/NonlinearTruss2DForceEnvelopesM67.cs',
        'src/StructuralSolver2D.Reporting/Transient/DynamicForceReportExporter.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Dynamics/M67/DynamicForceRecoveryM67Tests.cs',
        'tests/StructuralSolver2D.Analysis.Tests/Dynamics/M67/NonlinearEnvelopeM67Tests.cs',
        'tests/StructuralSolver2D.Cli.Tests/DynamicForceCliM67Tests.cs',
        'docs/structural/m67-dynamic-forces-and-reactions.md')) {
        if (-not (Test-Path (Join-Path $root $relative))) {
            throw "M67 file missing: $relative. Unpack FULL ZIP in a clean directory."
        }
    }
    Write-Host 'M67 1/3: Release build after mandatory cleanup'
    dotnet build --tl:off StructuralSolver2D.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'M67 Release build failed.' }

    Write-Host 'M67 2/3: focused C# dynamic force and nonlinear acceptance checks'
    dotnet test --tl:off tests/StructuralSolver2D.Analysis.Tests/StructuralSolver2D.Analysis.Tests.csproj `
        -c Release --no-build `
        --filter 'FullyQualifiedName~DynamicForceRecoveryM67Tests|FullyQualifiedName~NonlinearEnvelopeM67Tests|FullyQualifiedName~NonlinearValidationM66Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M67 analytical and nonlinear focused tests failed.' }
    dotnet test --tl:off tests/StructuralSolver2D.Cli.Tests/StructuralSolver2D.Cli.Tests.csproj `
        -c Release --no-build --filter 'FullyQualifiedName~DynamicForceCliM67Tests'
    if ($LASTEXITCODE -ne 0) { throw 'M67 CLI end-to-end tests failed.' }

    Write-Host 'M67 3/3: full .NET regression'
    dotnet test --tl:off StructuralSolver2D.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'M67 full regression failed.' }
    Write-Host 'M67 automated C# checks PASSED. Distributed dynamic N/V/M section diagrams and browser QA remain deferred.'
}
finally { Pop-Location }
