param([string]$report)
if (-not $report) { $report = 'P:\MGunes\DuplexerTestSuite\GaugeRR\GaugeRR_03_06_2026\GaugeRR_Report_ManualChamberSetings_example_20260603_165029.html' }
$html=Get-Content -Raw -Path $report
# Limit parsing to the Raw Measurement Traceability table to avoid matching summary numbers
$marker = 'Raw Measurement Traceability'
$idx = $html.IndexOf($marker)
if ($idx -ge 0) { $html = $html.Substring($idx) }
$re='<tr>.*?<td>(?:Base|Remote)</td>.*?<td>.*?</td>.*?<td>.*?</td>.*?<td>.*?</td>.*?<td>.*?</td>.*?<td>(?<value>[0-9]+\.[0-9]+)</td>.*?<td>(?<path>[^<]+?\.csv)</td>.*?</tr>'
$matches=[System.Text.RegularExpressions.Regex]::Matches($html,$re,[System.Text.RegularExpressions.RegexOptions]::Singleline)
$results=@()
foreach ($m in $matches) {
    $v = $m.Groups['value'].Value
    $p = $m.Groups['path'].Value
    $exists = Test-Path $p
    $contains = $false
    $meanStr = ''
    $reportedNum = ''
    if ($exists) {
        try {
            $lines = Get-Content -Path $p -ErrorAction Stop
            if ($lines.Count -gt 1) {
                $header = $lines[0]
                $cols = $header -split ','
                $idx = [array]::IndexOf($cols, 'CH1 Voltage(V)')
                if ($idx -lt 0) { $idx = 0 }
                $vals = @()
                for ($i = 1; $i -lt $lines.Count; $i++) {
                    if ([string]::IsNullOrWhiteSpace($lines[$i])) { continue }
                    $parts = $lines[$i] -split ','
                    if ($parts.Length -gt $idx) {
                        $d = 0.0
                        if ([double]::TryParse($parts[$idx], [ref]$d)) { $vals += $d }
                    }
                }
                if ($vals.Count -gt 0) {
                    $mean = ($vals | Measure-Object -Average).Average
                    $meanStr = [Math]::Round($mean,6).ToString('F6', [System.Globalization.CultureInfo]::InvariantCulture)
                    # Compare numerically within tolerance
                    $reported = 0.0
                    [double]::TryParse($v, [System.Globalization.NumberStyles]::Any, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$reported) | Out-Null
                    $reportedNum = [Math]::Round($reported,6).ToString('F6', [System.Globalization.CultureInfo]::InvariantCulture)
                    $contains = [math]::Abs($mean - $reported) -lt 0.0005
                }
            }
        } catch { }
    }
    $results += [PSCustomObject]@{ Value = $v; Path = $p; Exists = $exists; MeanMatches = $contains; CSVMean = $meanStr; ReportedMean = $reportedNum }
}
$results | Format-Table -AutoSize
$out='D:\VSCodeRepo\DuplexerFinalTest\gauge_rr_csv_check.csv'
$results | ConvertTo-Csv -NoTypeInformation | Out-File -FilePath $out -Encoding UTF8
Write-Output "Saved summary to: $out"
