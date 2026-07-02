# PC Spacesaver

Guided Windows disk cleanup app.

## Quick start

1. Clone this repo and install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Double-click **`run.bat`** (or run `dotnet run --project src\Spacesaver\Spacesaver.csproj`).
3. Complete the welcome screen, then use **Dashboard** → **Start cleanup wizard** to analyze your drive, or browse **Tasks** manually.

## Requirements

- Windows 10 version 1809 or later (Windows 11 recommended)
- 64-bit (x64) PC
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and the Windows application development workload (Visual Studio 2022) or Windows App SDK

## What it includes

- **Cleanup wizard** — scan → review → confirm → apply in four guided steps
- **Dashboard** — drive usage summary, progress tracking, and category shortcuts
- **Storage explorer** — folder size drill-down (analyze only, nothing deleted)
- **20 cleanup techniques** across Quick Wins, System Cleanup, Large Space Wins, and Manual Review
- **Step-by-step guidance** with warnings for risky options
- **One-click tool launchers** for Storage Sense, Disk Cleanup, System Properties, and more
- **Progress tracking** saved in `%LocalAppData%\PCSpacesaver`
- **System, light, or dark theme** (Settings → Appearance)

## Safety

This app is a guided cleanup helper with a hybrid apply model:

- **Scan** measures reclaimable space (Recycle Bin, Temp, caches, etc.) and ranks recommendations by impact and risk
- **Safe auto-clean** (only after you confirm): empty Recycle Bin, delete user Temp files older than 3 days (skips in-use files)
- **Everything else** opens the appropriate Windows tool — Disk Cleanup, Settings, Explorer — for you to complete manually
- **Advanced / caution actions** are unchecked by default and show explicit risk warnings before apply

It does **not**:

- Silently delete Downloads, browser data, or anything under `C:\Windows`
- Request administrator elevation automatically
- Recommend third-party registry cleaners, manual WinSxS deletion, or Prefetch cleanup
