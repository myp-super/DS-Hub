# DSH Web Stop
# Shuts down DeepSeek Harness: the server process tree including its
# "DSH Web" terminal window. (The browser tab opened in the default browser
# is left for the user to close.)
param(
    [int]$Port = 3080
)
$ErrorActionPreference = 'SilentlyContinue'

# kill the server tree: listener on $Port, walking up to the launcher's
# cmd window (CommandLine contains "DSH Web") so the terminal closes too.
$conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($conn) {
    $target = $conn.OwningProcess
    $cur = Get-CimInstance Win32_Process -Filter "ProcessId=$target" -ErrorAction SilentlyContinue
    while ($cur -and $cur.ParentProcessId -gt 0) {
        $pp = Get-CimInstance Win32_Process -Filter "ProcessId=$($cur.ParentProcessId)" -ErrorAction SilentlyContinue
        if (-not $pp) { break }
        $cur = $pp
        if ($cur.CommandLine -match 'DSH Web') { $target = $cur.ProcessId; break }
    }
    & taskkill /PID $target /T /F | Out-Null
}

Start-Sleep -Milliseconds 400
$still = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($still) { 'STOP FAILED' ; exit 1 } else { 'STOPPED' ; exit 0 }
