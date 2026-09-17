# Starts Blender and waits until the MCP add-on's bridge server listens on its port.
# Exit code 0 when the bridge is reachable, 1 otherwise.
param(
    [string]$BlendFile,
    [int]$Port = 9876,
    [int]$TimeoutSeconds = 60
)

$blender = $env:BLENDER_PATH ?? 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
if (-not (Test-Path $blender -PathType Leaf)) {
    "Blender not found at '$blender'. Set BLENDER_PATH or update the default path in this script and in .mcp.json."
    exit 1
}

function Get-Listener {
    Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
}

$listener = Get-Listener
if ($listener) {
    $owner = Get-Process -Id $listener.OwningProcess
    "Port $Port is already in use by $($owner.ProcessName) (PID $($owner.Id))."
    exit ($owner.ProcessName -eq 'blender' ? 0 : 1)
}

$startArgs = @{ FilePath = $blender; PassThru = $true }
if ($BlendFile) { $startArgs.ArgumentList = "`"$(Resolve-Path $BlendFile)`"" }
$process = Start-Process @startArgs

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while (-not (Get-Listener)) {
    if ($process.HasExited) {
        "Blender exited with code $($process.ExitCode) before the MCP bridge started."
        exit 1
    }
    if ((Get-Date) -gt $deadline) {
        "Blender is running (PID $($process.Id)) but nothing listens on port $Port after $TimeoutSeconds seconds."
        exit 1
    }
    Start-Sleep -Milliseconds 500
}

"Blender (PID $($process.Id)) MCP bridge is listening on port $Port."
exit 0
