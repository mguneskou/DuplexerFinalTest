param([string]$p)
$lines = Get-Content -Path $p
$header = $lines[0]
Write-Output "Header: $header"
$cols = $header -split ','
$idx = [array]::IndexOf($cols, 'CH1 Voltage(V)')
Write-Output "Index: $idx"
$vals = @()
for ($i = 1; $i -lt $lines.Count; $i++) {
    if ([string]::IsNullOrWhiteSpace($lines[$i])) { continue }
    $parts = $lines[$i] -split ','
        if ($parts.Length -gt $idx) {
        $s = $parts[$idx]
        $d = 0.0
        $ok = [double]::TryParse($s, [ref]$d)
        Write-Output "Line $i -> raw='$s' parsed=$ok value=$d"
        if ($ok) { $vals += $d }
    }
}
if ($vals.Count -gt 0) { $mean = ($vals | Measure-Object -Average).Average; $meanStr = [Math]::Round($mean,6).ToString('F6', [System.Globalization.CultureInfo]::InvariantCulture); Write-Output "Mean: $meanStr" } else { Write-Output "No numeric values parsed" }
