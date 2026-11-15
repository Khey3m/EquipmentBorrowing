param([Parameter(Mandatory=$true)][int]$ProcessId, [Parameter(Mandatory=$true)][string]$ImagePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class WindowCapture { [DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect); public struct RECT { public int Left, Top, Right, Bottom; } }'
[void][WindowCapture]::SetProcessDPIAware()
$windowProcess = Get-Process -Id $ProcessId
$rect = New-Object WindowCapture+RECT
if (![WindowCapture]::GetWindowRect($windowProcess.MainWindowHandle, [ref]$rect)) { throw 'Window bounds unavailable.' }
$bitmap = New-Object System.Drawing.Bitmap ($rect.Right-$rect.Left),($rect.Bottom-$rect.Top)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    $graphics.CopyFromScreen($rect.Left,$rect.Top,0,0,$bitmap.Size)
    $bitmap.Save($ImagePath)
}
finally { $graphics.Dispose(); $bitmap.Dispose() }
