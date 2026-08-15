# DSH Web Launcher
# 1. If the DeepSeek Harness Web GUI is already running, just open the browser
#    (default browser, new tab).
# 2. Otherwise start "npx --yes @deepseek-ai/dsh web" in its own minimized
#    console window and open the harness in the default browser when ready.

$ErrorActionPreference = 'SilentlyContinue'
$port = 3080
$url  = "http://127.0.0.1:$port"

function Test-DshPort {
    $tcp = $null
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $iar = $tcp.BeginConnect('127.0.0.1', $port, $null, $null)
        if ($iar.AsyncWaitHandle.WaitOne(800)) {
            $tcp.EndConnect($iar)
            return $true
        }
        return $false
    } catch {
        return $false
    } finally {
        if ($tcp) { $tcp.Close() }
    }
}

function Open-HarnessBrowser {
    # default browser, new tab
    Start-Process $url
}

# Already running? Just open the browser.
if (Test-DshPort) {
    Open-HarnessBrowser
    exit 0
}

# Start the server in a minimized console window titled "DSH Web".
# `/c` (not `/k`): when the harness process exits, the window closes itself.
Start-Process -FilePath 'cmd.exe' -ArgumentList '/c', 'title DSH Web & npx --yes @deepseek-ai/dsh web' -WindowStyle Minimized

# Wait until the port answers, then open the browser.
$deadline = (Get-Date).AddMinutes(3)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if (Test-DshPort) {
        Open-HarnessBrowser
        exit 0
    }
}

# Timed out: tell the user where to look.
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.MessageBox]::Show(
    'DeepSeek Harness did not become ready within 3 minutes.' + "`r`n" +
    'Check the "DSH Web" console window for errors.',
    'DSH Web Launcher') | Out-Null
exit 1
