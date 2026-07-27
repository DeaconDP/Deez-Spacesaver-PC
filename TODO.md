# TODO

## Project Organiser v1

- [x] Organiser models + JSON persistence (`Models/Organiser*.cs`, `Services/ProjectOrganiserService.cs`)
- [x] Cancelable recursive folder sizing with offline/partial states (`Services/FolderSizeService.cs`)
- [x] Drive inventory + stable per-drive colors (`Services/DriveInventoryService.cs`, `Helpers/DriveColorHelper.cs`)
- [x] Fix byte formatter unit-skip bug; share via `Helpers/ByteFormatHelper.cs`
- [x] Nav tab + expandable project tree with segmented per-drive bars (`Pages/ProjectOrganiserPage.xaml`)
- [x] Create/rename/remove projects; add/retype/remove folders via FolderPicker
- [x] Add folders untagged first; tag Working/Assets/Backup/Other later from folder menu
- [x] Multi-select Add folders (native IFileOpenDialog) with batch scan + aggregated status
- [x] Cached-first sizes; per-project and refresh-all scans with cancel + status feedback
- [x] Unit tests (`src/Spacesaver.Tests`)
- [x] Manual QA: wide/narrow layouts, offline drive, partial scan, live rescan
- [x] Sort folders by Tag / Location / Size; remember choice in `settings.json`
- [x] Sort folders by Date created / Date last modified; show FS created/modified on folder rows
- [x] View folder (open in Explorer) from folder actions menu
- [x] Suggestions (siblings + shallow name match) — all projects + per-project; review dialog to add
- [x] Select and remove multiple folder associations per project with one confirmation
- [x] Add more project folders while size scans run; queue their measurements into the active scan
- [x] UX overhaul: collapsible Drives strip, primary+More project actions, name-first folder rows, tag section headers, narrow toolbar reflow

## Responsive redesign

- [x] Theme tokens + styles (`Themes/AppTheme.xaml`)
- [x] Shell window size policy + NavigationView thresholds
- [x] PageRoot MaxWidth + Narrow/Wide VisualStates (all pages)
- [x] Home hero + adaptive category grid
- [x] Recommendations adaptive footer + legend collapse
- [x] Task list wrapping + Task detail Grid steps (no MaxWidth=640)
- [x] Settings open sections + Reset feedback
- [x] Manual resize QA notes (720 / 900 / 1100 / maximized)

## App polish

- [x] Rebuild the Windows app icon with multi-resolution frames and reliable runtime loading
- [x] Project Organiser DRIVES legend: graphic table with used % and per-drive usage bars
- [x] Replace run.bat terminal with branded installer/updater progress window (`run.ps1`, `tools/LaunchProgressWindow.ps1`)
