# Builds a one-click PC-Spacesaver.exe in the project root.
[CmdletBinding()]
param(
    [switch]$NoUi,
    [scriptblock]$UiPump = $null
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$project = Join-Path $root "src\Spacesaver\Spacesaver.csproj"
$exe = Join-Path $root "PC-Spacesaver.exe"

function Publish-SpacesaverCore {
    param([scriptblock]$Pump = $null)

    . (Join-Path $root "tools\LaunchProgressWindow.ps1")
    Invoke-ProcessWithUiPump `
        -FileName "dotnet" `
        -Arguments @("publish", $project, "-p:PublishProfile=root-singlefile") `
        -WorkingDirectory $root `
        -OnPump $Pump `
        -FailMessage "Publish failed." | Out-Null

    if (-not (Test-Path -LiteralPath $exe)) {
        throw "Publish failed - exe not found at $exe"
    }

    return [math]::Round((Get-Item -LiteralPath $exe).Length / 1MB, 1)
}

if ($NoUi) {
    $null = Publish-SpacesaverCore -Pump $UiPump
    return
}

# Standalone: branded progress window (same chrome as run.ps1).
. (Join-Path $root "tools\LaunchProgressWindow.ps1")
$logo = Join-Path $root "src\Spacesaver\Assets\BrandLogo.png"
$ui = New-LaunchProgressWindow -LogoPath $logo -Title "PC Spacesaver" -Subtitle "Publishing..."

& $ui.RunModal {
    & $ui.SetStatus "Publishing single-file exe..." "Building"
    $sizeMb = Publish-SpacesaverCore -Pump $ui.Pump
    & $ui.SetStatus "Ready: PC-Spacesaver.exe ($sizeMb MB)" "Done"
    Start-Sleep -Milliseconds 600
    return @{}
}
