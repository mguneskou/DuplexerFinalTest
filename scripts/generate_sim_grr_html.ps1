$dataRootParent = 'P:\MGunes\DuplexerTestSuite\Results'

# Find most recent GaugeRR_SimRun_*_Data folder
$candidate = Get-ChildItem -Path $dataRootParent -Directory | Where-Object { $_.Name -like 'GaugeRR_SimRun_*_Data' } | Sort-Object Name -Descending | Select-Object -First 1
if (-not $candidate) { Write-Error "No GaugeRR_SimRun_*_Data folders found under $dataRootParent"; exit 2 }
$dataRoot = $candidate.FullName
Write-Output "Using data root: $dataRoot"

$rows = @()
foreach ($opdir in Get-ChildItem -Path $dataRoot -Directory) {
    $operatorName = $opdir.Name
    foreach ($rep in Get-ChildItem -Path $opdir.FullName -Directory) {
        $replicateNo = $rep.Name -replace 'Replicate_',''
        foreach ($groupDir in Get-ChildItem -Path $rep.FullName -Directory) {
            $groupName = $groupDir.Name
            foreach ($csv in Get-ChildItem -Path $groupDir.FullName -Filter '*.csv' -File) {
                try {
                    $lines = Get-Content -Path $csv.FullName
                    if ($lines.Count -lt 2) { continue }
                    $cols = $lines[0] -split ','
                    $idx = [array]::IndexOf($cols, 'CH1 Voltage(V)')
                    if ($idx -lt 0) { $idx = 0 }
                    $vals = @()
                    for ($i=1; $i -lt $lines.Count; $i++) {
                        if ([string]::IsNullOrWhiteSpace($lines[$i])) { continue }
                        $parts = $lines[$i] -split ','
                        if ($parts.Length -gt $idx) {
                            $d = 0.0
                            if ([double]::TryParse($parts[$idx], [ref]$d)) { $vals += $d }
                        }
                    }
                    if ($vals.Count -eq 0) { continue }
                    $mean = [Math]::Round(($vals | Measure-Object -Average).Average,6)
                    $rows += [PSCustomObject]@{ Group = $groupName; TestType = ($csv.Name -split '_')[1]; Column='CH1 Voltage(V)'; Part = ($csv.Name -split '_')[0]; Operator = $operatorName; Replicate = $replicateNo; Mean = $mean; Path = $csv.FullName }
                } catch { }
            }
        }
    }
}

$html = @"
<!DOCTYPE html>
<html><head><meta charset='utf-8'/><title>Gauge R&R Sim Report</title></head><body>
<h1>Simulated Gauge R&R Report</h1>
<table>
<tr><th>Group</th><th>Test</th><th>Column</th><th>Part</th><th>Operator</th><th>Replicate</th><th>Value</th><th>CSV Path</th></tr>
"@

foreach ($r in $rows) {
    $html += "<tr><td>$($r.Group)</td><td>$($r.TestType)</td><td>$($r.Column)</td><td>$($r.Part)</td><td>$($r.Operator)</td><td>$($r.Replicate)</td><td>$($r.Mean.ToString('F6',[System.Globalization.CultureInfo]::InvariantCulture))</td><td>$($r.Path)</td></tr>`n"
}

$html += "</table></body></html>"

$out = Join-Path $dataRoot "GaugeRR_Report_Sim.html"
Set-Content -Path $out -Value $html -Encoding UTF8
Write-Output "Wrote HTML report: $out"
Write-Output $out
