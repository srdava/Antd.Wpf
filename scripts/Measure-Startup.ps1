#Requires -Version 7.0
param(
    [ValidateSet('net48', 'net8.0-windows')][string] $Target = 'net8.0-windows',
    [ValidateRange(10, 100)][int] $Pairs = 10,
    [string] $OutputDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repository = Split-Path $PSScriptRoot -Parent
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repository ("artifacts/performance/startup/{0}/{1}" -f $Target, (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$galleryDirectory = Join-Path $repository "samples/Antd.Wpf.Gallery/bin/Release/$Target"
$gallery = Join-Path $galleryDirectory $(if ($Target -eq 'net48') { 'Antd.Wpf.Gallery.exe' } else { 'Antd.Wpf.Gallery.dll' })
if (-not (Test-Path -LiteralPath $gallery -PathType Leaf)) { throw "Build Release Gallery before measuring: $gallery" }
$library = Join-Path $galleryDirectory 'Antd.Wpf.dll'
$galleryHash = (Get-FileHash -LiteralPath $gallery -Algorithm SHA256).Hash
$libraryHash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash
$rows = [Collections.Generic.List[object]]::new()
$deltas = [Collections.Generic.List[object]]::new()
$frequency = [Diagnostics.Stopwatch]::Frequency

for ($pair = 0; $pair -lt $Pairs; $pair++) {
    $pairRows = @{}
    $modes = if ($pair % 2 -eq 0) { @('empty', 'controls') } else { @('controls', 'empty') }
    foreach ($mode in $modes) {
        $path = Join-Path $OutputDirectory ("pair-{0:D2}-{1}.csv" -f $pair, $mode)
        $startInfo = [Diagnostics.ProcessStartInfo]::new()
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.WorkingDirectory = $repository
        if ($Target -eq 'net48') { $startInfo.FileName = $gallery }
        else {
            $startInfo.FileName = (Get-Command dotnet -CommandType Application).Source
            $startInfo.ArgumentList.Add($gallery)
        }
        foreach ($argument in @('--measure-startup', $mode, $path)) { $startInfo.ArgumentList.Add($argument) }
        $process = [Diagnostics.Process]::new()
        $process.StartInfo = $startInfo
        $launch = [Diagnostics.Stopwatch]::GetTimestamp()
        try {
            if (-not $process.Start()) { throw 'Could not start Gallery probe' }
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(15000)) { $process.Kill($true); $process.WaitForExit(); throw "Startup probe timed out: $mode" }
            $exit = [Diagnostics.Stopwatch]::GetTimestamp()
            $stdout.GetAwaiter().GetResult() | Set-Content -LiteralPath "$path.stdout.txt"
            $stderr.GetAwaiter().GetResult() | Set-Content -LiteralPath "$path.stderr.txt"
            if ($process.ExitCode -ne 0) { throw "Startup probe failed ($($process.ExitCode)): $path.stderr.txt" }
            $samples = @(Import-Csv -LiteralPath $path)
            if ($samples.Count -ne 1) { throw "Incomplete startup sample: $path" }
            $sample = $samples[0]
            $expectedCount = if ($mode -eq 'controls') { 200 } else { 0 }
            $expectedDictionaries = if ($mode -eq 'controls') { 1 } else { 0 }
            if ($sample.mode -ne $mode -or [int]$sample.controlCount -ne $expectedCount -or [int]$sample.processId -ne $process.Id -or
                [long]$sample.qpcFrequency -ne $frequency -or [int]$sample.scopeDictionaries -ne $expectedDictionaries -or
                [int]$sample.visualNodes -lt 1 -or $sample.focusAccepted -notin @('True', 'False')) {
                throw "Invalid startup metadata: $path"
            }
            $startup = [long]$sample.startupQpc; $loaded = [long]$sample.loadedQpc
            $rendering = [long]$sample.renderingQpc; $inputDispatch = [long]$sample.inputDispatchQpc
            if ($startup -lt $launch -or $loaded -lt $startup -or $rendering -lt $loaded -or $inputDispatch -lt $loaded -or
                $rendering -gt $exit -or $inputDispatch -gt $exit) { throw "Invalid checkpoint order: $path" }
            $assemblies = @(Import-Csv -LiteralPath ([IO.Path]::ChangeExtension($path, 'assemblies.csv')))
            $libraryAssemblies = @($assemblies | Where-Object { $_.assembly -eq 'Antd.Wpf' })
            if (($mode -eq 'empty' -and $libraryAssemblies.Count -ne 0) -or
                ($mode -eq 'controls' -and @($libraryAssemblies | Where-Object stage -eq 'ready').Count -ne 1)) {
                throw "Empty/themed assembly isolation failed: $path"
            }
            $row = [pscustomobject][ordered]@{
                pair = $pair; mode = $mode; processId = $process.Id; launchQpc = $launch; exitQpc = $exit
                startupMilliseconds = ($startup - $launch) * 1000.0 / $frequency
                loadedMilliseconds = ($loaded - $launch) * 1000.0 / $frequency
                renderingMilliseconds = ($rendering - $launch) * 1000.0 / $frequency
                inputDispatchMilliseconds = ($inputDispatch - $launch) * 1000.0 / $frequency
                processDurationMilliseconds = ($exit - $launch) * 1000.0 / $frequency
                focusAccepted = [bool]::Parse($sample.focusAccepted); raw = [IO.Path]::GetFileName($path)
            }
            $rows.Add($row); $pairRows[$mode] = $row
        }
        finally { $process.Dispose() }
    }
    $deltas.Add([pscustomobject][ordered]@{
        pair = $pair
        loadedDeltaMilliseconds = $pairRows.controls.loadedMilliseconds - $pairRows.empty.loadedMilliseconds
        renderingDeltaMilliseconds = $pairRows.controls.renderingMilliseconds - $pairRows.empty.renderingMilliseconds
        inputDispatchDeltaMilliseconds = $pairRows.controls.inputDispatchMilliseconds - $pairRows.empty.inputDispatchMilliseconds
    })
}

function Get-Distribution([object[]] $Values) {
    $ordered = @($Values | Sort-Object)
    if ($ordered.Count -ne $Pairs) { throw 'Incomplete startup distribution' }
    [ordered]@{
        samples = $ordered.Count
        p50Milliseconds = $ordered[[int][Math]::Ceiling($ordered.Count * 0.5) - 1]
        p95Milliseconds = $ordered[[int][Math]::Ceiling($ordered.Count * 0.95) - 1]
        maxMilliseconds = $ordered[-1]
    }
}
$rows | Export-Csv -LiteralPath (Join-Path $OutputDirectory 'samples.csv') -NoTypeInformation
$deltas | Export-Csv -LiteralPath (Join-Path $OutputDirectory 'paired-deltas.csv') -NoTypeInformation
$inputDeltas = Get-Distribution @($deltas | ForEach-Object inputDispatchDeltaMilliseconds)
if ((Get-FileHash -LiteralPath $gallery -Algorithm SHA256).Hash -ne $galleryHash -or
    (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash -ne $libraryHash) { throw 'Measured binaries changed during startup sampling' }
$summary = [ordered]@{
    target = $Target; scenario = 'PF01.fresh-process-startup-proxy'; pairs = $Pairs; independentProcesses = $rows.Count
    empty = [ordered]@{}; controls = [ordered]@{}
    pairedLoadedDelta = Get-Distribution @($deltas | ForEach-Object loadedDeltaMilliseconds)
    pairedRenderingDelta = Get-Distribution @($deltas | ForEach-Object renderingDeltaMilliseconds)
    pairedInputDispatchDelta = $inputDeltas
    provisionalBudgetMilliseconds = 300; pairedInputDispatchProxyWithinBudget = $inputDeltas.p95Milliseconds -le 300
    focusAcceptedSamples = @($rows | Where-Object focusAccepted).Count
    gallerySha256 = $galleryHash; librarySha256 = $libraryHash
    runnerSha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
    machine = $env:COMPUTERNAME; os = [Environment]::OSVersion.ToString(); processors = [Environment]::ProcessorCount
    cpu = @((Get-CimInstance Win32_Processor).Name); gpu = @((Get-CimInstance Win32_VideoController).Name)
    qpcFrequency = $frequency
    qualification = 'Fresh independent processes, alternating paired order; OS caches not flushed; hidden-window Rendering and Input Dispatcher proxies, not GPU presentation or physical interactivity/complete PF01 acceptance. Negative deltas retained.'
}
foreach ($mode in @('empty', 'controls')) {
    $modeRows = @($rows | Where-Object mode -eq $mode)
    foreach ($checkpoint in @('startup', 'loaded', 'rendering', 'inputDispatch')) {
        $summary[$mode][$checkpoint] = Get-Distribution @($modeRows | ForEach-Object { $_.("${checkpoint}Milliseconds") })
    }
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'summary.json')
Write-Host "MEASURED: $Target, $Pairs pairs/$($rows.Count) processes, paired Input Dispatcher delta p95=$($inputDeltas.p95Milliseconds) ms, proxyWithinBudget=$($summary.pairedInputDispatchProxyWithinBudget); full PF01 acceptance remains open."
