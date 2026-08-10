[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$gitCommand = Get-Command git -ErrorAction Stop
$failures = [System.Collections.Generic.List[string]]::new()

$repositoryFiles = @(
    & $gitCommand.Source -C $repositoryRoot ls-files --cached --others --exclude-standard
)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to enumerate tracked repository files.'
}

$forbiddenPathPattern =
    '^(outputs|programmer_progetv19|img-log|\.modification_logic|\.tmp|tmp|logs/judge)/'
$forbiddenExtensionPattern = '\.(ap\d+|gx3|gxw|zap\d+|dll|exe|pdb)$'

foreach ($file in $repositoryFiles) {
    $normalizedPath = $file.Replace('\', '/')
    if ($normalizedPath -match $forbiddenPathPattern) {
        $failures.Add("Generated or local-only path is tracked: $normalizedPath")
    }
    if ($normalizedPath -match $forbiddenExtensionPattern) {
        $failures.Add("Native project or compiled binary is tracked: $normalizedPath")
    }
}

$strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$jsonFiles = @($repositoryFiles | Where-Object { $_ -match '\.json$' })
foreach ($file in $jsonFiles) {
    $fullPath = Join-Path $repositoryRoot $file
    try {
        $content = [System.IO.File]::ReadAllText($fullPath, $strictUtf8)
        $null = $content | ConvertFrom-Json
    }
    catch {
        $failures.Add("Invalid UTF-8 JSON in ${file}: $($_.Exception.Message)")
    }
}

$powershellFiles = @($repositoryFiles | Where-Object { $_ -match '\.ps1$' })
foreach ($file in $powershellFiles) {
    $tokens = $null
    $parseErrors = $null
    $fullPath = Join-Path $repositoryRoot $file
    $null = [System.Management.Automation.Language.Parser]::ParseFile(
        $fullPath,
        [ref]$tokens,
        [ref]$parseErrors
    )
    foreach ($parseError in $parseErrors) {
        $failures.Add("PowerShell parse error in ${file}: $($parseError.Message)")
    }
}

$pythonFiles = @($repositoryFiles | Where-Object { $_ -match '\.py$' })
if ($pythonFiles.Count -gt 0) {
    $pythonCommand = Get-Command python -ErrorAction SilentlyContinue
    if (-not $pythonCommand) {
        $failures.Add('Python is required to validate tracked Python files.')
    }
    else {
        $pythonPaths = @($pythonFiles | ForEach-Object {
            Join-Path $repositoryRoot $_
        })
        & $pythonCommand.Source -m py_compile @pythonPaths
        if ($LASTEXITCODE -ne 0) {
            $failures.Add('One or more tracked Python files failed syntax validation.')
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    throw "Repository validation failed with $($failures.Count) issue(s)."
}

[PSCustomObject]@{
    Status = 'PASS'
    RepositoryFiles = $repositoryFiles.Count
    JsonFiles = $jsonFiles.Count
    PowerShellFiles = $powershellFiles.Count
    PythonFiles = $pythonFiles.Count
}
