# Opens the real sample window and uses it with real mouse and keyboard input, and touch through Windows' touch injection.
# Checks what it can, saves a frame of everything, and exits 1 if a check fails. See README.md here.
#
# It takes over the cursor and focus for about a minute, so ask before running it on someone's PC.
# Windows only, and PowerShell 5.1 or later: powershell -ExecutionPolicy Bypass -File tools\real-window\drive.ps1
param(
    # The sample to run. Build it first. A Debug build also has the performance line to check.
    [string]$Exe = (Join-Path $PSScriptRoot '..\..\samples\Avonic.Sample\bin\Debug\net10.0\Avonic.Sample.exe'),
    # Where the frames go. By default a new folder for each run, so that an old frame can't pass for a new one.
    [string]$Out = (Join-Path ([IO.Path]::GetTempPath()) ('avonic-real-window\' + (Get-Date -Format 'yyyyMMdd-HHmmss')))
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Exe)) { throw "No sample at $Exe. Build it first." }
New-Item -ItemType Directory -Force $Out | Out-Null
$Exe = (Resolve-Path $Exe).Path
$debugBuild = $Exe -match '\\Debug\\'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class RealInput {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);
  public static void Down() { mouse_event(2, 0, 0, 0, IntPtr.Zero); }
  public static void Up() { mouse_event(4, 0, 0, 0, IntPtr.Zero); }
  public static void Wheel(int d) { mouse_event(0x800, 0, 0, unchecked((uint)d), IntPtr.Zero); }

  [StructLayout(LayoutKind.Sequential)] struct PT { public int x, y; }
  [StructLayout(LayoutKind.Sequential)] struct RC { public int l, t, r, b; }
  [StructLayout(LayoutKind.Sequential)] struct PI {
    public uint type, id, frame, flags; public IntPtr device, hwnd;
    public PT px, him, pxRaw, himRaw; public uint time, history; public int data; public uint keys;
    public ulong perf; public int change; }
  [StructLayout(LayoutKind.Sequential)] struct TI { public PI p; public uint tf, mask; public RC c, cr; public uint orient, press; }
  [DllImport("user32.dll", SetLastError = true)] static extern bool InitializeTouchInjection(uint n, uint mode);
  [DllImport("user32.dll", SetLastError = true)] static extern bool InjectTouchInput(uint n, ref TI t);
  public static bool TouchInit() { return InitializeTouchInjection(1, 3); }
  // The pointer id has to be below the count given to InitializeTouchInjection.
  static void Send(int x, int y, uint flags) {
    var t = new TI();
    t.p.type = 2; t.p.id = 0; t.p.flags = flags; t.p.px.x = x; t.p.px.y = y;
    t.mask = 7; t.orient = 90; t.press = 32000;
    t.c.l = x - 2; t.c.r = x + 2; t.c.t = y - 2; t.c.b = y + 2;
    if (!InjectTouchInput(1, ref t)) throw new InvalidOperationException("Touch injection failed: " + Marshal.GetLastWin32Error());
  }
  public static void TouchDown(int x, int y) { Send(x, y, 0x10000 | 2 | 4); }
  public static void TouchMove(int x, int y) { Send(x, y, 0x20000 | 2 | 4); }
  public static void TouchUp(int x, int y) { Send(x, y, 0x40000); }
}
'@
[void][RealInput]::SetProcessDPIAware()
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$SendKeys = [System.Windows.Forms.SendKeys]
$cursor = [System.Windows.Forms.Cursor]::Position
$script:failures = 0
$script:checks = 0

function Check($name, [bool]$ok, $detail = '') {
    $script:checks++
    if ($ok) { "  ok    $name" } else { $script:failures++; "  FAIL  $name $detail" }
}

# From the screen, not PrintWindow, which offsets a window with no frame of its own by its invisible border.
function Shot($name) {
    $r = $win.Current.BoundingRectangle
    $bmp = New-Object Drawing.Bitmap ([int]$r.Width), ([int]$r.Height)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
    $g.Dispose()
    $bmp.Save((Join-Path $Out "$name.png"))
    $bmp.Dispose()
}
function Pixel($x, $y) {
    $bmp = New-Object Drawing.Bitmap 1, 1
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]$x, [int]$y, 0, 0, $bmp.Size)
    $g.Dispose()
    $c = $bmp.GetPixel(0, 0).ToArgb()
    $bmp.Dispose()
    $c
}
function All($type) {
    $cond = New-Object Windows.Automation.PropertyCondition $AE::ControlTypeProperty, $type
    $win.FindAll($TS::Descendants, $cond) | ForEach-Object { $_ }
}
function Named($type, $like) { All $type | Where-Object { $_.Current.Name -like $like } | Select-Object -First 1 }
function Texts { (All $CT::Text | ForEach-Object { $_.Current.Name } | Where-Object { $_ }) }
function FirstText($like) { Texts | Where-Object { $_ -like $like } | Select-Object -First 1 }
# Throws rather than aim at the corner of the screen, where a double click could close someone's app.
function Centre($el) {
    if (-not $el) { throw 'Nothing to aim at: the element was not found' }
    $b = $el.Current.BoundingRectangle
    if ([double]::IsInfinity($b.Width) -or [double]::IsInfinity($b.Height) -or [double]::IsNaN($b.Width) -or $b.Width -le 0 -or $b.Height -le 0) { throw "Nothing to aim at: $($el.Current.Name) has no size" }
    [int]($b.X + $b.Width / 2), [int]($b.Y + $b.Height / 2)
}
function Fore { [void][RealInput]::SetForegroundWindow($p.MainWindowHandle); Start-Sleep -Milliseconds 150 }
# Called before any input, so that nothing lands in another app if something else takes the focus.
function Guard {
    if ([RealInput]::GetForegroundWindow() -ne $p.MainWindowHandle) {
        Fore
        if ([RealInput]::GetForegroundWindow() -ne $p.MainWindowHandle) { throw 'The sample is not in front, so no input was sent' }
    }
}
function Away { $r = $win.Current.BoundingRectangle; [void][RealInput]::SetCursorPos(([int]$r.X + 4), ([int]$r.Y + 4)); Start-Sleep -Milliseconds 150 }
function Key($text, $ms = 250) { Guard; $SendKeys::SendWait($text); Start-Sleep -Milliseconds $ms }
function Press { Guard; $script:held = $true; [RealInput]::Down() }
function Release { [RealInput]::Up(); $script:held = $false }
# Lets go of whatever an exception left held, on the user's desktop as well as in the sample.
function LetGo {
    if ($script:held) { Release }
    if ($script:touching) {
        $script:touching = $false
        try { [RealInput]::TouchUp($script:touchX, $script:touchY) } catch { }
    }
}
function TouchDown($x, $y) { Guard; $script:touching = $true; $script:touchX = $x; $script:touchY = $y; [RealInput]::TouchDown($x, $y) }
function TouchUp($x, $y) { [RealInput]::TouchUp($x, $y); $script:touching = $false }
# What a page reports comes after layout, so it can take a moment.
function WaitForText($like, $seconds = 5) {
    $until = (Get-Date).AddSeconds($seconds)
    do {
        $found = FirstText $like
        if ($found) { return $found }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $until)
}
function EndedAt {
    $text = WaitForText 'Ended at*'
    if ($text -match '^Ended at (\d+)$') { [int]$Matches[1] } else { -1 }
}
function Invoke-Element($el) { $el.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Index {
    $back = Named $CT::Button 'Back'
    if ($back) { Invoke-Element $back; Start-Sleep -Milliseconds 300 }
}
function Open($title) {
    Index
    $link = Named $CT::Button $title
    if (-not $link) { throw "$title isn't on the index" }
    Invoke-Element $link
    Start-Sleep -Milliseconds 800
    Fore
}
function Click($x, $y) {
    Guard
    [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 120
    Press; Start-Sleep -Milliseconds 50; Release; Start-Sleep -Milliseconds 250
}
function ClickTwice {
    Press; Start-Sleep -Milliseconds 40; Release; Start-Sleep -Milliseconds 60
    Press; Start-Sleep -Milliseconds 40; Release; Start-Sleep -Milliseconds 300
}
function Tap($x, $y) {
    TouchDown $x $y; Start-Sleep -Milliseconds 40
    [RealInput]::TouchMove($x, $y); Start-Sleep -Milliseconds 40
    TouchUp $x $y; Start-Sleep -Milliseconds 250
}
function Drag($x, $y, $dy, $steps = 15, $ms = 16) {
    TouchDown $x $y
    for ($i = 1; $i -le $steps; $i++) {
        Start-Sleep -Milliseconds $ms
        [RealInput]::TouchMove($x, [int]($y + $dy * $i / $steps))
    }
    TouchUp $x ($y + $dy)
}
function Wheel($notches) { Guard; 1..$notches | ForEach-Object { [RealInput]::Wheel(-120); Start-Sleep -Milliseconds 50 }; Start-Sleep -Milliseconds 350 }
function Section($title, [scriptblock]$body) {
    "== $title"
    try { & $body }
    catch { LetGo; $script:failures++; "  FAIL  the section stopped: $($_.Exception.Message) at line $($_.InvocationInfo.ScriptLineNumber)" }
    if ($p.HasExited) { throw "The app exited during $title, with code $($p.ExitCode)" }
}

$p = Start-Process $Exe -PassThru
try {
    $win = $null
    for ($i = 0; $i -lt 40 -and -not $win; $i++) {
        Start-Sleep -Milliseconds 500
        $p.Refresh()
        if ($p.MainWindowHandle -ne 0) { $win = $AE::FromHandle($p.MainWindowHandle) }
    }
    if (-not $win) { throw 'The window never appeared' }
    Fore
    Start-Sleep -Milliseconds 500
    $r = $win.Current.BoundingRectangle
    $area = [System.Windows.Forms.Screen]::FromHandle($p.MainWindowHandle).WorkingArea
    if ($r.Bottom -gt $area.Bottom -or $r.Right -gt $area.Right -or $r.Y -lt $area.Y -or $r.X -lt $area.X) {
        throw "The window ($r) doesn't fit on the screen ($area), so clicks would miss and frames would be cut off. Lower the display scale, or move it."
    }
    $scale = $r.Width / 390
    # Before starting injection, which adds a contact of its own.
    $contacts = [RealInput]::GetSystemMetrics(95)
    $touch = [RealInput]::TouchInit()
    "$Exe"
    "window $($r.Width) x $($r.Height), scale $([math]::Round($r.Width / 390, 2)), touch hardware contacts $contacts, touch injection $touch"
    "frames in $Out"
    $titles = @(All $CT::Button | ForEach-Object { $_.Current.Name } | Where-Object { $_ })
    Shot 'index'

    Section 'Every page on the index' {
        foreach ($title in $titles) {
            Open $title
            Shot $title
            # A control with no size is invisible, as the fixed slot's button once was. Page up on a scroll bar is meant to
            # have none at the top of a page, and so are its other parts at the ends.
            $empty = @(All $CT::Button | Where-Object { $_.Current.Name -notin 'Line up', 'Page up', 'Page down', 'Line down' } | Where-Object {
                $b = $_.Current.BoundingRectangle
                [double]::IsInfinity($b.Width) -or [double]::IsInfinity($b.Height) -or $b.Width -eq 0 -or $b.Height -eq 0
            } | ForEach-Object { $_.Current.Name })
            Check "$title has no button without a size" ($empty.Count -eq 0) ($empty -join ', ')
            if ($debugBuild) {
                $line = WaitForText "${title}: * ms, *"
                Check "$title reports its cost" ([bool]$line) ''
                if ($line) { "        $line" }
            }
        }
        if ($debugBuild) {
            Index
            Start-Sleep -Milliseconds 200
            Check 'the index has no performance line' (-not (FirstText '*: * ms, *'))
        }
    }

    Section 'ion-button' {
        Open 'ion-button'
        $x, $y = Centre (Named $CT::Button 'Solid')
        [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 250
        Press; Start-Sleep -Milliseconds 180
        Shot 'ion-button-press'
        Release; Start-Sleep -Milliseconds 400
        Check 'a click counts once' ((FirstText 'Clicks:*') -eq 'Clicks: 1') (FirstText 'Clicks:*')
        ClickTwice
        Check 'a double click counts twice' ((FirstText 'Clicks:*') -eq 'Clicks: 3') (FirstText 'Clicks:*')
        Press; Start-Sleep -Milliseconds 60
        [void][RealInput]::SetCursorPos($x, ($y + 300)); Start-Sleep -Milliseconds 100
        Release; Start-Sleep -Milliseconds 250
        Check 'pressing, dragging off and releasing does not count' ((FirstText 'Clicks:*') -eq 'Clicks: 3') (FirstText 'Clicks:*')
        [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 100
        Press; Start-Sleep -Milliseconds 60
        [void][RealInput]::SetCursorPos(($x + 3), ($y + 3)); Start-Sleep -Milliseconds 60
        Release; Start-Sleep -Milliseconds 300
        Check 'a shaky click counts' ((FirstText 'Clicks:*') -eq 'Clicks: 4') (FirstText 'Clicks:*')
        Key '{ENTER}' 300
        Check 'Enter counts' ((FirstText 'Clicks:*') -eq 'Clicks: 5') (FirstText 'Clicks:*')
        Key ' ' 300
        Check 'Space counts' ((FirstText 'Clicks:*') -eq 'Clicks: 6') (FirstText 'Clicks:*')
        Open 'ion-button'
        $x, $y = Centre (Named $CT::Button 'Solid')
        Tap $x $y
        Check 'a touch tap counts once' ((FirstText 'Clicks:*') -eq 'Clicks: 1') (FirstText 'Clicks:*')
    }

    Section 'ion-card' {
        Open 'ion-card'
        $x, $y = Centre (Named $CT::Button 'Button:*')
        $right = [int]($win.Current.BoundingRectangle.X + 300 * $scale)
        $plain = Pixel $right $y
        Click $x $y
        Check 'a click counts once' ((FirstText 'Clicks:*') -eq 'Clicks: 1') (FirstText 'Clicks:*')
        ClickTwice
        Check 'a double click counts twice' ((FirstText 'Clicks:*') -eq 'Clicks: 3') (FirstText 'Clicks:*')
        Away
        Start-Sleep -Milliseconds 700
        Check 'focus from a click does not show' ($plain -eq (Pixel $right $y))
        Key '{TAB}'
        Key '+{TAB}'
        Shot 'ion-card-keyboard-focus'
        Check 'keyboard focus shows' ((Pixel $right $y) -ne $plain)
        Key 'a'
        Shot 'ion-card-letter-clears'
        Check 'a letter key clears it' ((Pixel $right $y) -eq $plain)
        Open 'ion-card'
        $x, $y = Centre (Named $CT::Button 'Button:*')
        Tap $x $y
        Check 'a touch tap counts once' ((FirstText 'Clicks:*') -eq 'Clicks: 1') (FirstText 'Clicks:*')
        Start-Sleep -Milliseconds 700
        $before = Pixel $right $y
        Drag $x $y 90 8 16
        Start-Sleep -Milliseconds 300
        Check 'a touch drag does not click' ((FirstText 'Clicks:*') -eq 'Clicks: 1') (FirstText 'Clicks:*')
        Start-Sleep -Milliseconds 1200
        Check 'the ripple goes after a drag' ((Pixel $right $y) -eq $before)
    }

    Section 'ion-content' {
        Open 'ion-content'
        $fixed = Named $CT::Button 'Fixed'
        $f = $fixed.Current.BoundingRectangle
        $w = $win.Current.BoundingRectangle
        Check 'the fixed button is in the window' (($f.Height -gt 0) -and ($f.Bottom -le $w.Bottom) -and ($f.Right -le $w.Right)) "$f"
        $fx, $fy = Centre $fixed
        Click $fx $fy
        Check 'the fixed button clicks' ((FirstText '*fixed button*') -ne $null)
        $bx, $by = Centre (Named $CT::Button 'By 200')
        Click $bx $by
        Start-Sleep -Milliseconds 500
        Check 'ScrollByPoint ends where it should' ((FirstText 'Ended at*') -eq 'Ended at 200') (FirstText 'Ended at*')
        Shot 'ion-content-scrolled'
        $f2 = (Named $CT::Button 'Fixed').Current.BoundingRectangle
        Check 'the fixed button stays put' ($f2.Y -eq $f.Y) "$($f2.Y) against $($f.Y)"
        Check 'the status has moved on from the first click' (-not (FirstText '*fixed button*'))
        Tap $fx $fy
        Check 'a touch tap on the fixed button clicks' ([bool](WaitForText '*fixed button*' 2))
        $tx, $ty = Centre (Named $CT::Button 'Top')
        Click $tx $ty
        Start-Sleep -Milliseconds 700
        Check 'ScrollToTop gets there' ((EndedAt) -eq 0) (FirstText 'Ended at*')
        $cx = [int]($w.X + $w.Width / 2)
        [void][RealInput]::SetCursorPos($cx, ([int]($w.Y + $w.Height / 2))); Start-Sleep -Milliseconds 150
        Wheel 4
        Check 'the wheel scrolls' ((FirstText 'Ended at*') -eq 'Ended at 200') (FirstText 'Ended at*')
        Drag $cx ([int]($w.Y + $w.Height * 0.7)) (-300)
        Start-Sleep -Milliseconds 800
        Shot 'ion-content-touch-drag'
        "        800 ms after a drag: $(FirstText 'Top *')$(FirstText 'Ended at*')"
        Start-Sleep -Milliseconds 2500
        $ended = EndedAt
        Check 'a touch drag scrolls, then ends' ($ended -gt 200) "at $ended"
        "        once it stops: $ended"
    }

    Section 'ion-list: sticky dividers' {
        Open 'ion-list'
        $w = $win.Current.BoundingRectangle
        $cx = [int]($w.X + $w.Width / 2)
        [void][RealInput]::SetCursorPos($cx, ([int]($w.Y + $w.Height / 2))); Start-Sleep -Milliseconds 150
        # Where a stuck divider's text is, in the window: below the header, and above anything else in the page.
        function DividerY { (Named $CT::Text "The next group's divider").Current.BoundingRectangle.Y - $win.Current.BoundingRectangle.Y }
        function IsStuck($y) { $y -ge 0 -and $y -le (130 * $scale) }
        Wheel 20
        Shot 'ion-list-stuck'
        $first = DividerY
        Wheel 2
        $second = DividerY
        Check 'a divider sticks under the wheel' ((IsStuck $first) -and $first -eq $second) "$first, then $second"
        Open 'ion-list'
        Drag $cx ([int]($w.Y + $w.Height * 0.8)) (-600) 20 16
        Start-Sleep -Milliseconds 1500
        Drag $cx ([int]($w.Y + $w.Height * 0.8)) (-300)
        Start-Sleep -Milliseconds 1500
        $first = DividerY
        Drag $cx ([int]($w.Y + $w.Height * 0.8)) (-200)
        Start-Sleep -Milliseconds 1500
        $second = DividerY
        Shot 'ion-list-stuck-touch'
        Check 'a divider sticks under touch' ((IsStuck $first) -and $first -eq $second) "$first, then $second"
    }

    Section 'ion-label: Tab' {
        Open 'ion-label'
        Key '{TAB}' 450
        Shot 'ion-label-tab-1'
        Key '{TAB}' 450
        Shot 'ion-label-tab-2'
        Key '{TAB}' 450
        Shot 'ion-label-tab-3'
        'frames only: look at them'
    }

    Section 'ion-item: touch' {
        Open 'ion-item'
        $x, $y = Centre (Named $CT::Button 'Button with detail')
        Tap $x $y
        $name = (All $CT::Button | ForEach-Object { $_.Current.Name } | Where-Object { $_ -like 'Clicks:*' }) | Select-Object -First 1
        Check 'a touch tap counts once' ($name -eq 'Clicks: 1') $name
    }

    # Headless tests run the bars on a test clock; this is the real one. The indeterminate bars and the buffer's dots move,
    # so two frames of the bars a moment apart differ; and a new value eases in.
    Section 'ion-progress-bar: on the real clock' {
        Open 'ion-progress-bar'
        Away
        function Grab($x, $y, $wd, $ht) {
            $bmp = New-Object Drawing.Bitmap ([int]$wd), ([int]$ht)
            $g = [Drawing.Graphics]::FromImage($bmp); $g.CopyFromScreen([int]$x, [int]$y, 0, 0, $bmp.Size); $g.Dispose(); $bmp
        }
        $w = $win.Current.BoundingRectangle
        $rtl = (Named $CT::Text 'Right to left').Current.BoundingRectangle
        $top = [int]($w.Y + 80 * $scale)
        $ht = [int]($rtl.Y - $top)
        $a = Grab $w.X $top $w.Width $ht
        Start-Sleep -Milliseconds 300
        $b = Grab $w.X $top $w.Width $ht
        $diff = 0
        for ($y = 0; $y -lt $ht; $y += 2) { for ($x = 0; $x -lt [int]$w.Width; $x += 3) { if ($a.GetPixel($x, $y).ToArgb() -ne $b.GetPixel($x, $y).ToArgb()) { $diff++ } } }
        $a.Dispose(); $b.Dispose()
        Check 'the bars move on their own' ($diff -gt 40) "$diff sampled pixels differ"

        $ease = (Named $CT::Text 'Changes ease*').Current.BoundingRectangle
        $vx, $vy = Centre (Named $CT::Button 'Value')
        $by = [int](($ease.Bottom + ($vy - 22 * $scale)) / 2)
        $bx = [int]($w.X + (16 + 0.3 * ($w.Width / $scale - 32)) * $scale)
        $before = Pixel $bx $by
        Click $vx $vy
        Start-Sleep -Milliseconds 500
        $after = [Drawing.Color]::FromArgb((Pixel $bx $by))
        Shot 'ion-progress-bar-value'
        Check 'a new value fills the bar to it' (($after.ToArgb() -ne $before) -and ($after.B -gt 180) -and ($after.R -lt 80)) ('{0:X8} from {1:X8}' -f $after.ToArgb(), $before)
    }

    # A chip is ion-activatable: hover for a mouse only, and a ripple from a press. Read from a pixel in the chip's padding,
    # left of its label.
    Section 'ion-chip' {
        Open 'ion-chip'
        Away
        Start-Sleep -Milliseconds 400
        function Inside($label) {
            $b = (Named $CT::Text $label).Current.BoundingRectangle
            if (-not $b -or $b.Width -le 0) { throw "No chip labelled $label" }
            [int]($b.X - 6 * $scale), [int]($b.Y + $b.Height / 2), [int]($b.X + $b.Width / 2)
        }
        $px, $py, $cx = Inside 'Default'
        $plain = Pixel $px $py

        Guard
        [void][RealInput]::SetCursorPos($cx, $py); Start-Sleep -Milliseconds 400
        $hover = Pixel $px $py
        Check 'a mouse over it shows the hover' ($hover -ne $plain) ('{0:X8}' -f $hover)
        Press; Start-Sleep -Milliseconds 250
        $pressed = Pixel $px $py
        Shot 'ion-chip-pressed'
        Release
        Check 'a press shows more than the hover' (($pressed -ne $hover) -and ($pressed -ne $plain)) ('{0:X8}' -f $pressed)
        Away
        Start-Sleep -Milliseconds 1200
        $gone = Pixel $px $py
        Check 'the ripple and the hover go after it' ($gone -eq $plain) ('{0:X8} against {1:X8}' -f $gone, $plain)

        # A touch tap shows the ripple. Not checked while held: with injected touch, nothing shows until the finger moves or
        # lifts, though Activatable lights up 100 ms after a press. Nor afterwards: Windows moves the cursor to the touch
        # point and, in about half the runs, promotes the touch to a mouse move a second later, which hovers the chip.
        TouchDown $cx $py; Start-Sleep -Milliseconds 120
        [RealInput]::TouchMove($cx, $py); Start-Sleep -Milliseconds 40
        TouchUp $cx $py
        $seen = $false
        for ($i = 0; $i -lt 12 -and -not $seen; $i++) { Start-Sleep -Milliseconds 50; $seen = (Pixel $px $py) -ne $plain }
        Check 'a touch tap shows the ripple' $seen
        Away
        Start-Sleep -Milliseconds 1500

        # Unlike the card, a chip has no ion-focusable: Ionic styles :host(:focus), so once an app makes it focusable, any
        # focus shows, a click's included, and a key doesn't clear it (measured in Chromium: clickPlainFocus, tabFocusAfterClick).
        $fx, $fy, $fcx = Inside 'Focusable'
        $unfocused = Pixel $fx $fy
        Click $fcx $fy
        Away
        Start-Sleep -Milliseconds 1200
        Shot 'ion-chip-after-click'
        $clicked = Pixel $fx $fy
        Check 'a click on a focusable chip shows its focus' ($clicked -ne $unfocused) ('{0:X8}' -f $clicked)
        Key 'a'
        Start-Sleep -Milliseconds 300
        $p1 = Pixel $fx $fy
        Check 'a letter key leaves it' (($p1 -eq $clicked) -and ($p1 -ne $unfocused)) ('{0:X8} against {1:X8}' -f $p1, $clicked)
        Key '{TAB}'
        Start-Sleep -Milliseconds 300
        $p2 = Pixel $fx $fy
        Check 'Tab away clears it' (($p2 -eq $unfocused) -and ($clicked -ne $unfocused)) ('{0:X8} against {1:X8}' -f $p2, $unfocused)
        Key '+{TAB}'
        Start-Sleep -Milliseconds 300
        Shot 'ion-chip-keyboard-focus'
        $p3 = Pixel $fx $fy
        Check 'and Shift+Tab back shows it' (($p3 -eq $clicked) -and ($p3 -ne $unfocused)) ('{0:X8} against {1:X8}' -f $p3, $clicked)
    }

    # The sample's knob is a sideways gesture on a track, in a content that scrolls: a sideways drag moves the knob and
    # leaves the page, and a drag up the track leaves the knob and scrolls the page, as a pan with direction x does in Ionic.
    Section 'gesture: sideways on a page that scrolls' {
        Open 'gesture'
        function Blue($x, $y) { $c = [Drawing.Color]::FromArgb((Pixel $x $y)); ($c.B -gt 180) -and ($c.R -lt 90) -and ($c.G -lt 150) }
        function RowY { (Named $CT::Text 'Row 5').Current.BoundingRectangle.Y }
        $w = $win.Current.BoundingRectangle
        $kx = [int]($w.X + 44 * $scale)   # the knob's centre: 16 of margin and half its 56
        $from = [int](Named $CT::Text 'Drag the knob*').Current.BoundingRectangle.Bottom
        $to = [int](Named $CT::Text 'Row 1').Current.BoundingRectangle.Y
        $ky = $null
        for ($y = $from; $y -lt $to -and -not $ky; $y += 2) { if (Blue $kx $y) { $ky = $y + [int](28 * $scale) } }
        if (-not $ky) { throw "The knob isn't between the status and the rows" }
        $rowY = RowY
        $move = [int](150 * $scale)

        Guard
        [void][RealInput]::SetCursorPos($kx, $ky); Start-Sleep -Milliseconds 150
        Press
        for ($i = 1; $i -le 15; $i++) { Start-Sleep -Milliseconds 16; [void][RealInput]::SetCursorPos(($kx + [int]($move * $i / 15)), ($ky + [int](4 * $i / 15))) }
        Start-Sleep -Milliseconds 80
        Release; Start-Sleep -Milliseconds 300
        Shot 'gesture-mouse-drag'
        Check 'a mouse drag sideways starts and ends the gesture' ([bool](FirstText 'Ended')) ((Texts | Select-Object -First 3) -join ' | ')
        Check 'the knob follows the mouse' ((Blue ($kx + $move) $ky) -and -not (Blue $kx $ky))
        Check 'the page stays put under the mouse' ((RowY) -eq $rowY) "$(RowY) against $rowY"

        $x0 = $kx + $move
        $back = [int](100 * $scale)
        TouchDown $x0 $ky
        for ($i = 1; $i -le 15; $i++) { Start-Sleep -Milliseconds 16; [RealInput]::TouchMove(($x0 - [int]($back * $i / 15)), $ky) }
        Start-Sleep -Milliseconds 80
        TouchUp ($x0 - $back) $ky; Start-Sleep -Milliseconds 300
        Shot 'gesture-touch-drag'
        $detail = FirstText 'delta *'
        Check 'a touch drag sideways ends the gesture, moving back' ((FirstText 'Ended') -and ($detail -like 'delta -*')) $detail
        Check 'the knob follows the finger' ((Blue ($x0 - $back) $ky) -and -not (Blue ($x0 + [int](20 * $scale)) $ky))
        Check 'the page stays put under the finger' ((RowY) -eq $rowY) "$(RowY) against $rowY"

        $kx2 = $x0 - $back
        Drag $kx2 $ky (-300)
        Start-Sleep -Milliseconds 1500
        Shot 'gesture-touch-up'
        Check 'a touch drag up the track scrolls the page' ((RowY) -lt ($rowY - 150)) "$(RowY) against $rowY"
        # Ionic's abortGesture calls notCaptured, with the detail at the moment the pan was detected.
        $up = FirstText 'delta *'
        Check 'and the gesture gives it up, calling NotCaptured' ((FirstText 'Not captured') -and ($up -like 'delta 0, -*')) "$((Texts | Select-Object -First 3) -join ' | ')"
    }

    # A button in a header's toolbar is still a native button and ion-activatable: a click and a touch tap each count once,
    # and a mouse over it shows md's hover, which goes when it leaves. The sample's Edit button reports in the footer.
    Section 'ion-toolbar: a button in the header' {
        Open 'ion-toolbar'
        Away
        Start-Sleep -Milliseconds 400
        $edit = Named $CT::Button 'Edit'
        $x, $y = Centre $edit
        $b = $edit.Current.BoundingRectangle
        # In the button's padding, left of its label.
        $hx = [int]($b.X + 3 * $scale)
        $plain = Pixel $hx $y

        Guard
        [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 400
        $hover = Pixel $hx $y
        Shot 'ion-toolbar-hover'
        Check 'a mouse over it shows the hover' ($hover -ne $plain) ('{0:X8} against {1:X8}' -f $hover, $plain)
        Press; Start-Sleep -Milliseconds 50; Release; Start-Sleep -Milliseconds 300
        Check 'a click counts once' ([bool](FirstText 'Edit tapped 1 time')) "$((Texts | Where-Object { $_ -like 'Edit tapped*' -or $_ -eq 'Footer' }) -join ' | ')"
        Away
        Start-Sleep -Milliseconds 1200
        $gone = Pixel $hx $y
        Check 'the hover and the ripple go once it leaves' ($gone -eq $plain) ('{0:X8} against {1:X8}' -f $gone, $plain)

        Tap $x $y
        Start-Sleep -Milliseconds 300
        Check 'a touch tap counts once' ([bool](FirstText 'Edit tapped 2 times')) "$((Texts | Where-Object { $_ -like 'Edit tapped*' }) -join ' | ')"
        Away
    }

    Check 'the app is still running' (-not $p.HasExited)
}
finally {
    LetGo
    [void][RealInput]::SetCursorPos($cursor.X, $cursor.Y)
    if (-not $p.HasExited) { [void]$p.CloseMainWindow(); Start-Sleep -Milliseconds 500 }
    if (-not $p.HasExited) { $p.Kill() }
}

"$($script:checks - $script:failures) of $($script:checks) checks passed"
if ($script:failures -gt 0) { exit 1 }
