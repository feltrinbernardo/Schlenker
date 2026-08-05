[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\runs\gxworks2-captures'),
    [switch]$ValidateOnly,
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'runs')).TrimEnd('\')
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\')
$gxWorksExecutable = 'C:\Program Files (x86)\MELSOFT\GPPW2\GD2.exe'
$allowedProjectRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repoRoot 'fixtures\projects')
).TrimEnd('\') + '\'
$protectedProjectPath = 'D:\GX Works\schlenker.gxw'
$pathComparison = [System.StringComparison]::OrdinalIgnoreCase

$outputIsAllowed =
    $resolvedOutputDirectory.Equals($runsRoot, $pathComparison) -or
    $resolvedOutputDirectory.StartsWith($runsRoot + '\', $pathComparison)
if (-not $outputIsAllowed) {
    throw "Capture output must stay under the repository runs directory: $runsRoot"
}
if (-not (Test-Path -LiteralPath $gxWorksExecutable -PathType Leaf)) {
    throw "GX Works2 executable was not found: $gxWorksExecutable"
}

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

if (-not ('Schlenker.GxWorksCapture.NativeMethods' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Schlenker.GxWorksCapture
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
    [void][Schlenker.GxWorksCapture.NativeMethods]::GetWindowText(
        $Window,
        $builder,
        $builder.Capacity
    )
    $builder.ToString()
}

function Get-WindowBounds {
    param([Parameter(Mandatory)][IntPtr]$Window)

    $rectangle = [Schlenker.GxWorksCapture.Rect]::new()
    $rectangleSize = [System.Runtime.InteropServices.Marshal]::SizeOf(
        [type][Schlenker.GxWorksCapture.Rect]
    )
    $dwmResult = [Schlenker.GxWorksCapture.NativeMethods]::DwmGetWindowAttribute(
        $Window,
        9,
        [ref]$rectangle,
        $rectangleSize
    )
    if ($dwmResult -ne 0) {
        if (-not [Schlenker.GxWorksCapture.NativeMethods]::GetWindowRect(
            $Window,
            [ref]$rectangle
        )) {
            throw "Unable to read window bounds for handle $($Window.ToInt64())."
        }
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

function Assert-SafeProjectTitle {
    param([AllowEmptyString()][string]$Title)

    if ($Title.IndexOf($protectedProjectPath, $pathComparison) -ge 0 -or
        $Title.IndexOf('D:\GX Works\', $pathComparison) -ge 0) {
        throw "Refusing to capture the protected original project: $Title"
    }
    if ($Title -match '(?i)\.gxw' -and
        $Title.IndexOf($allowedProjectRoot, $pathComparison) -lt 0) {
        $truncatedProjectMatch = [regex]::Match(
            $Title,
            '(?i)MELSOFT Series GX Works2\s+(?<project>\.\.\..+?\.gxw)(?:\s+-\s+\[|$)'
        )
        $recentKey = 'HKCU:\Software\MITSUBISHI\SWnDN-GPPW2\MainFrame\Recent File List'
        $configuredProject = if (Test-Path -LiteralPath $recentKey) {
            Get-ItemPropertyValue -LiteralPath $recentKey -Name File1 -ErrorAction SilentlyContinue
        }
        $configuredProject = if ($configuredProject) {
            [System.IO.Path]::GetFullPath($configuredProject)
        }
        $truncatedSuffix = if ($truncatedProjectMatch.Success) {
            $truncatedProjectMatch.Groups['project'].Value.Substring(3)
        }
        $truncatedTitleIsSafe =
            $configuredProject -and
            $configuredProject.StartsWith($allowedProjectRoot, $pathComparison) -and
            (Test-Path -LiteralPath $configuredProject -PathType Leaf) -and
            $truncatedSuffix -and
            $configuredProject.EndsWith($truncatedSuffix, $pathComparison)

        if (-not $truncatedTitleIsSafe) {
            throw "Refusing to capture a GX Works2 project outside the disposable root: $Title"
        }
    }
}

if ($SelfTest) {
    Assert-SafeProjectTitle -Title 'MELSOFT Series GX Works2'
    Assert-SafeProjectTitle -Title (
        'MELSOFT Series GX Works2 ' +
        (Join-Path $allowedProjectRoot 'schlenker-proof\schlenker-proof.gxw')
    )

    $configuredProjectForSelfTest = Get-ItemPropertyValue -LiteralPath (
        'HKCU:\Software\MITSUBISHI\SWnDN-GPPW2\MainFrame\Recent File List'
    ) -Name File1
    $configuredProjectDirectoryName = Split-Path -Leaf (
        Split-Path -Parent $configuredProjectForSelfTest
    )
    $configuredProjectSuffix = $configuredProjectDirectoryName.Substring(
        [Math]::Min(15, $configuredProjectDirectoryName.Length)
    ) + '\' + (Split-Path -Leaf $configuredProjectForSelfTest)
    Assert-SafeProjectTitle -Title (
        "MELSOFT Series GX Works2 ...$configuredProjectSuffix - [[PRG]Read MAIN (Read Only)]"
    )

    $unsafeTitles = @(
        "MELSOFT Series GX Works2 $protectedProjectPath",
        'MELSOFT Series GX Works2 C:\unapproved\project.gxw',
        'MELSOFT Series GX Works2 ...not-the-configured-project\project.gxw - [[PRG]Read MAIN]'
    )
    foreach ($unsafeTitle in $unsafeTitles) {
        $wasRejected = $false
        try {
            Assert-SafeProjectTitle -Title $unsafeTitle
        }
        catch {
            $wasRejected = $true
        }
        if (-not $wasRejected) {
            throw "Unsafe project title was unexpectedly accepted: $unsafeTitle"
        }
    }

    [PSCustomObject]@{
        Status = 'PASS'
        Mode = 'self-test'
        GXWorksExecutable = $gxWorksExecutable
        AllowedProjectRoot = $allowedProjectRoot
        AllowedOutputRoot = $runsRoot + '\'
        CaptureMethod = 'foreground-window screen copy'
        InputCapability = 'none'
        UnsafeTitleRejection = 'PASS'
    }
    return
}

if ($ValidateOnly) {
    [PSCustomObject]@{
        Status = 'PASS'
        Mode = 'validate-only'
        GXWorksExecutable = $gxWorksExecutable
        AllowedProjectRoot = $allowedProjectRoot
        AllowedOutputRoot = $runsRoot + '\'
        CaptureMethod = 'foreground-window screen copy'
        InputCapability = 'none'
    }
    return
}

$gxProcesses = @(Get-Process -Name GD2 -ErrorAction SilentlyContinue)
if ($gxProcesses.Count -ne 1) {
    throw "Expected exactly one running GX Works2 process; found $($gxProcesses.Count)."
}
$gxProcess = $gxProcesses[0]
$actualExecutable = $gxProcess.MainModule.FileName
if (-not $actualExecutable.Equals($gxWorksExecutable, $pathComparison)) {
    throw "Unexpected GX Works2 executable path: $actualExecutable"
}
if ($gxProcess.MainWindowHandle -eq [IntPtr]::Zero) {
    throw 'GX Works2 does not currently expose a main window.'
}

$foregroundWindow = [Schlenker.GxWorksCapture.NativeMethods]::GetForegroundWindow()
if ($foregroundWindow -eq [IntPtr]::Zero) {
    throw 'Windows did not report a foreground window.'
}
$foregroundProcessId = [uint32]0
[void][Schlenker.GxWorksCapture.NativeMethods]::GetWindowThreadProcessId(
    $foregroundWindow,
    [ref]$foregroundProcessId
)
if ($foregroundProcessId -ne [uint32]$gxProcess.Id) {
    throw 'GX Works2 must own the foreground before a capture is allowed.'
}

$captureHandles = @(
    $gxProcess.MainWindowHandle
    $foregroundWindow
) | Select-Object -Unique
$windowRecords = @(foreach ($windowHandle in $captureHandles) {
    if (-not [Schlenker.GxWorksCapture.NativeMethods]::IsWindowVisible($windowHandle)) {
        throw "GX Works2 window $($windowHandle.ToInt64()) is not visible."
    }
    if ([Schlenker.GxWorksCapture.NativeMethods]::IsIconic($windowHandle)) {
        throw "GX Works2 window $($windowHandle.ToInt64()) is minimized."
    }
    $windowTitle = Get-WindowTitle -Window $windowHandle
    Assert-SafeProjectTitle -Title $windowTitle
    $windowBounds = Get-WindowBounds -Window $windowHandle
    if ($windowBounds.Width -lt 32 -or $windowBounds.Height -lt 32) {
        throw "GX Works2 window $($windowHandle.ToInt64()) has invalid capture bounds."
    }
    [PSCustomObject]@{
        Handle = $windowHandle
        Title = $windowTitle
        Bounds = $windowBounds
    }
})

New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null
$captureTimestamp = Get-Date
$captureStem = 'gxworks2-{0}' -f $captureTimestamp.ToString('yyyyMMdd-HHmmssfff')
$images = @()
$virtualScreen = [System.Windows.Forms.SystemInformation]::VirtualScreen

for ($index = 0; $index -lt $windowRecords.Count; $index++) {
    $record = $windowRecords[$index]
    $bounds = $record.Bounds
    if ($bounds.Left -lt $virtualScreen.Left -or
        $bounds.Top -lt $virtualScreen.Top -or
        $bounds.Right -gt $virtualScreen.Right -or
        $bounds.Bottom -gt $virtualScreen.Bottom) {
        throw "GX Works2 window $($record.Handle.ToInt64()) is not fully inside the visible desktop."
    }

    $bitmap = [System.Drawing.Bitmap]::new(
        $bounds.Width,
        $bounds.Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(
            $bounds.Left,
            $bounds.Top,
            0,
            0,
            [System.Drawing.Size]::new($bounds.Width, $bounds.Height),
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
    }
}

$manifest = [PSCustomObject]@{
    CapturedAt = $captureTimestamp.ToString('o')
    Status = 'PASS'
    SafetyModel = 'read-only, exact-process, foreground-only, disposable-project-only'
    CaptureMethod = 'System.Drawing.Graphics.CopyFromScreen'
    InputCapability = 'none'
    ProcessId = $gxProcess.Id
    ExecutablePath = $actualExecutable
    MainWindowHandle = $gxProcess.MainWindowHandle.ToInt64()
    ForegroundWindowHandle = $foregroundWindow.ToInt64()
    AllowedProjectRoot = $allowedProjectRoot
    Images = $images
}
$manifestPath = Join-Path $resolvedOutputDirectory ($captureStem + '-manifest.json')
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

[PSCustomObject]@{
    Status = 'PASS'
    Manifest = $manifestPath
    Images = @($images | ForEach-Object { $_.Path })
}
