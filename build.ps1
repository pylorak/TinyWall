<#
    Build script for TinyWall releases.

    Usage:
        build.ps1 [--skip-update] [--skip-sign]

    --skip-update : Skip creating update files at the end.
    --skip-sign   : Skip all code-signing steps. Requires --skip-update.
#>

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

# Directory this script lives in; used to anchor all relative paths.
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# ---------------------------------------------------------------------------
# Parse command-line switches.
# ---------------------------------------------------------------------------
$SkipUpdate = $false
$SkipSign   = $false
foreach ($a in $args) {
    switch ($a) {
        '--skip-update' { $SkipUpdate = $true; break }
        '--skip-sign'   { $SkipSign   = $true; break }
        default {
            [Console]::Error.WriteLine("Unrecognized argument: $a")
            exit 1
        }
    }
}

if ($SkipSign -and -not $SkipUpdate) {
    [Console]::Error.WriteLine('--skip-sign can only be used together with --skip-update.')
    exit 1
}

# ---------------------------------------------------------------------------
# Read build-config.ini
# ---------------------------------------------------------------------------
$ConfigPath = Join-Path $ScriptDir 'build-config.ini'
if (-not (Test-Path -LiteralPath $ConfigPath)) {
    [Console]::Error.WriteLine("Configuration file not found: $ConfigPath")
    exit 1
}

# ReadAllLines handles both CRLF and LF line endings, and strips BOM automatically.
$ConfigLines = [System.IO.File]::ReadAllLines($ConfigPath, [System.Text.Encoding]::UTF8)

# Manual parse: comments start with '#' or ';', values are unquoted,
# keys are case-insensitive, surrounding whitespace is trimmed.
$Config = @{}
foreach ($line in $ConfigLines) {
    $t = $line.Trim()
    if ($t.Length -eq 0) { continue }
    $first = $t[0]
    if ($first -eq '#' -or $first -eq ';') { continue }
    $eq = $t.IndexOf('=')
    if ($eq -lt 0) { continue }
    $k = $t.Substring(0, $eq).Trim()
    $v = $t.Substring($eq + 1).Trim()
    if ($k.Length -gt 0) { $Config[$k] = $v }
}

function Require-ConfigString {
    param([Parameter(Mandatory)][string]$Key)
    $v = $Config[$Key]
    if ($null -eq $v -or $v.Length -eq 0) {
        [Console]::Error.WriteLine("Missing or empty required value '$Key' in build-config.ini.")
        exit 1
    }
    return $v
}

# ---------------------------------------------------------------------------
# Convert a possibly-relative path to an absolute one. The target need not exist.
# ---------------------------------------------------------------------------
function To-Absolute {
    param([Parameter(Mandatory)][string]$Path)
    if (-not [System.IO.Path]::IsPathRooted($Path)) {
        $Path = Join-Path $ScriptDir $Path
    }
    return [System.IO.Path]::GetFullPath($Path)
}

# ---------------------------------------------------------------------------
# Ensure a set of directories exist, creating them if necessary.
# ---------------------------------------------------------------------------
function Ensure-Directories {
    param([Parameter(Mandatory)][string[]]$Paths)
    foreach ($p in $Paths) {
        New-Item -ItemType Directory -Path $p -Force | Out-Null
    }
}

# ---------------------------------------------------------------------------
# Run an external command after echoing the call. Only show its stderr.
# ---------------------------------------------------------------------------
function Invoke-Tool {
    param([Parameter(Mandatory)][string]$Command)
    Write-Host "==> $Command"

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = 'cmd.exe'
    $psi.Arguments = '/c "' + $Command + '"'
    $psi.WorkingDirectory = $ScriptDir
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $false

    $proc = [System.Diagnostics.Process]::Start($psi)
    try {
        # Drain stdout (and discard) so the pipe buffer cannot fill and deadlock.
        $null = $proc.StandardOutput.ReadToEnd()
        $proc.WaitForExit()
        $code = $proc.ExitCode
    } finally {
        $proc.Dispose()
    }

    if ($code -ne 0) {
        [Console]::Error.WriteLine("Command failed with exit code $code. Aborting.")
        exit $code
    }
}

# ---------------------------------------------------------------------------
# Locate MSBuild via vswhere, falling back to PATH.
# ---------------------------------------------------------------------------
function Find-MSBuild {
    $pf86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
    if ($null -ne $pf86 -and $pf86.Length -gt 0) {
        $vswhere = Join-Path $pf86 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path -LiteralPath $vswhere) {
            $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe'
            if ($null -ne $found) {
                foreach ($line in $found) {
                    $p = "$line".Trim()
                    if ($p.Length -gt 0 -and (Test-Path -LiteralPath $p)) {
                        return $p
                    }
                }
            }
        }
    }
    $gmc = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($null -ne $gmc) { return $gmc.Source }
    return $null
}

# ===========================================================================
# Build procedure
# ===========================================================================

$StagingFolder = To-Absolute (Require-ConfigString 'staging-folder')
if (-not $SkipSign) {
    $CertName     = Require-ConfigString 'certificate'
    $SignToolPath = To-Absolute (Require-ConfigString 'signtool-path')
    $TimestampUrl = Require-ConfigString 'timestamp-url'
}

# ---------------------------------------------------------------------------
# Step 1: Compile the application (AnyCPU, Release).
# ---------------------------------------------------------------------------
Write-Host '== Step 1: Compiling TinyWall (AnyCPU Release) ==' -ForegroundColor Cyan

$MSBuild = Find-MSBuild
if ($null -eq $MSBuild -or $MSBuild.Length -eq 0) {
    [Console]::Error.WriteLine('Could not locate MSBuild. Install Visual Studio / Build Tools, or run from a Developer Command Prompt.')
    exit 1
}
$MSBuild = To-Absolute $MSBuild

$TinyWallProject = To-Absolute 'TinyWall\TinyWall.csproj'
Invoke-Tool ('"{0}" "{1}" /p:Configuration=Release /p:Platform=AnyCPU /t:Restore' -f $MSBuild, $TinyWallProject)
Invoke-Tool ('"{0}" "{1}" /p:Configuration=Release /p:Platform=AnyCPU /t:Rebuild' -f $MSBuild, $TinyWallProject)

$TinyWallExe = To-Absolute 'TinyWall\bin\Release\TinyWall.exe'
$BinRelease  = To-Absolute 'TinyWall\bin\Release'
if (-not (Test-Path -LiteralPath $TinyWallExe)) {
    [Console]::Error.WriteLine("Expected build output not found: $TinyWallExe")
    exit 1
}

# ---------------------------------------------------------------------------
# Ensure staging folder structure exists.
# ---------------------------------------------------------------------------
$StagingProgramFiles  = Join-Path $StagingFolder 'ProgramFiles\TinyWall'
$StagingCommonAppData = Join-Path $StagingFolder 'CommonAppData\TinyWall'
$StagingTmp           = Join-Path $StagingFolder 'tmp'
Ensure-Directories @($StagingFolder, $StagingProgramFiles, $StagingCommonAppData, $StagingTmp)

# ---------------------------------------------------------------------------
# Step 2: Verify language resources are already optimized.
#   resx-optimizer /compare returns non-zero when the source resx differs from
#   the optimized form, which aborts the build.
# ---------------------------------------------------------------------------
Write-Host '== Step 2: Verifying optimized language resources ==' -ForegroundColor Cyan

$ResxWinformsOut  = Join-Path $StagingTmp 'resx-winforms'
$ResxResourcesOut = Join-Path $StagingTmp 'resx-resources'
Ensure-Directories @($ResxWinformsOut, $ResxResourcesOut)

$ResxWinformsSrc  = To-Absolute 'TinyWall'
$ResxResourcesSrc = To-Absolute 'TinyWall\Resources'

Invoke-Tool ('"{0}" resx-optimizer /compare /resource-dir "{1}" /output-folder "{2}"' -f $TinyWallExe, $ResxWinformsSrc, $ResxWinformsOut)
Invoke-Tool ('"{0}" resx-optimizer /compare /resource-dir "{1}" /output-folder "{2}"' -f $TinyWallExe, $ResxResourcesSrc, $ResxResourcesOut)

# ---------------------------------------------------------------------------
# Step 3: Create the application database (profiles.json).
# ---------------------------------------------------------------------------
Write-Host '== Step 3: Creating profiles.json ==' -ForegroundColor Cyan

$DatabaseSrc = To-Absolute 'TinyWall\Database'
Invoke-Tool ('"{0}" database-creator /source-folder "{1}" /output-folder "{2}"' -f $TinyWallExe, $DatabaseSrc, $StagingTmp)

$ProfilesJson = Join-Path $StagingTmp 'profiles.json'
if (-not (Test-Path -LiteralPath $ProfilesJson)) {
    [Console]::Error.WriteLine("Expected database output not found: $ProfilesJson")
    exit 1
}

# ---------------------------------------------------------------------------
# Step 4: Populate the staging directory.
# ---------------------------------------------------------------------------
Write-Host '== Step 4: Populating staging directory ==' -ForegroundColor Cyan

$SrcHosts      = To-Absolute 'TinyWall\Resources\hosts'
$SrcDocDir     = To-Absolute 'TinyWall\doc'
$SrcLicenseRtf = To-Absolute 'MsiSetup\License.rtf'
$SrcIco        = To-Absolute 'TinyWall\Resources\img\TinyWall.ico'

# 4.1  hosts -> hosts.bck
Invoke-Tool ('echo F | xcopy /Y "{0}" "{1}"' -f $SrcHosts, (Join-Path $StagingCommonAppData 'hosts.bck'))

# 4.2  profiles.json -> CommonAppData\TinyWall\
Invoke-Tool ('xcopy /Y /I "{0}" "{1}\"' -f $ProfilesJson, $StagingCommonAppData)

# 4.3  TinyWall\doc -> ProgramFiles\TinyWall\
Invoke-Tool ('xcopy /Y /I /E "{0}" "{1}\doc\"' -f $SrcDocDir, $StagingProgramFiles)

# 4.4  License.rtf -> ProgramFiles\TinyWall\
Invoke-Tool ('xcopy /Y /I "{0}" "{1}\"' -f $SrcLicenseRtf, $StagingProgramFiles)

# 4.5  TinyWall.ico -> ProgramFiles\TinyWall\
Invoke-Tool ('xcopy /Y /I "{0}" "{1}\"' -f $SrcIco, $StagingProgramFiles)

# 4.6  bin\Release\* -> ProgramFiles\TinyWall\
Invoke-Tool ('xcopy /Y /I /E "{0}\*" "{1}\"' -f $BinRelease, $StagingProgramFiles)

# ---------------------------------------------------------------------------
# Step 5: Sign all staged files.
# ---------------------------------------------------------------------------
if (-not $SkipSign) {
    Write-Host '== Step 5: Signing staged files ==' -ForegroundColor Cyan
    Invoke-Tool ('"{0}" batch-signer /certificate-name "{1}" /sign-dir "{2}" /signtool-path "{3}" /timestamp-url "{4}"' -f $TinyWallExe, $CertName, $StagingFolder, $SignToolPath, $TimestampUrl)
}

# ---------------------------------------------------------------------------
# Step 6: Build the MSIs.
# ---------------------------------------------------------------------------
Write-Host '== Step 6: Building MSI setups ==' -ForegroundColor Cyan

$MsiProject = To-Absolute 'MsiSetup\MsiSetup.wixproj'
Invoke-Tool ('"{0}" "{1}" /p:Configuration=Release /p:Platform=x86   /t:Build' -f $MSBuild, $MsiProject)
Invoke-Tool ('"{0}" "{1}" /p:Configuration=Release /p:Platform=arm64 /t:Build' -f $MSBuild, $MsiProject)

# ---------------------------------------------------------------------------
# Step 7: Sign the built MSIs.
# ---------------------------------------------------------------------------
if (-not $SkipSign) {
    Write-Host '== Step 7: Signing MSI files ==' -ForegroundColor Cyan
    $MsiBin = To-Absolute 'MsiSetup\bin\Release'
    Invoke-Tool ('"{0}" batch-signer /certificate-name "{1}" /sign-dir "{2}" /signtool-path "{3}" /timestamp-url "{4}"' -f $TinyWallExe, $CertName, $MsiBin, $SignToolPath, $TimestampUrl)
}

# ---------------------------------------------------------------------------
# Step 8: Create update files for the built-in updater.
# ---------------------------------------------------------------------------
if (-not $SkipUpdate) {
    Write-Host '== Step 8: Creating update files ==' -ForegroundColor Cyan
    $UpdateUrl     = Require-ConfigString 'update-url'
    $UpdateOutput  = To-Absolute (Require-ConfigString 'update-output')
    $MsiProjectDir = To-Absolute 'MsiSetup'
    Ensure-Directories @($UpdateOutput)
    Invoke-Tool ('"{0}" update-creator /base-url "{1}" /project-dir "{2}" /output-folder "{3}"' -f $TinyWallExe, $UpdateUrl, $MsiProjectDir, $UpdateOutput)
}

Write-Host '== Build completed successfully. ==' -ForegroundColor Green