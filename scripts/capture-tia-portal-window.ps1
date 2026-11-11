[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ExpectedProjectPath,

    [string]$OutputDirectory,

    [switch]$ValidateOnly,

    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'runs')).TrimEnd('\')
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $runsRoot 'tia-portal-captures'
}
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\')
$tiaExecutable = 'C:\Program Files\SIEMENS\Automation\Portal V19\Bin\Siemens.Automation.Portal.exe'
$protectedProjectPath = [System.IO.Path]::GetFullPath(
    'C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12\Backup Schlenkers 36-10 190036-7-8v2.12.ap19'
)
$pathComparison = [System.StringComparison]::OrdinalIgnoreCase

function Assert-SafeProjectPath {
    param([Parameter(Mandatory)][string]$ProjectPath)

    $resolvedProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)
    if (-not $resolvedProjectPath.EndsWith('.ap19', $pathComparison)) {
        throw "Expected a TIA Portal V19 .ap19 project: $resolvedProjectPath"
    }
    if ($resolvedProjectPath.Equals($protectedProjectPath, $pathComparison)) {
        throw "Refusing the protected original TIA project: $resolvedProjectPath"
    }
    if ($resolvedProjectPath.IndexOf('-DeployWorking-', $pathComparison) -lt 0) {
        throw "TIA capture is limited to an explicit DeployWorking project: $resolvedProjectPath"
    }

    $resolvedProjectPath
}

function Assert-SafeProjectTitle {
    param(
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$ProjectPath
    )

    $expectedTitlePath = [System.IO.Path]::Combine(
        [System.IO.Path]::GetDirectoryName($ProjectPath),
        [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    )
    if ($Title.IndexOf($expectedTitlePath, $pathComparison) -lt 0) {
        throw "The foreground TIA window does not show the expected working project: $Title"
    }
}

$outputIsAllowed =
    $resolvedOutputDirectory.Equals($runsRoot, $pathComparison) -or
    $resolvedOutputDirectory.StartsWith($runsRoot + '\', $pathComparison)
if (-not $outputIsAllowed) {
    throw "Capture output must stay under the repository runs directory: $runsRoot"
}
if (-not (Test-Path -LiteralPath $tiaExecutable -PathType Leaf)) {
    throw "TIA Portal V19 executable was not found: $tiaExecutable"
}

$resolvedExpectedProjectPath = Assert-SafeProjectPath -ProjectPath $ExpectedProjectPath

if ($SelfTest) {
    $safeProject = 'C:\TIA Projects\Schlenkers-MTP1200-DeployWorking-20260919\Schlenkers.ap19'
    $safeResolved = Assert-SafeProjectPath -ProjectPath $safeProject
    Assert-SafeProjectTitle -Title (
        'Siemens - ' + [System.IO.Path]::ChangeExtension($safeResolved, $null).TrimEnd('.')
    ) -ProjectPath $safeResolved

    foreach ($unsafeProject in @(
        $protectedProjectPath,
        'C:\TIA Projects\Schlenkers\Schlenkers.ap19',
        'C:\TIA Projects\Schlenkers-MTP1200-DeployWorking-20260919\Schlenkers.ap20'
    )) {
        $rejected = $false
        try {
            [void](Assert-SafeProjectPath -ProjectPath $unsafeProject)
        }
        catch {
            $rejected = $true
        }
        if (-not $rejected) {
            throw "Unsafe TIA project was unexpectedly accepted: $unsafeProject"
        }
    }

    [PSCustomObject]@{
        Status = 'PASS'
        Mode = 'self-test'
        TiaExecutable = $tiaExecutable
        AllowedOutputRoot = $runsRoot + '\'
        CaptureMethod = 'foreground-window screen copy'
        InputCapability = 'none'
        UnsafeProjectRejection = 'PASS'
    }
    return
}

if (-not (Test-Path -LiteralPath $resolvedExpectedProjectPath -PathType Leaf)) {
    throw "Expected TIA working project was not found: $resolvedExpectedProjectPath"
}

if ($ValidateOnly) {
    [PSCustomObject]@{
        Status = 'PASS'
        Mode = 'validate-only'
        ExpectedProjectPath = $resolvedExpectedProjectPath
        TiaExecutable = $tiaExecutable
        AllowedOutputRoot = $runsRoot + '\'
        CaptureMethod = 'foreground-window screen copy'
        InputCapability = 'none'
    }
    return
}

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

if (-not ('Schlenker.TiaPortalCapture.NativeMethods' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Schlenker.TiaPortalCapture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr window);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr window, StringBuilder text, int maximumCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr window, out Rect rectangle);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(
            IntPtr window,
            int attribute,
            out Rect value,
            int valueSize
        );
    }
}
'@
}

function Get-WindowTitle {
    param([Parameter(Mandatory)][IntPtr]$Window)

    $builder = [System.Text.StringBuilder]::new(2048)
    [void][Schlenker.TiaPortalCapture.NativeMethods]::GetWindowText(
        $Window,
        $builder,
        $builder.Capacity
    )
    $builder.ToString()
}

function Get-WindowBounds {
    param([Parameter(Mandatory)][IntPtr]$Window)

    $rectangle = [Schlenker.TiaPortalCapture.Rect]::new()
    $rectangleSize = [System.Runtime.InteropServices.Marshal]::SizeOf(
        [type][Schlenker.TiaPortalCapture.Rect]
    )
    $dwmResult = [Schlenker.TiaPortalCapture.NativeMethods]::DwmGetWindowAttribute(
        $Window,
        9,
        [ref]$rectangle,
        $rectangleSize
    )
    if ($dwmResult -ne 0 -and
        -not [Schlenker.TiaPortalCapture.NativeMethods]::GetWindowRect(
            $Window,
            [ref]$rectangle
        )) {
        throw "Unable to read TIA window bounds for handle $($Window.ToInt64())."
    }

    [PSCustomObject]@{
        Left = $rectangle.Left
        Top = $rectangle.Top
        Right = $rectangle.Right
        Bottom = $rectangle.Bottom
        Width = $rectangle.Right - $rectangle.Left
        Height = $rectangle.Bottom - $rectangle.Top
    }
}

$tiaProcesses = @(
    Get-Process -Name 'Siemens.Automation.Portal' -ErrorAction SilentlyContinue |
        Where-Object {
            $_.MainWindowHandle -ne [IntPtr]::Zero -and
            $_.MainModule.FileName.Equals($tiaExecutable, $pathComparison)
        }
)
if ($tiaProcesses.Count -ne 1) {
    throw "Expected exactly one window-owning TIA Portal V19 process; found $($tiaProcesses.Count)."
}
$tiaProcess = $tiaProcesses[0]
$actualExecutable = $tiaProcess.MainModule.FileName
if (-not $actualExecutable.Equals($tiaExecutable, $pathComparison)) {
    throw "Unexpected TIA Portal executable path: $actualExecutable"
}
if ($tiaProcess.MainWindowHandle -eq [IntPtr]::Zero) {
    throw 'TIA Portal does not currently expose a main window.'
}

$tiaBase = 'C:\Program Files\SIEMENS\Automation\Portal V19'
[void][Reflection.Assembly]::LoadFrom(
    (Join-Path $tiaBase 'Bin\PublicAPI\Siemens.Engineering.Contract.dll')
)
[void][Reflection.Assembly]::LoadFrom(
    (Join-Path $tiaBase 'Bin\PublicAPI\Siemens.Engineering.ClientAdapter.Interfaces.dll')
)
[void][Reflection.Assembly]::LoadFrom(
    (Join-Path $tiaBase 'PublicAPI\V19\Siemens.Engineering.dll')
)
$tiaProcessInfo = @(
    [Siemens.Engineering.TiaPortal]::GetProcesses() |
        Where-Object { $_.Id -eq $tiaProcess.Id }
)
if ($tiaProcessInfo.Count -ne 1) {
    throw 'Unable to identify exactly one active TIA Portal project through Openness.'
}
$activeProjectPath = [System.IO.Path]::GetFullPath($tiaProcessInfo[0].ProjectPath)
if (-not $activeProjectPath.Equals($resolvedExpectedProjectPath, $pathComparison)) {
    throw "Active TIA project differs from the expected working project: $activeProjectPath"
}

$foregroundWindow = [Schlenker.TiaPortalCapture.NativeMethods]::GetForegroundWindow()
if ($foregroundWindow -eq [IntPtr]::Zero) {
    throw 'Windows did not report a foreground window.'
}
$foregroundProcessId = [uint32]0
[void][Schlenker.TiaPortalCapture.NativeMethods]::GetWindowThreadProcessId(
    $foregroundWindow,
    [ref]$foregroundProcessId
)
if ($foregroundProcessId -ne [uint32]$tiaProcess.Id) {
    throw 'TIA Portal must own the foreground before a capture is allowed.'
}

$captureHandles = @(
    $tiaProcess.MainWindowHandle
    $foregroundWindow
) | Select-Object -Unique
$windowRecords = @(foreach ($windowHandle in $captureHandles) {
    if (-not [Schlenker.TiaPortalCapture.NativeMethods]::IsWindowVisible($windowHandle)) {
        if ($windowHandle -eq $foregroundWindow) {
            throw "Foreground TIA Portal window $($windowHandle.ToInt64()) is not visible."
        }
        continue
    }
    if ([Schlenker.TiaPortalCapture.NativeMethods]::IsIconic($windowHandle)) {
        if ($windowHandle -eq $foregroundWindow) {
            throw "Foreground TIA Portal window $($windowHandle.ToInt64()) is minimized."
        }
        continue
    }
    $windowTitle = Get-WindowTitle -Window $windowHandle
    $windowBounds = Get-WindowBounds -Window $windowHandle
    if ($windowBounds.Width -lt 32 -or $windowBounds.Height -lt 32) {
        if ($windowHandle -eq $foregroundWindow) {
            throw "Foreground TIA Portal window $($windowHandle.ToInt64()) has invalid capture bounds."
        }
        continue
    }
    [PSCustomObject]@{
        Handle = $windowHandle
        Title = $windowTitle
        Bounds = $windowBounds
    }
})
if ($windowRecords.Count -eq 0) {
    throw 'No visible TIA Portal window was available for capture.'
}

New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null
$captureTimestamp = Get-Date
$captureStem = 'tia-portal-{0}' -f $captureTimestamp.ToString('yyyyMMdd-HHmmssfff')
$images = @()
$virtualScreen = [System.Windows.Forms.SystemInformation]::VirtualScreen

for ($index = 0; $index -lt $windowRecords.Count; $index++) {
    $record = $windowRecords[$index]
    $bounds = $record.Bounds
    $captureLeft = [Math]::Max($bounds.Left, $virtualScreen.Left)
    $captureTop = [Math]::Max($bounds.Top, $virtualScreen.Top)
    $captureRight = [Math]::Min($bounds.Right, $virtualScreen.Right)
    $captureBottom = [Math]::Min($bounds.Bottom, $virtualScreen.Bottom)
    $captureWidth = $captureRight - $captureLeft
    $captureHeight = $captureBottom - $captureTop
    if ($captureWidth -lt 32 -or $captureHeight -lt 32) {
        throw "TIA Portal window $($record.Handle.ToInt64()) has no useful visible capture area."
    }

    $bitmap = [System.Drawing.Bitmap]::new(
        $captureWidth,
        $captureHeight,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(
            $captureLeft,
            $captureTop,
            0,
            0,
            [System.Drawing.Size]::new($captureWidth, $captureHeight),
            [System.Drawing.CopyPixelOperation]::SourceCopy
        )
        $imageName = '{0}-window-{1}-hwnd-{2}.png' -f (
            $captureStem,
            ($index + 1),
            $record.Handle.ToInt64()
        )
        $imagePath = Join-Path $resolvedOutputDirectory $imageName
        $bitmap.Save($imagePath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }

    $imageItem = Get-Item -LiteralPath $imagePath
    $images += [PSCustomObject]@{
        Path = $imageItem.FullName
        SHA256 = (Get-FileHash -LiteralPath $imageItem.FullName -Algorithm SHA256).Hash
        Length = $imageItem.Length
        WindowHandle = $record.Handle.ToInt64()
        WindowTitle = $record.Title
        Bounds = $record.Bounds
        CapturedBounds = [PSCustomObject]@{
            Left = $captureLeft
            Top = $captureTop
            Right = $captureRight
            Bottom = $captureBottom
            Width = $captureWidth
            Height = $captureHeight
        }
    }
}

$manifest = [PSCustomObject]@{
    CapturedAt = $captureTimestamp.ToString('o')
    Status = 'PASS'
    SafetyModel = 'read-only, exact-process, foreground-only, explicit-working-project-only'
    CaptureMethod = 'System.Drawing.Graphics.CopyFromScreen'
    InputCapability = 'none'
    ProcessId = $tiaProcess.Id
    ExecutablePath = $actualExecutable
    ExpectedProjectPath = $resolvedExpectedProjectPath
    MainWindowHandle = $tiaProcess.MainWindowHandle.ToInt64()
    ForegroundWindowHandle = $foregroundWindow.ToInt64()
    Images = $images
}
$manifestPath = Join-Path $resolvedOutputDirectory ($captureStem + '-manifest.json')
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

[PSCustomObject]@{
    Status = 'PASS'
    Manifest = $manifestPath
    Images = @($images | ForEach-Object { $_.Path })
}
