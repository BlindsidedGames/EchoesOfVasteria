param(
    [Parameter(Mandatory = $true)][string]$Filter,
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe',
    [string]$Output = 'Artifacts\AutomatedTests',
    [int]$TimeoutSeconds = 900
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
# These fixtures own disposable save roots and the runner's empty scene.
if (-not $Filter.StartsWith('Tests.PlayMode.FieldsProductionPlayModeTests')) {
    throw 'Use an isolated FieldsProductionPlayModeTests fixture filter.'
}
$running = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {
    $_.CommandLine -and $_.CommandLine.Replace('/', '\').Contains($project)
}
if ($running) { throw 'Close this project in Unity before running batch tests. Preserve unsaved edits first.' }
if (-not [IO.Path]::IsPathRooted($Output)) { $Output = Join-Path $project $Output }
New-Item -ItemType Directory -Force $Output | Out-Null
$outputPath = (Resolve-Path $Output).Path
$settings = Join-Path $project 'ProjectSettings\EditorSettings.asset'
$settingsBytes = [IO.File]::ReadAllBytes($settings)
$xml = Join-Path $outputPath 'results.xml'
$log = Join-Path $outputPath 'editor.log'
if (Test-Path -LiteralPath $xml) { Remove-Item -LiteralPath $xml }
try {
    # Fast play mode retains test-framework state. A batch test run needs a fresh domain.
    $text = [Text.Encoding]::UTF8.GetString($settingsBytes)
    $text = [regex]::Replace($text, '(?m)^(\s*m_EnterPlayModeOptions:)\s*\d+', '$1 0')
    [IO.File]::WriteAllText($settings, $text, [Text.UTF8Encoding]::new($false))
    $arguments = @('-batchmode', '-projectPath', ('"' + $project + '"'), '-runTests',
        '-testPlatform', 'PlayMode', '-testFilter', ('"' + $Filter + '"'),
        '-testResults', ('"' + $xml + '"'), '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $process.Id
        throw "Unity test run timed out; see $log"
    }
    $process.Refresh()
    if (-not (Test-Path -LiteralPath $xml)) { throw "Unity produced no results (exit $($process.ExitCode)); see $log" }
    [xml]$report = Get-Content -LiteralPath $xml -Raw
    $cases = @($report.SelectNodes('//test-case'))
    $failures = @($report.SelectNodes('//failure'))
    $passed = @($cases | Where-Object result -eq 'Passed').Count
    $summary = [ordered]@{ processExit = $process.ExitCode; result = $report.'test-run'.result;
        total = $cases.Count; passed = $passed; failures = $failures.Count; filter = $Filter }
    $summary | ConvertTo-Json | Set-Content (Join-Path $outputPath 'summary.json')
    $summary | ConvertTo-Json
    if ($process.ExitCode -ne 0 -or $report.'test-run'.result -ne 'Passed' -or $cases.Count -eq 0 -or $passed -eq 0 -or $failures.Count -ne 0) {
        throw "Automated tests did not pass; see $xml"
    }
}
finally { [IO.File]::WriteAllBytes($settings, $settingsBytes) }
