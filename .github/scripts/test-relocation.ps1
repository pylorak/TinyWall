<#
    Behavioural tests for ExecutableRelocator, run against a real build of TinyWall.exe.

    Builds a throwaway directory tree that mimics the layouts used by Squirrel/Electron
    installers, then asks the executable's "relocation-test" dev command what the service
    would do with each stored exception path.

    Usage:
        test-relocation.ps1 -Exe <path to TinyWall.exe>
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Exe
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Exe)) {
    throw "Executable not found: $Exe"
}

$Root = Join-Path ([System.IO.Path]::GetTempPath()) ("tw-reloc-" + [System.Guid]::NewGuid().ToString('N'))
$ResultFile = Join-Path $Root 'result.txt'

function New-Exe([string]$RelativePath) {
    $full = Join-Path $Root $RelativePath
    New-Item -ItemType Directory -Path (Split-Path -Parent $full) -Force | Out-Null
    Set-Content -LiteralPath $full -Value 'not a real executable' -NoNewline
    return $full
}

function New-Dir([string]$RelativePath) {
    $full = Join-Path $Root $RelativePath
    New-Item -ItemType Directory -Path $full -Force | Out-Null
    return $full
}

$script:Failures = 0
$script:Total = 0

function Assert-Relocation([string]$Name, [string]$OldRelative, [string]$ExpectedRelative) {
    $script:Total++

    $old = Join-Path $Root $OldRelative
    $expected = if ([string]::IsNullOrEmpty($ExpectedRelative)) { '' } else { Join-Path $Root $ExpectedRelative }

    if (Test-Path -LiteralPath $ResultFile) { Remove-Item -LiteralPath $ResultFile -Force }

    $proc = Start-Process -FilePath $Exe -NoNewWindow -Wait -PassThru -ArgumentList @(
        'relocation-test',
        '/executable-path', "`"$old`"",
        '/output-file', "`"$ResultFile`""
    )

    $actual = if (Test-Path -LiteralPath $ResultFile) { (Get-Content -LiteralPath $ResultFile -Raw) } else { '' }
    if ($null -eq $actual) { $actual = '' }
    $actual = $actual.Trim()

    # Exit code must agree with the written result: 0 = found, non-zero = nothing found.
    $expectedExit = if ($expected -eq '') { 1 } else { 0 }

    if (($actual -ieq $expected) -and ($proc.ExitCode -eq $expectedExit)) {
        Write-Host "PASS  $Name"
    }
    else {
        $script:Failures++
        Write-Host "FAIL  $Name"
        Write-Host "      stored   : $old"
        Write-Host "      expected : $(if ($expected -eq '') { '(no match)' } else { $expected }) [exit $expectedExit]"
        Write-Host "      actual   : $(if ($actual -eq '') { '(no match)' } else { $actual }) [exit $($proc.ExitCode)]"
    }
}

try {
    # --- Squirrel/Electron layout: version folders side by side ---
    New-Exe 'AnthropicClaude\app-0.10.0\claude.exe' | Out-Null
    New-Exe 'AnthropicClaude\app-0.11.0\claude.exe' | Out-Null
    New-Exe 'AnthropicClaude\app-0.9.3\other.exe'   | Out-Null
    Assert-Relocation 'update picks the highest version, compared numerically' `
        'AnthropicClaude\app-0.9.3\claude.exe' 'AnthropicClaude\app-0.11.0\claude.exe'

    New-Exe 'Discord\app-1.3.0\modules\voice\helper.exe' | Out-Null
    Assert-Relocation 'sub-path below the versioned folder is preserved' `
        'Discord\app-1.2.3\modules\voice\helper.exe' 'Discord\app-1.3.0\modules\voice\helper.exe'

    New-Exe 'AppFour\app-2.0\somethingelse.exe' | Out-Null
    Assert-Relocation 'a different file name is not followed' `
        'AppFour\app-1.0\wanted.exe' ''

    New-Exe 'AppFive\application-2.0\x.exe' | Out-Null
    Assert-Relocation 'a different folder stem is not followed' `
        'AppFive\app-1.0\x.exe' ''

    Assert-Relocation 'a missing install root is not followed (offline volume)' `
        'NoSuchRoot\app-1.0\x.exe' ''

    New-Dir 'AppSeven' | Out-Null
    Assert-Relocation 'an uninstalled application is left alone' `
        'AppSeven\app-1.0\x.exe' ''

    New-Exe 'AppEight\lib\x.exe' | Out-Null
    Assert-Relocation 'folder names without a version token are ignored' `
        'AppEight\bin\x.exe' ''

    New-Dir 'AppNine\app-1.0.0' | Out-Null
    New-Exe 'AppNine\app-1.1.0\app.exe' | Out-Null
    Assert-Relocation 'old folder left behind without the executable' `
        'AppNine\app-1.0.0\app.exe' 'AppNine\app-1.1.0\app.exe'

    New-Exe 'AppTen\app-1.2.3-beta.1\app.exe' | Out-Null
    Assert-Relocation 'prerelease suffix in the folder name' `
        'AppTen\app-1.2.2\app.exe' 'AppTen\app-1.2.3-beta.1\app.exe'

    New-Exe 'AppEleven\2.0.0\app.exe' | Out-Null
    Assert-Relocation 'bare version folder with no stem' `
        'AppEleven\1.2.3\app.exe' 'AppEleven\2.0.0\app.exe'

    New-Exe 'AppTwelve\app-0.8.0\app.exe' | Out-Null
    Assert-Relocation 'rollback is followed when it is the only candidate' `
        'AppTwelve\app-0.9.3\app.exe' 'AppTwelve\app-0.8.0\app.exe'

    $older = New-Exe 'AppThirteen\app-1.2\app.exe'
    $newer = New-Exe 'AppThirteen\app-1.2.0\app.exe'
    (Get-Item -LiteralPath $older).LastWriteTimeUtc = [DateTime]::UtcNow.AddDays(-2)
    (Get-Item -LiteralPath $newer).LastWriteTimeUtc = [DateTime]::UtcNow
    Assert-Relocation 'equal versions are broken by the newer file' `
        'AppThirteen\app-1.1\app.exe' 'AppThirteen\app-1.2.0\app.exe'

    $existing = New-Exe 'AppFourteen\app-1.0\app.exe'
    Assert-Relocation 'a path that still exists is never rewritten' `
        'AppFourteen\app-1.0\app.exe' ''

    New-Exe 'AppFifteen\rel-2.0\app-1.1\app.exe' | Out-Null
    New-Exe 'AppFifteen\rel-3.0\app-1.0\app.exe' | Out-Null
    Assert-Relocation 'the deepest versioned folder is tried first' `
        'AppFifteen\rel-2.0\app-1.0\app.exe' 'AppFifteen\rel-2.0\app-1.1\app.exe'

    Write-Host ''
    Write-Host "$($script:Total - $script:Failures)/$($script:Total) passed"
}
finally {
    if (Test-Path -LiteralPath $Root) {
        Remove-Item -LiteralPath $Root -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($script:Failures -gt 0) {
    exit 1
}
