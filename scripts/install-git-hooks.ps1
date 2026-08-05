[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$hookPath = Join-Path $repositoryRoot '.githooks\pre-commit'
$gitExecutable = 'C:\Program Files\Git\cmd\git.exe'

if (-not (Test-Path -LiteralPath $hookPath -PathType Leaf)) {
    throw "Tracked pre-commit hook not found: $hookPath"
}
if (-not (Test-Path -LiteralPath $gitExecutable -PathType Leaf)) {
    $gitCommand = Get-Command git -ErrorAction SilentlyContinue
    if (-not $gitCommand) {
        throw 'Git is required to install the repository hooks.'
    }
    $gitExecutable = $gitCommand.Source
}

& $gitExecutable -C $repositoryRoot config --local core.hooksPath .githooks
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to configure core.hooksPath.'
}

$configuredPath = (& $gitExecutable -C $repositoryRoot config --local --get core.hooksPath).Trim()
if ($configuredPath -ne '.githooks') {
    throw "Unexpected core.hooksPath value: $configuredPath"
}

[PSCustomObject]@{
    Status = 'PASS'
    Repository = $repositoryRoot
    HooksPath = $configuredPath
    PreCommitHook = $hookPath
}

