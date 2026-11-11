# Cooperative batch lease, outside the native project. No native file is opened.
# CreateNew is atomic across processes; a crash leaves a marker requiring review.
function Enter-TiaProjectLease {
    param([Parameter(Mandatory)][string]$ProjectPath)
    $fullPath = [IO.Path]::GetFullPath($ProjectPath)
    $identity = $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ([IO.Path]::DirectorySeparatorChar -eq '\') {
        $identity = $identity.Replace('/', '\').ToUpperInvariant()
    }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $key = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($identity))).Replace('-', '') }
    finally { $sha.Dispose() }
    $base = if ([IO.Path]::DirectorySeparatorChar -eq '\') { [Environment]::GetFolderPath('LocalApplicationData') } else { [IO.Path]::GetTempPath() }
    $root = Join-Path $base 'Schlenker/ProjectLeases'
    [IO.Directory]::CreateDirectory($root) | Out-Null
    $path = Join-Path $root ($key + '.lease')
    try {
        $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
    }
    catch {
        throw "PROJECT_ACCESS_BLOCKED: $path. Another batch owns this project, or a previous run ended unexpectedly. Reconcile the owner and native state; never remove a live lease or retry on a timer. $($_.Exception.Message)"
    }
    try {
        $data = @{ project=$fullPath; pid=$PID; startedUtc=[DateTime]::UtcNow.ToString('o'); owner=[Environment]::UserName } | ConvertTo-Json -Compress
        $bytes = [Text.Encoding]::UTF8.GetBytes($data)
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush($true)
        return [pscustomobject]@{ Stream=$stream; Path=$path }
    }
    catch {
        # Keep the marker: initialization failure is not proof of a safe retry.
        $stream.Dispose()
        throw
    }
}

function Exit-TiaProjectLease {
    param([Parameter(Mandatory)]$Lease)
    $Lease.Stream.Dispose()
    [IO.File]::Delete($Lease.Path)
}
