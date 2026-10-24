# Verify exact G6 file SHA-256 hashes before making local modifications.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$manifest = Join-Path $root 'RELEASE_SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw "Missing manifest: $manifest" }
$errors = 0
$count = 0
foreach ($line in (Get-Content -LiteralPath $manifest -Encoding UTF8)) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
    if ($line -notmatch '^([a-fA-F0-9]{64})  (.+)$') { throw "Malformed checksum record: $line" }
    $expected = $Matches[1]; $relative = $Matches[2]
    if ($relative.StartsWith('/') -or $relative.Contains('..') -or $relative.Contains('\')) {
        throw "Unsafe manifest path: $relative"
    }
    $path = Join-Path $root ($relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $count++
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Write-Host "MISSING: $relative" -ForegroundColor Red; $errors++; continue
    }
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($actual -ine $expected) { Write-Host "MISMATCH: $relative" -ForegroundColor Red; $errors++ }
}
if ($errors -gt 0) { throw "G7 manifest FAILED: $errors mismatches over $count files." }
Write-Host "G7 manifest PASSED: $count checked files." -ForegroundColor Green
