# Branded WPF progress chrome for PC Spacesaver launch/publish scripts.
# Dot-source from STA PowerShell. Returns a controller hashtable.

function New-LaunchProgressWindow {
    [CmdletBinding()]
    param(
        [string]$LogoPath,
        [string]$Title = "PC Spacesaver",
        [string]$Subtitle = "Preparing build..."
    )

    Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Xaml | Out-Null

    $xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="PC Spacesaver"
        Width="440" Height="248"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="True">
  <Window.Resources>
    <Storyboard x:Key="FadeIn">
      <DoubleAnimation Storyboard.TargetName="Root"
                       Storyboard.TargetProperty="Opacity"
                       From="0" To="1" Duration="0:0:0.35"/>
    </Storyboard>
    <Storyboard x:Key="BarPulse" RepeatBehavior="Forever" AutoReverse="True">
      <DoubleAnimation Storyboard.TargetName="AccentBar"
                       Storyboard.TargetProperty="Opacity"
                       From="0.55" To="1" Duration="0:0:1.1"/>
    </Storyboard>
  </Window.Resources>
  <Border x:Name="Root" Opacity="0"
          Background="#FF0A0B0F"
          BorderBrush="#FF2A2E3A"
          BorderThickness="1"
          CornerRadius="4"
          Padding="28,24,28,22">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>

      <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
        <Border Width="40" Height="40" CornerRadius="2" Background="#FF12141C"
                BorderBrush="#FF2A2E3A" BorderThickness="1" Margin="0,0,14,0">
          <Image x:Name="Logo" Width="28" Height="28"
                 Stretch="Uniform" HorizontalAlignment="Center" VerticalAlignment="Center"/>
        </Border>
        <StackPanel VerticalAlignment="Center">
          <TextBlock x:Name="TitleText" Text="PC Spacesaver"
                     FontFamily="Segoe UI Variable Display, Segoe UI"
                     FontSize="20" FontWeight="SemiBold" Foreground="#FFE8EAEF"/>
          <TextBlock x:Name="SubtitleText" Text="Preparing build..."
                     FontFamily="Segoe UI Variable Text, Segoe UI"
                     FontSize="13" Foreground="#FF8B90A0" Margin="0,2,0,0"/>
        </StackPanel>
      </StackPanel>

      <TextBlock x:Name="StatusText" Grid.Row="1"
                 Text="Starting..."
                 FontFamily="Cascadia Mono, Consolas, Courier New"
                 FontSize="12" Foreground="#FF8B90A0"
                 TextWrapping="Wrap" Margin="0,0,0,14"/>

      <Border Grid.Row="2" Height="4" Background="#FF12141C" CornerRadius="2" ClipToBounds="True">
        <ProgressBar x:Name="AccentBar"
                     Height="4"
                     IsIndeterminate="True"
                     Foreground="#FF00E5C0"
                     Background="Transparent"
                     BorderThickness="0"/>
      </Border>

      <StackPanel x:Name="ErrorPanel" Grid.Row="3" Visibility="Collapsed" Margin="0,16,0,0">
        <TextBlock x:Name="ErrorText"
                   FontFamily="Segoe UI Variable Text, Segoe UI"
                   FontSize="13" Foreground="#FFFF3D81"
                   TextWrapping="Wrap" Margin="0,0,0,8"/>
        <Expander x:Name="DetailExpander" Header="Details"
                  Foreground="#FF8B90A0" FontSize="12" Margin="0,0,0,12">
          <TextBox x:Name="DetailBox"
                   IsReadOnly="True"
                   TextWrapping="Wrap"
                   AcceptsReturn="True"
                   VerticalScrollBarVisibility="Auto"
                   MaxHeight="90"
                   FontFamily="Cascadia Mono, Consolas, Courier New"
                   FontSize="11"
                   Foreground="#FFE8EAEF"
                   Background="#FF12141C"
                   BorderBrush="#FF2A2E3A"
                   BorderThickness="1"
                   Padding="8"/>
        </Expander>
        <Button x:Name="CloseButton" Content="Close"
                Width="88" Height="30" HorizontalAlignment="Right"
                Background="#FF12141C" Foreground="#FFE8EAEF"
                BorderBrush="#FF2A2E3A" BorderThickness="1"
                FontFamily="Segoe UI Variable Text, Segoe UI" FontSize="13"
                Cursor="Hand"/>
      </StackPanel>
    </Grid>
  </Border>
</Window>
"@

    $reader = [System.Xml.XmlReader]::Create([System.IO.StringReader]::new($xaml))
    $window = [Windows.Markup.XamlReader]::Load($reader)

    $root = $window.FindName("Root")
    $logo = $window.FindName("Logo")
    $titleText = $window.FindName("TitleText")
    $subtitleText = $window.FindName("SubtitleText")
    $statusText = $window.FindName("StatusText")
    $accentBar = $window.FindName("AccentBar")
    $errorPanel = $window.FindName("ErrorPanel")
    $errorText = $window.FindName("ErrorText")
    $detailBox = $window.FindName("DetailBox")
    $detailExpander = $window.FindName("DetailExpander")
    $closeButton = $window.FindName("CloseButton")

    $titleText.Text = $Title
    $subtitleText.Text = $Subtitle

    $root.Add_MouseLeftButtonDown({
        param($s, $e)
        if ($e.ChangedButton -eq [System.Windows.Input.MouseButton]::Left) {
            try { $window.DragMove() } catch { }
        }
    })

    if ($LogoPath -and (Test-Path -LiteralPath $LogoPath)) {
        try {
            $bmp = New-Object System.Windows.Media.Imaging.BitmapImage
            $bmp.BeginInit()
            $bmp.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
            $bmp.UriSource = [Uri]::new((Resolve-Path -LiteralPath $LogoPath).ProviderPath)
            $bmp.EndInit()
            $bmp.Freeze()
            $logo.Source = $bmp
        } catch { }
    }

    $closeButton.Add_Click({ $window.Close() })

    $fadeIn = $window.Resources["FadeIn"]
    $barPulse = $window.Resources["BarPulse"]

    $pumpUi = {
        [System.Windows.Threading.Dispatcher]::CurrentDispatcher.Invoke(
            [Action]{},
            [System.Windows.Threading.DispatcherPriority]::Background
        ) | Out-Null
    }.GetNewClosure()

    $controller = [ordered]@{}
    $controller.Window = $window
    $controller.Pump = $pumpUi

    $controller.SetStatus = {
        param([string]$Status, [string]$SubtitleLine = $null)
        $statusText.Text = $Status
        if ($null -ne $SubtitleLine -and $SubtitleLine -ne "") {
            $subtitleText.Text = $SubtitleLine
        }
        & $pumpUi
    }.GetNewClosure()

    $controller.ShowError = {
        param([string]$Message, [string]$Detail = "")
        $barPulse.Stop($window)
        $accentBar.IsIndeterminate = $false
        $accentBar.Foreground = [System.Windows.Media.BrushConverter]::new().ConvertFrom("#FFFF3D81")
        $accentBar.Maximum = 100
        $accentBar.Value = 100
        $subtitleText.Text = "Something went wrong"
        $statusText.Text = ""
        $errorText.Text = $Message
        if ($Detail) {
            $detailBox.Text = $Detail
            $detailExpander.Visibility = [System.Windows.Visibility]::Visible
        } else {
            $detailExpander.Visibility = [System.Windows.Visibility]::Collapsed
        }
        $errorPanel.Visibility = [System.Windows.Visibility]::Visible
        $window.Height = 360
        & $pumpUi
    }.GetNewClosure()

    $controller.WaitUntilClosed = {
        $frame = [System.Windows.Threading.DispatcherFrame]::new()
        $null = $window.Add_Closed({ $frame.Continue = $false }.GetNewClosure())
        [System.Windows.Threading.Dispatcher]::PushFrame($frame)
    }.GetNewClosure()

    $controller.RunModal = {
        param([scriptblock]$Work)
        $window.Show()
        $fadeIn.Begin($window)
        $barPulse.Begin($window)
        & $pumpUi

        $launchPath = $null
        try {
            $result = & $Work
            if ($result -is [hashtable] -and $result.ContainsKey("LaunchPath")) {
                $launchPath = [string]$result.LaunchPath
            }

            if ($launchPath) {
                Start-Process -FilePath $launchPath | Out-Null
            }

            $barPulse.Stop($window)
            $window.Close()
        } catch {
            $parts = @($_.Exception.ToString())
            if ($_.ScriptStackTrace) { $parts += $_.ScriptStackTrace }
            & $controller.ShowError $_.Exception.Message ($parts -join "`n")
            & $controller.WaitUntilClosed
        }
    }.GetNewClosure()
    return $controller
}

# Run an external process while keeping the WPF UI alive.
function Invoke-ProcessWithUiPump {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$FileName,
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$WorkingDirectory = (Get-Location).Path,
        [scriptblock]$OnPump = $null,
        [Parameter(Mandatory)][string]$FailMessage
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FileName
    $psi.Arguments = ($Arguments | ForEach-Object {
        $a = [string]$_
        if ($a -match '[\s"]') { '"' + ($a -replace '"', '\"') + '"' } else { $a }
    }) -join ' '
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    [void]$proc.Start()

    # Async reads avoid the classic redirect deadlock while we pump the UI.
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    while (-not $proc.HasExited) {
        if ($OnPump) { & $OnPump }
        Start-Sleep -Milliseconds 40
    }
    $proc.WaitForExit()

    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result
    if ($null -eq $stdout) { $stdout = "" }
    if ($null -eq $stderr) { $stderr = "" }
    $exitCode = $proc.ExitCode

    if ($exitCode -ne 0) {
        $detail = @($stderr.Trim(), $stdout.Trim()) | Where-Object { $_ }
        $joined = ($detail -join "`n`n")
        if (-not $joined) { $joined = "$FileName exited with code $exitCode." }
        throw [System.Exception]::new("$FailMessage`n$joined")
    }

    return @{ StdOut = $stdout; StdErr = $stderr; ExitCode = $exitCode }
}
