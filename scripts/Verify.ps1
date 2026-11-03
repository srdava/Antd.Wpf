#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repository = Split-Path $PSScriptRoot -Parent

function Invoke-DotNet {
    param([string[]] $Arguments)
    if ($Arguments[0] -in @('build', 'test', 'pack', 'restore', 'publish')) {
        $Arguments += '-bl:artifacts/verify-{}.binlog'
    }
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed ($LASTEXITCODE): $($Arguments -join ' ')"
    }
}

Push-Location $repository
try {
    # Catch source truncation before building; dotnet format is unreliable on this host.
    $emptySources = @(Get-ChildItem src, samples, tests -Filter '*.cs' -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Length -eq 0 })
    if ($emptySources.Count -gt 0) {
        throw "Empty C# sources: $($emptySources.FullName -join ', ')"
    }

    Invoke-DotNet -Arguments @('build', 'Antd.Wpf.sln', '-c', 'Release', '--no-incremental', '-v', 'minimal')
    foreach ($framework in @('net48', 'net8.0-windows')) {
        Invoke-DotNet -Arguments @('test', 'Antd.Wpf.sln', '-c', 'Release', '-f', $framework, '--no-build', '-v', 'minimal',
            '--logger', "trx;LogFileName=verified-$framework.trx", '--results-directory', 'artifacts/test-results')
    }
    Invoke-DotNet -Arguments @('pack', 'src/Antd.Wpf/Antd.Wpf.csproj', '-c', 'Release', '--no-build', '-o', 'artifacts/packages')

    $package = Join-Path $repository 'artifacts/packages/Antd.Wpf.0.1.0-alpha.1.nupkg'
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package)
    try {
        foreach ($asset in @('README.md', 'THIRD-PARTY-NOTICES.txt', 'lib/net48/Antd.Wpf.dll', 'lib/net8.0-windows7.0/Antd.Wpf.dll')) {
            if ($null -eq $archive.GetEntry($asset)) {
                throw "Package missing $asset"
            }
        }
        $noticeStream = $archive.GetEntry('THIRD-PARTY-NOTICES.txt').Open()
        $noticeHasher = [System.Security.Cryptography.SHA256]::Create()
        try {
            $noticeHash = [BitConverter]::ToString($noticeHasher.ComputeHash($noticeStream)).Replace('-', '')
            if ($noticeHash -ne (Get-FileHash -LiteralPath 'src/Antd.Wpf/THIRD-PARTY-NOTICES.txt' -Algorithm SHA256).Hash) {
                throw 'Packaged third-party notices differ from source'
            }
        }
        finally { $noticeStream.Dispose(); $noticeHasher.Dispose() }
    }
    finally {
        $archive.Dispose()
    }

    # Each package content hash has an isolated cache, so an older alpha cannot mask a new build.
    $packageHash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.Substring(0, 16)
    $cache = Join-Path $repository "artifacts/consumer-packages/$packageHash"
    Invoke-DotNet -Arguments @('build', 'tests/Consumers/Antd.Wpf.Consumer/Antd.Wpf.Consumer.csproj', '-c', 'Release',
        "-p:RestorePackagesPath=$cache", '-v', 'minimal')

    foreach ($framework in @('net48', 'net481')) {
        $outputDirectory = Join-Path $repository 'artifacts/consumer-output'
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
        $output = Join-Path $outputDirectory "$framework.log"
        $errorOutput = Join-Path $outputDirectory "$framework.error.log"
        $consumer = Start-Process -FilePath (Join-Path $repository "tests/Consumers/Antd.Wpf.Consumer/bin/Release/$framework/Antd.Wpf.Consumer.exe") `
            -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $output -RedirectStandardError $errorOutput
        if ($consumer.ExitCode -ne 0 -or (Get-Content -LiteralPath $output -Raw) -notmatch 'PASS: packaged BAML') {
            throw "Package consumer failed: $framework"
        }
        Write-Host "$framework : $(Get-Content -LiteralPath $output -Raw)"
    }
    foreach ($framework in @('net8.0-windows', 'net10.0-windows')) {
        Invoke-DotNet -Arguments @("tests/Consumers/Antd.Wpf.Consumer/bin/Release/$framework/Antd.Wpf.Consumer.dll")
    }

    Invoke-DotNet -Arguments @('samples/Antd.Wpf.Gallery/bin/Release/net8.0-windows/Antd.Wpf.Gallery.dll',
        '--capture', (Join-Path $repository 'artifacts/gallery-net8.png'))
    $capture = Start-Process -FilePath (Join-Path $repository 'samples/Antd.Wpf.Gallery/bin/Release/net48/Antd.Wpf.Gallery.exe') `
        -ArgumentList @('--capture', ('"{0}"' -f (Join-Path $repository 'artifacts/gallery-net48.png'))) `
        -WindowStyle Hidden -Wait -PassThru
    if ($capture.ExitCode -ne 0) {
        throw 'Framework Gallery capture failed'
    }
    Invoke-DotNet -Arguments @('samples/Antd.Wpf.Gallery/bin/Release/net8.0-windows/Antd.Wpf.Gallery.dll',
        '--capture-matrix', (Join-Path $repository 'artifacts/visual-matrix-net8.png'))
    $matrixCapture = Start-Process -FilePath (Join-Path $repository 'samples/Antd.Wpf.Gallery/bin/Release/net48/Antd.Wpf.Gallery.exe') `
        -ArgumentList @('--capture-matrix', ('"{0}"' -f (Join-Path $repository 'artifacts/visual-matrix-net48.png'))) `
        -WindowStyle Hidden -Wait -PassThru
    if ($matrixCapture.ExitCode -ne 0) { throw 'Framework visual matrix capture failed' }
    Invoke-DotNet -Arguments @('samples/Antd.Wpf.Gallery/bin/Release/net8.0-windows/Antd.Wpf.Gallery.dll',
        '--capture-divider', (Join-Path $repository 'artifacts/divider-matrix-net8.png'))
    $dividerCapture = Start-Process -FilePath (Join-Path $repository 'samples/Antd.Wpf.Gallery/bin/Release/net48/Antd.Wpf.Gallery.exe') `
        -ArgumentList @('--capture-divider', ('"{0}"' -f (Join-Path $repository 'artifacts/divider-matrix-net48.png'))) `
        -WindowStyle Hidden -Wait -PassThru
    if ($dividerCapture.ExitCode -ne 0) { throw 'Framework divider matrix capture failed' }
    Invoke-DotNet -Arguments @('samples/Antd.Wpf.Gallery/bin/Release/net8.0-windows/Antd.Wpf.Gallery.dll',
        '--capture-flex', (Join-Path $repository 'artifacts/flex-matrix-net8.png'))
    $flexCapture = Start-Process -FilePath (Join-Path $repository 'samples/Antd.Wpf.Gallery/bin/Release/net48/Antd.Wpf.Gallery.exe') `
        -ArgumentList @('--capture-flex', ('"{0}"' -f (Join-Path $repository 'artifacts/flex-matrix-net48.png'))) `
        -WindowStyle Hidden -Wait -PassThru
    if ($flexCapture.ExitCode -ne 0) { throw 'Framework flex matrix capture failed' }
    Invoke-DotNet -Arguments @('samples/Antd.Wpf.Gallery/bin/Release/net8.0-windows/Antd.Wpf.Gallery.dll',
        '--capture-space', (Join-Path $repository 'artifacts/space-matrix-net8.png'))
    $spaceCapture = Start-Process -FilePath (Join-Path $repository 'samples/Antd.Wpf.Gallery/bin/Release/net48/Antd.Wpf.Gallery.exe') `
        -ArgumentList @('--capture-space', ('"{0}"' -f (Join-Path $repository 'artifacts/space-matrix-net48.png'))) `
        -WindowStyle Hidden -Wait -PassThru
    if ($spaceCapture.ExitCode -ne 0) { throw 'Framework space matrix capture failed' }
    Invoke-DotNet -Arguments @('samples/Antd.Wpf.Gallery/bin/Release/net8.0-windows/Antd.Wpf.Gallery.dll',
        '--capture-grid', (Join-Path $repository 'artifacts/grid-matrix-net8.png'))
    $gridCapture = Start-Process -FilePath (Join-Path $repository 'samples/Antd.Wpf.Gallery/bin/Release/net48/Antd.Wpf.Gallery.exe') `
        -ArgumentList @('--capture-grid', ('"{0}"' -f (Join-Path $repository 'artifacts/grid-matrix-net48.png'))) `
        -WindowStyle Hidden -Wait -PassThru
    if ($gridCapture.ExitCode -ne 0) { throw 'Framework grid matrix capture failed' }
    Invoke-DotNet -Arguments @('samples/Antd.Wpf.Gallery/bin/Release/net8.0-windows/Antd.Wpf.Gallery.dll',
        '--capture-layout', (Join-Path $repository 'artifacts/layout-matrix-net8.png'))
    $layoutCapture = Start-Process -FilePath (Join-Path $repository 'samples/Antd.Wpf.Gallery/bin/Release/net48/Antd.Wpf.Gallery.exe') `
        -ArgumentList @('--capture-layout', ('"{0}"' -f (Join-Path $repository 'artifacts/layout-matrix-net48.png'))) `
        -WindowStyle Hidden -Wait -PassThru
    if ($layoutCapture.ExitCode -ne 0) { throw 'Framework layout matrix capture failed' }

    $testResults = foreach ($framework in @('net48', 'net8.0-windows')) {
        $trxPath = "artifacts/test-results/verified-$framework.trx"
        [xml] $trx = Get-Content -LiteralPath $trxPath -Raw
        $counts = $trx.TestRun.ResultSummary.Counters
        if ([int]$counts.total -eq 0 -or [int]$counts.passed -ne [int]$counts.total) {
            throw "TRX has missing, failed or skipped results: $trxPath"
        }
        [ordered]@{ target = $framework; passed = [int]$counts.passed; total = [int]$counts.total; report = $trxPath }
    }
    $snapshotPath = 'artifacts/performance/verified-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
    New-Item -ItemType Directory -Path $snapshotPath | Out-Null
    Copy-Item -LiteralPath 'artifacts/performance/m0', 'artifacts/performance/popup-comparison', 'artifacts/performance/divider', 'artifacts/performance/password' -Destination $snapshotPath -Recurse
    $performance = foreach ($framework in @('net48', 'net8.0-windows')) {
        foreach ($sceneFolder in @('m0', 'divider', 'password')) {
            $csvPath = "$snapshotPath/$sceneFolder/$framework/samples.csv"
            foreach ($group in (Import-Csv -LiteralPath $csvPath | Group-Object scenario)) {
                $times = @($group.Group | ForEach-Object { [double]$_.milliseconds } | Sort-Object)
                if ($times.Count -ne 30) { throw "Incomplete performance samples: $csvPath" }
                $budget = if ($group.Name -like 'PF02.*') { 200 } else { 300 }
                $p95 = $times[[int][Math]::Ceiling($times.Count * 0.95) - 1]
                [ordered]@{
                    target = $framework; scenario = $group.Name; samples = $times.Count
                    p50Milliseconds = $times[[int][Math]::Ceiling($times.Count * 0.5) - 1]
                    p95Milliseconds = $p95; provisionalBudgetMilliseconds = $budget; proxyWithinBudget = $p95 -le $budget
                    maxMilliseconds = $times[-1]; raw = $csvPath
                    environment = "$snapshotPath/$sceneFolder/$framework/environment.txt"
                    qualification = 'Hidden-window layout/Dispatcher proxy; not full PF acceptance'
                }
            }
        }
    }
    $sourceFiles = @(Get-ChildItem src, samples, tests -Recurse -File | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in @('.cs', '.xaml', '.csproj')
    } | Sort-Object FullName)
    $sourceSnapshot = @($sourceFiles | ForEach-Object {
        [ordered]@{ path = [IO.Path]::GetRelativePath($repository, $_.FullName); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
    $report = [ordered]@{
        validatedAt = (Get-Date).ToString('o'); command = 'pwsh -File scripts/Verify.ps1'; exitCode = 0
        scope = 'M0, P1 native selection/layout, Switch label typography/thumb motion, Input search dispatch/cancellation, count/length hints and multiline row bounds/scrolling, Flex wrapping/distributed justification, manual Sider collapse and Gallery scrolling/compact-navigation regression; not full catalog or release acceptance'
        sdk = (& dotnet --version); machine = $env:COMPUTERNAME
        tests = @($testResults); package = 'artifacts/packages/Antd.Wpf.0.1.0-alpha.1.nupkg'
        packageSha256 = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
        thirdPartyNotices = [ordered]@{ source = 'src/Antd.Wpf/THIRD-PARTY-NOTICES.txt'; sha256 = $noticeHash; packagedBytesMatchSource = $true }
        consumerTargetsPassed = @('net48', 'net481', 'net8.0-windows', 'net10.0-windows')
        captures = @('artifacts/gallery-net48.png', 'artifacts/gallery-net8.png', 'artifacts/visual-matrix-net48.png', 'artifacts/visual-matrix-net8.png',
            'artifacts/divider-matrix-net48.png', 'artifacts/divider-matrix-net8.png', 'artifacts/flex-matrix-net48.png', 'artifacts/flex-matrix-net8.png',
            'artifacts/space-matrix-net48.png', 'artifacts/space-matrix-net8.png', 'artifacts/grid-matrix-net48.png', 'artifacts/grid-matrix-net8.png',
            'artifacts/layout-matrix-net48.png', 'artifacts/layout-matrix-net8.png')
        performance = @($performance); popupComparisonDirectory = "$snapshotPath/popup-comparison"; sourceSnapshot = $sourceSnapshot
        limitations = @(
            'Framework consumers share the installed CLR 4.x runtime; not separate OS installations.',
            'Earlier PF05 runs exceeded 300ms; current proxy results do not explain or close the intermittent issue.',
            'PF01/PF03/PF04/PF06-PF10, physical keyboard/IME, DPI matrix, screen reader, x86 and heap-root investigation remain unverified.',
            'Switch has 200ms thumb motion with cancellation; color/label transitions, pressed-handle stretch, shadow and busy rotation remain pending. Hardware frame-time/GC acceptance is unverified.'
            'Flex supports optional horizontal/vertical wrapping and distributed justification; Grow/Shrink/Basis/AlignSelf, wrap-reverse, align-content selection and baseline remain pending. No Flex wall-time/GC budget is claimed.'
            'Space uses decorative separators, suppresses separators at wrap boundaries, and does not implement Compact or baseline. No Space wall-time/GC budget is claimed.'
            'Grid implements base spans/offsets/gutters/alignment; responsive breakpoints, Order/Push/Pull/Grow are pending. No Grid wall-time/GC budget is claimed.'
            'Layout uses native docking, container-relative triggers and immediate manual collapse; responsive collapse, motion and standalone Layout time/GC/heap gates are pending.'
        )
    }
    $reportJson = ($report | ConvertTo-Json -Depth 8).Replace("`r`n", "`n") + "`n"
    [IO.File]::WriteAllText((Join-Path $repository 'docs/m0-validation.json'), $reportJson, [Text.UTF8Encoding]::new($false))
    Write-Host 'PASS: solution, dual-target tests, package assets, four consumers and Gallery captures.'
}
finally {
    Pop-Location
}
