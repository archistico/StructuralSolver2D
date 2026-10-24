# G8B demo: build and execute four comparable house models, then generate reports.
# This script does not claim member or NTC-combination compliance.
[CmdletBinding()]
param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Set-Location -LiteralPath $root
$models = @(
    'g8b-b_riferimento-senza-controventi.json',
    'g8b-b_riferimento-controventato.json',
    'g8b-a_leggero-controventato.json',
    'g8b-c_robusto-controventato.json'
)
$combinations = @('ULS_GRAVITY', 'ULS_WIND_POS', 'ULS_WIND_NEG', 'SLS_RARE')
$cli = 'src/StructuralSolver2D.Cli'
$output = 'reports/g8b-house'
New-Item -ItemType Directory -Force -Path $output | Out-Null
function Invoke-Dotnet([string[]]$CmdArgs) {
    & dotnet @CmdArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet command failed (exit $LASTEXITCODE): $($CmdArgs -join ' ')" }
}
if (-not $SkipBuild) { Invoke-Dotnet @('build', 'StructuralSolver2D.sln', '-c', 'Release', '--tl:off') }
$run = @('run', '--no-build', '-c', 'Release', '--project', $cli, '--')
foreach ($file in $models) {
    $model = "examples/mixed/g8b-house/$file"
    $stem = [IO.Path]::GetFileNameWithoutExtension($file)
    foreach ($comb in $combinations) {
        $report = "$output/$stem--$comb.md"
        Write-Host "G8B REPORT $stem / $comb" -ForegroundColor Cyan
        Invoke-Dotnet ($run + @('report', $model, $report, $comb))
    }
}
foreach ($stem in @('g8b-b_riferimento-senza-controventi', 'g8b-b_riferimento-controventato')) {
    $file = "examples/mixed/g8b-house/$stem.json"
    $viewer = "$output/$stem--ULS_WIND_POS.html"
    Write-Host "G8B VIEWER $stem / ULS_WIND_POS" -ForegroundColor Cyan
    Invoke-Dotnet ($run + @('export-viewer', $file, $viewer, 'ULS_WIND_POS'))
}
Write-Host "G8B demo completed: 16 reports and 2 HTML viewers in $output" -ForegroundColor Green
Write-Host 'No conformity of elements, actions or combinations is implied.'
