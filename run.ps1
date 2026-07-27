# PC Spacesaver - one-click restore, publish, and launch (branded progress UI).
# Invoked by run.bat with a hidden console. Requires STA (set by run.bat).

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
Set-Location -LiteralPath $root

. (Join-Path $root "tools\LaunchProgressWindow.ps1")

$logo = Join-Path $root "src\Spacesaver\Assets\BrandLogo.png"
$ui = New-LaunchProgressWindow -LogoPath $logo -Title "PC Spacesaver" -Subtitle "Updating build..."

& $ui.RunModal {
    & $ui.SetStatus "Checking .NET SDK..." "Getting ready"

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        throw ".NET 8 SDK is required. Install from https://dotnet.microsoft.com/download/dotnet/8.0"
    }

    & $ui.SetStatus "Restoring packages..." "Updating build..."
    Invoke-ProcessWithUiPump `
        -FileName "dotnet" `
        -Arguments @("restore", (Join-Path $root "Spacesaver.sln")) `
        -WorkingDirectory $root `
        -OnPump $ui.Pump `
        -FailMessage "Package restore failed." | Out-Null

    & $ui.SetStatus "Publishing PC-Spacesaver.exe..." "Building"
    & (Join-Path $root "publish.ps1") -NoUi -UiPump $ui.Pump

    $exe = Join-Path $root "PC-Spacesaver.exe"
    if (-not (Test-Path -LiteralPath $exe)) {
        throw "Publish finished but PC-Spacesaver.exe was not found."
    }

    & $ui.SetStatus "Starting app..." "Almost there"
    return @{ LaunchPath = $exe }
}
