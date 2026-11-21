#requires -Version 7
<#
.SYNOPSIS
    #254: does a UIA search fail (E_FAIL) while a page leaves an AvonicNav, and does it without the nav?

.DESCRIPTION
    Builds UiaRepro (a window whose pages change, with Avalonia's UIA provider hooked and logged), UiaClient (a UIA client
    that changes the pages through Invoke and searches the window while they change), and the Debug desktop sample, then runs
    each scenario below: the client starts the app, drives it, logs every search, and closes it. No mouse, no keys, no adb; the
    app's window does come to the front when it starts, so take the desktop's lease first.

    Scenarios (the name is the log folder's):
      sample-*         the sample's ion-nav page, its links invoked in turn, as drive.ps1's section clicks them
      nav-*            UiaRepro's AvonicNav: "Push a page", "Pop this page", md's transition
      nav-unanimated   the same with no transition
      nav-plain        pages of stock Buttons and TextBlocks instead of Avonic's items
      hide-*           no nav: a plain Panel whose top child's IsVisible flips
      remove-*         no nav: the same child removed from the Panel and added back
      *-auto           the app changes itself on a timer, and the client only searches, for 20 s
      fix-*            UiaRepro --fix: the hooked provider answers a null fragment root with UIA_E_ELEMENTNOTAVAILABLE
                       (notavailable) or with the window's root (windowroot) instead
    The api part: managed = System.Windows.Automation's FindAll, as drive.ps1; com = IUIAutomation.FindAll; com-cache =
    FindAllBuildCache; *-walk = the raw view walked one element at a time, so that the failing step shows; listen = managed,
    with a StructureChanged handler on the window.

    Logs go to -Out (default R:/source/Avonic-wt/nav-uia-logs/<time>): per scenario client-<name>.log (each search),
    app-events.log (what the app did, anomalies, first-chance exceptions) and app-dump-NN.log (the provider calls before each
    anomaly or failed search). summary.txt has a line per scenario.

.EXAMPLE
    pwsh -NoProfile -File R:/source/Avonic-wt/nav-uia/tools/real-window/uia-findall/run.ps1
.EXAMPLE
    pwsh -NoProfile -File R:/source/Avonic-wt/nav-uia/tools/real-window/uia-findall/run.ps1 -Only 'hide-*,nav-managed' -Rounds 10
#>
param(
    [string]$Out = '',
    [string]$Only = '*',
    [int]$Rounds = 24,
    [int]$PollMs = 1500,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$repo = (Resolve-Path (Join-Path $here '../../..')).Path
if (-not $Out) { $Out = Join-Path (Split-Path $repo -Parent) ("nav-uia-logs/" + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force $Out | Out-Null

$reproExe = Join-Path $here 'UiaRepro/bin/Debug/net8.0/UiaRepro.exe'
$clientExe = Join-Path $here 'UiaClient/bin/Debug/net10.0-windows/UiaClient.exe'
$sampleExe = Join-Path $repo 'samples/Avonic.Sample.Desktop/bin/Debug/net10.0/Avonic.Sample.Desktop.exe'

if (-not $NoBuild) {
    foreach ($proj in @((Join-Path $here 'UiaRepro/UiaRepro.csproj'), (Join-Path $here 'UiaClient/UiaClient.csproj'),
                        (Join-Path $repo 'samples/Avonic.Sample.Desktop/Avonic.Sample.Desktop.csproj'))) {
        Write-Host "build $proj"
        dotnet build $proj -c Debug -v q -nologo | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "the build of $proj failed" }
    }
}

# The hooks install and see a call, with no window.
$check = Join-Path $Out 'hooks-check'
& $reproExe --hooks-only --log $check
if ($LASTEXITCODE -ne 0) { throw "UiaRepro's hooks didn't install: see $check" }

$sampleCycle = 'Open message: Lunch*|Write a reply|Discard the draft|Back to inbox|Push a draft|Push a draft|Pop to root|Open settings|Start over at a new inbox'
$navCycle = 'Push a page|Pop this page'
$scenarios = @(
    @{ Name = 'sample-managed';   Exe = $sampleExe; Args = '';                         Api = 'managed';      Triggers = $sampleCycle; Sample = $true }
    @{ Name = 'sample-com';       Exe = $sampleExe; Args = '';                         Api = 'com';          Triggers = $sampleCycle; Sample = $true }
    @{ Name = 'sample-com-walk';  Exe = $sampleExe; Args = '';                         Api = 'com-walk';     Triggers = $sampleCycle; Sample = $true }
    @{ Name = 'nav-managed';      Exe = $reproExe;  Args = '--mode nav';               Api = 'managed';      Triggers = $navCycle }
    @{ Name = 'nav-com';          Exe = $reproExe;  Args = '--mode nav';               Api = 'com';          Triggers = $navCycle }
    @{ Name = 'nav-com-cache';    Exe = $reproExe;  Args = '--mode nav';               Api = 'com-cache';    Triggers = $navCycle }
    @{ Name = 'nav-com-walk';     Exe = $reproExe;  Args = '--mode nav';               Api = 'com-walk';     Triggers = $navCycle }
    @{ Name = 'nav-managed-walk'; Exe = $reproExe;  Args = '--mode nav';               Api = 'managed-walk'; Triggers = $navCycle }
    @{ Name = 'nav-listen';       Exe = $reproExe;  Args = '--mode nav';               Api = 'managed';      Triggers = $navCycle; Listen = $true }
    @{ Name = 'nav-unanimated';   Exe = $reproExe;  Args = '--mode nav --unanimated';  Api = 'managed';      Triggers = $navCycle }
    @{ Name = 'nav-plain';        Exe = $reproExe;  Args = '--mode nav --content plain'; Api = 'managed';    Triggers = $navCycle }
    @{ Name = 'hide-managed';     Exe = $reproExe;  Args = '--mode hide';              Api = 'managed';      Triggers = 'Toggle' }
    @{ Name = 'hide-com-walk';    Exe = $reproExe;  Args = '--mode hide';              Api = 'com-walk';     Triggers = 'Toggle' }
    @{ Name = 'remove-managed';   Exe = $reproExe;  Args = '--mode remove';            Api = 'managed';      Triggers = 'Toggle' }
    @{ Name = 'remove-com-walk';  Exe = $reproExe;  Args = '--mode remove';            Api = 'com-walk';     Triggers = 'Toggle' }
    @{ Name = 'hide-auto';        Exe = $reproExe;  Args = '--mode hide --auto 150';   Api = 'managed';      Triggers = ''; Idle = 20000 }
    @{ Name = 'remove-auto';      Exe = $reproExe;  Args = '--mode remove --auto 150'; Api = 'managed';      Triggers = ''; Idle = 20000 }
    @{ Name = 'nav-auto';         Exe = $reproExe;  Args = '--mode nav --auto 400';    Api = 'managed';      Triggers = ''; Idle = 20000 }
    # What the provider could answer instead of a null fragment root (UiaRepro --fix), to see what UIA makes of each.
    @{ Name = 'fix-notavailable-hide-auto';   Exe = $reproExe; Args = '--mode hide --auto 150 --fix notavailable';   Api = 'managed'; Triggers = ''; Idle = 20000 }
    @{ Name = 'fix-notavailable-remove-auto'; Exe = $reproExe; Args = '--mode remove --auto 150 --fix notavailable'; Api = 'managed'; Triggers = ''; Idle = 20000 }
    @{ Name = 'fix-notavailable-nav-managed'; Exe = $reproExe; Args = '--mode nav --fix notavailable';               Api = 'managed'; Triggers = $navCycle }
    @{ Name = 'fix-notavailable-nav-com';     Exe = $reproExe; Args = '--mode nav --fix notavailable';               Api = 'com';     Triggers = $navCycle }
    @{ Name = 'fix-windowroot-hide-auto';     Exe = $reproExe; Args = '--mode hide --auto 150 --fix windowroot';     Api = 'managed'; Triggers = ''; Idle = 20000 }
    @{ Name = 'fix-windowroot-remove-auto';   Exe = $reproExe; Args = '--mode remove --auto 150 --fix windowroot';   Api = 'managed'; Triggers = ''; Idle = 20000 }
    @{ Name = 'fix-windowroot-nav-managed';   Exe = $reproExe; Args = '--mode nav --fix windowroot';                 Api = 'managed'; Triggers = $navCycle }
    @{ Name = 'fix-windowroot-nav-com';       Exe = $reproExe; Args = '--mode nav --fix windowroot';                 Api = 'com';     Triggers = $navCycle }
)

$patterns = $Only -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
$summary = Join-Path $Out 'summary.txt'
"#254 run at $(Get-Date -Format o) on $env:COMPUTERNAME; rounds $Rounds, poll $PollMs ms" | Set-Content $summary
foreach ($s in $scenarios) {
    if (-not ($patterns | Where-Object { $s.Name -like $_ })) { continue }
    $dir = Join-Path $Out $s.Name
    New-Item -ItemType Directory -Force $dir | Out-Null
    $appArgs = $s.Args
    if ($s.Exe -eq $reproExe) { $appArgs = "$appArgs --log `"$dir`"" }
    $clientArgs = @('--exe', $s.Exe, '--args', $appArgs, '--log', $dir, '--name', $s.Name, '--api', $s.Api,
                    '--triggers', $s.Triggers, '--rounds', $Rounds, '--poll-ms', $(if ($s.Idle) { $s.Idle } else { $PollMs }))
    if ($s.Sample) { $clientArgs += '--sample' }
    if ($s.Listen) { $clientArgs += '--listen' }
    Write-Host "== $($s.Name)"
    $line = & $clientExe @clientArgs
    if ($LASTEXITCODE -ne 0) { $line = "$($s.Name): the client failed with $LASTEXITCODE" }
    $events = Join-Path $dir 'app-events.log'
    if (Test-Path $events) {
        $text = Get-Content $events
        $anomalies = @($text | Where-Object { $_ -match ' ANOMALY ' }).Count
        $exceptions = @($text | Where-Object { $_ -match ' (EXCEPTION|FIRST-CHANCE) ' }).Count
        $line = "$line; app: $anomalies anomalies, $exceptions exceptions"
    }
    Write-Host "   $line"
    $line | Add-Content $summary
}

Write-Host ""
Write-Host "Logs: $Out"
Get-Content $summary | Write-Host
