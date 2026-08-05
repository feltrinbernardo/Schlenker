[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$Request,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$Summary,

    [ValidateSet('completed', 'partial', 'blocked')]
    [string]$Status = 'completed',

    [string[]]$Files = @(),

    [string]$Notes,

    [datetimeoffset]$Timestamp = [datetimeoffset]::Now
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$logDirectory = Join-Path $repositoryRoot 'logs\change-log'
$logPath = Join-Path $logDirectory ($Timestamp.ToString('yyyy-MM-dd') + '.md')
$gitExecutable = 'C:\Program Files\Git\cmd\git.exe'

if (-not (Test-Path -LiteralPath $gitExecutable -PathType Leaf)) {
    $gitCommand = Get-Command git -ErrorAction SilentlyContinue
    if (-not $gitCommand) {
        throw 'Git is required to record branch and commit metadata.'
    }
    $gitExecutable = $gitCommand.Source
}

function ConvertTo-SingleLine {
    param([AllowEmptyString()][string]$Value)

    if (-not $Value) {
        return ''
    }
    return (($Value -replace '[\r\n]+', ' ') -replace '\s{2,}', ' ').Trim()
}

function ConvertTo-RepositoryPath {
    param([Parameter(Mandatory)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        $fullPath = [System.IO.Path]::GetFullPath($Path)
        $rootWithSeparator = $repositoryRoot.TrimEnd('\') + '\'
        if ($fullPath.StartsWith(
            $rootWithSeparator,
            [System.StringComparison]::OrdinalIgnoreCase
        )) {
            return $fullPath.Substring($rootWithSeparator.Length).Replace('\', '/')
        }
        return $fullPath
    }
    return $Path.Replace('\', '/')
}

$branch = (& $gitExecutable -C $repositoryRoot branch --show-current).Trim()
$commit = (& $gitExecutable -C $repositoryRoot rev-parse --short HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to read repository commit metadata.'
}
if (-not $branch) {
    $branch = '(detached HEAD)'
}

$requestText = ConvertTo-SingleLine -Value $Request
$summaryText = ConvertTo-SingleLine -Value $Summary
$notesText = ConvertTo-SingleLine -Value $Notes
$affectedFiles = @(
    $Files |
        Where-Object { $_ } |
        ForEach-Object { ConvertTo-RepositoryPath -Path $_ } |
        Sort-Object -Unique
)

$entryLines = @(
    "## $($Timestamp.ToString('o'))"
    ''
    "- Status: $Status"
    "- Request: $requestText"
    "- Result: $summaryText"
    "- Branch: $branch"
    "- Base commit: $commit"
    "- Actor: $env:USERNAME"
    "- UTC timestamp: $($Timestamp.UtcDateTime.ToString('o'))"
)
if ($affectedFiles.Count -eq 0) {
    $entryLines += '- Files: none recorded'
}
else {
    $entryLines += '- Files:'
    $entryLines += $affectedFiles | ForEach-Object { "  - ``$_``" }
}
if ($notesText) {
    $entryLines += "- Notes: $notesText"
}
$entry = ($entryLines -join [Environment]::NewLine) +
    [Environment]::NewLine + [Environment]::NewLine

if ($PSCmdlet.ShouldProcess($logPath, 'Append change-log entry')) {
    if (-not (Test-Path -LiteralPath $logDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    }
    if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
        $header = "# Change Log - $($Timestamp.ToString('yyyy-MM-dd'))" +
            [Environment]::NewLine + [Environment]::NewLine
        [System.IO.File]::WriteAllText(
            $logPath,
            $header,
            [System.Text.UTF8Encoding]::new($false)
        )
    }
    [System.IO.File]::AppendAllText(
        $logPath,
        $entry,
        [System.Text.UTF8Encoding]::new($false)
    )
}

[PSCustomObject]@{
    Status = 'PASS'
    LogPath = $logPath
    Timestamp = $Timestamp.ToString('o')
    Branch = $branch
    Commit = $commit
    FilesRecorded = $affectedFiles.Count
}
