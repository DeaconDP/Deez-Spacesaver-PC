# Deez Spacesaver PC

<p align="center">
  <img src="https://cdn.jsdelivr.net/gh/DeaconDP/Deez-Spacesaver-PC@PLACEHOLDER/docs/screenshots/hero.png" alt="Deez Spacesaver PC" width="720" />
</p>

Guided Windows disk cleanup — scan, recommend, and clean safely. No install, no separate .NET download.

![License: MIT](https://img.shields.io/badge/license-MIT-blue)
![Platform: Windows](https://img.shields.io/badge/platform-Windows-informational)

## Who it’s for

Windows users who want reclaimable-space recommendations and step-by-step cleanup without silent dangerous deletes.

## Quick start

1. Download or clone this folder
2. Double-click **`PC-Spacesaver.exe`**
3. Click **Scan for Recommendations**

Rebuild from source: double-click **`run.bat`** (needs .NET 8 SDK + Windows App SDK workload).

## Features

- Scan ranks reclaimable space by impact and risk
- Project Organiser (read-only on disk) across drives
- ~20 techniques across Quick Wins / System / Large / Manual Review
- Opens Windows tools for anything beyond safe auto-clean

## Limitations

- Windows 10 1809+ x64 (Windows 11 recommended)
- Does not silently wipe Downloads or `C:\Windows`
- Advanced actions unchecked by default

## Development

`run.bat` / `publish.ps1` rebuild the self-contained exe.

## Credit

Created by [deac.online](https://deac.online) @ [worldbuild.io](https://worldbuild.io)

## License

See repository for license terms.
