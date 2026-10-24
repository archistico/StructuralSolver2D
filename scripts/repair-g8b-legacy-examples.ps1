# G8B FIX2: migrate only two obsolete JSON *reference* files left in examples/
# after extracting G8B FIX1 over an older G8B tree.
# Keeps original bytes in a separate backup folder. Never touches valid models.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$folder = Join-Path $root 'examples/mixed/g8b-house'
$names = @(
    'g8b-controlli-analitici.json',
    'g8b-risultati-elastici-indipendenti.json'
)
$backupFolder = Join-Path $root '.g8b-legacy-backup'
$moved = 0
foreach ($name in $names) {
    $source = Join-Path $folder $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { continue }
    $item = Get-Item -LiteralPath $source -Force -ErrorAction Stop
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "G8B legacy repair: refusing to move a linked file: $source"
    }
    # The old files have a 'models' reference-data collection, not FEM nodes/members.
    # Refuse to move any different content, even if the filename happens to match.
    try {
        $json = Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "G8B legacy repair: cannot inspect JSON '$source'. $_"
    }
    if ($null -eq $json.PSObject.Properties['models'] -or
        $null -ne $json.PSObject.Properties['nodes'] -or
        $null -ne $json.PSObject.Properties['members']) {
        throw "G8B legacy repair: unexpected JSON structure, refusing to move: $source"
    }
    if (-not (Test-Path -LiteralPath $backupFolder)) {
        New-Item -ItemType Directory -Path $backupFolder -ErrorAction Stop | Out-Null
    }
    $backupItem = Get-Item -LiteralPath $backupFolder -Force -ErrorAction Stop
    if (-not $backupItem.PSIsContainer -or
        ($backupItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "G8B legacy repair: invalid backup folder: $backupFolder"
    }
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fffffff'
    $destination = Join-Path $backupFolder "$name.$stamp.bak"
    if (Test-Path -LiteralPath $destination) {
        throw "G8B legacy repair: backup collision: $destination"
    }
    Move-Item -LiteralPath $source -Destination $destination -ErrorAction Stop
    Write-Warning "Obsolete G8B reference JSON moved out of examples: $name. Backup: $destination"
    $moved++
}
Write-Host "G8B legacy examples repair: moved $moved obsolete file(s) outside examples/."
