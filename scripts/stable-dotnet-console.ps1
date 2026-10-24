# Dot-source this AFTER clean-project-build.ps1 from each run-m*-tests.ps1.
# Capture dotnet stdout/stderr as text rather than allowing progress/cursor commands
# to write directly to the interactive PowerShell terminal. Requires PowerShell 5.1+.
$script:StructuralDotnetExecutable = (Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
$script:StructuralConsoleLogDirectory = Join-Path ([System.IO.Path]::GetTempPath()) 'StructuralSolver2D-test-logs'
if (-not (Test-Path -LiteralPath $script:StructuralConsoleLogDirectory)) {
    New-Item -ItemType Directory -Path $script:StructuralConsoleLogDirectory -Force | Out-Null
}
$script:StructuralConsoleLogPath = Join-Path $script:StructuralConsoleLogDirectory (
    'dotnet-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + $PID + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8) + '.log'
)
# Keep the caller's environment intact: CI/TERM/NO_COLOR/DOTNET_NOLOGO are
# applied only while the native dotnet process runs, then restored in finally.
$ProgressPreference = 'SilentlyContinue'
Write-Host "Plain-text .NET console enabled; log: $script:StructuralConsoleLogPath"

# WARNING for run-m*.ps1 authors: avoid abbreviated '-v:minimal' / '-v:quiet'
# on a PowerShell FUNCTION named dotnet: PS may bind -v as the common
# parameter -Verbose and pass "minimal" as an extra solution filename.
# Use default verbosity or the unambiguous '--verbosity minimal' syntax.
# Existing scripts invoke 'dotnet' normally and inspect $LASTEXITCODE.
# This function intentionally shadows only those invocations in the current script.
function dotnet {
    $nativeArguments = @($args)
    $previousErrorAction = $ErrorActionPreference
    $writer = $null
    $exitCode = 1
    # Windows PowerShell 5.1 otherwise may decode .NET UTF-8 output using the
    # legacy OEM code page (e.g. "È" becomes "├¿"). Restore the host encoding.
    $previousConsoleOutputEncoding = $null
    $consoleOutputEncodingChanged = $false
    $temporaryEnvironment = @{
        DOTNET_NOLOGO = '1'
        NO_COLOR = '1'
        CI = 'true'
        TERM = 'dumb'
    }
    $previousEnvironment = @{}
    try {
        foreach ($name in $temporaryEnvironment.Keys) {
            $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
            [Environment]::SetEnvironmentVariable($name, $temporaryEnvironment[$name], 'Process')
        }
        try {
            $previousConsoleOutputEncoding = [Console]::OutputEncoding
            [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
            $consoleOutputEncodingChanged = $true
        }
        catch {
            # Some non-interactive hosts do not expose a writable console encoding.
            # Continue with the host default rather than preventing the build.
        }
        $writer = [System.IO.StreamWriter]::new(
            $script:StructuralConsoleLogPath, $true, [System.Text.UTF8Encoding]::new($false))
        $writer.AutoFlush = $true
        # Windows PowerShell 5.1 can turn redirected native stderr into ErrorRecords.
        # Preserve the native process exit status instead of treating stderr text as an exception.
        $ErrorActionPreference = 'Continue'
        & $script:StructuralDotnetExecutable @nativeArguments 2>&1 | ForEach-Object {
            $line = [string]$_
            # Strip CSI terminal formatting/cursor sequences, OSC hyperlinks and other
            # nonprinting control characters, while retaining readable warnings/errors.
            $line = [regex]::Replace($line, "\x1b\[[0-?]*[ -/]*[@-~]", '')
            $line = [regex]::Replace($line, "\x1b\][^\x07]*(?:\x07|\x1b\\)", '')
            $line = [regex]::Replace($line, '[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]', '')
            foreach ($part in ($line -split '[\r\n]+')) {
                if ([string]::IsNullOrWhiteSpace($part)) { continue }
                $writer.WriteLine($part)
                Write-Output $part
            }
        }
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
        if ($consoleOutputEncodingChanged) {
            try { [Console]::OutputEncoding = $previousConsoleOutputEncoding }
            catch { }  # Console may have been detached during execution.
        }
        if ($null -ne $writer) { $writer.Dispose() }
        foreach ($name in $previousEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
        }
        $global:LASTEXITCODE = $exitCode
    }
}
