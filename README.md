# PC Spacesaver

Guided Windows disk cleanup app. Double-click **PC-Spacesaver.exe** to run — no install, no .NET download, no extra folders.

## Quick start

1. Download or clone this folder.
2. Double-click **`PC-Spacesaver.exe`** in this directory.
3. Click **Scan for Recommendations** to analyze your drive, or follow the checklist manually.

First launch may take a few seconds while the app unpacks bundled runtime files to a temp folder. Later launches are faster.

## Requirements

- Windows 10 version 1809 or later (Windows 11 recommended)
- 64-bit (x64) PC

## What it includes

- **Scan for Recommendations** — measures reclaimable space and lets you choose what to apply
- **Project Organiser** — map each project's folders across all your drives (tag as Working, Parent, Assets, Backup, Junk, Template, Experiment, or Other later), with per-drive size bars to spot redundant copies; sort folders by tag, location, size, date created, or date last modified (choice remembered; rows show filesystem created/modified); **Suggestions** finds likely siblings and name matches to add. View-only: it never moves or deletes files.
- **20 cleanup techniques** across Quick Wins, System Cleanup, Large Space Wins, and Manual Review
- **Step-by-step guidance** with warnings for risky options
- **One-click tool launchers** for Storage Sense, Disk Cleanup, System Properties, and more
- **Progress tracking** saved in `%LocalAppData%\PCSpacesaver` (organiser projects in `projects.json`, cleanup progress in `progress.json`, UI prefs such as folder sort in `settings.json`)
- **Dark mode** UI

## Rebuild the exe (developers)

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and the Windows application development workload (Visual Studio 2022) or Windows App SDK.

Double-click **`run.bat`** to restore packages, rebuild, and launch the app.

Publish only (no launch):

```powershell
.\publish.ps1
```

This writes `PC-Spacesaver.exe` to the project root as a self-contained single file.

## Safety

This app is a guided cleanup helper with a hybrid apply model:

- **Scan** measures reclaimable space (Recycle Bin, Temp, caches, etc.) and ranks recommendations by impact and risk
- **Safe auto-clean** (only after you confirm): empty Recycle Bin, delete user Temp files older than 3 days (skips in-use files)
- **Everything else** opens the appropriate Windows tool — Disk Cleanup, Settings, Explorer — for you to complete manually
- **Advanced / caution actions** are unchecked by default and show explicit risk warnings before apply
- **Project Organiser is read-only on disk** — it only measures folder sizes and stores your project/folder associations locally; removing a project or folder from the organiser never touches the actual files. Offline USB drives keep their last known sizes.

It does **not**:

- Silently delete Downloads, browser data, or anything under `C:\Windows`
- Request administrator elevation automatically
- Recommend third-party registry cleaners, manual WinSxS deletion, or Prefetch cleanup
