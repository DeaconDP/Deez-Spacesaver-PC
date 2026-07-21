# Roadmap

## Epics

- [x] **Responsive redesign (2026-07)** — Adaptive Narrow/Wide layouts, cyberpunk-dark tokens, window min size, content rail. Closes the “awful resize” UX gap.
- [x] **Project Organiser v1 — visual (2026-07)** — New nav tab mapping projects to Working/Asset/Backup/Other folders across drives; cached recursive sizes, per-drive segmented bars with stable drive colors, offline-USB awareness, JSON persistence, unit tests. View-only by design.

## Deferred

- 2026-07-17: Light theme — dark-only product; add only if users ask. `src/Spacesaver/App.xaml`
- 2026-07-17: Custom display fonts (Space Grotesk / IBM Plex) — using Segoe UI Variable + Cascadia Mono for WinUI packaging simplicity. `src/Spacesaver/Themes/AppTheme.xaml`
- 2026-07-17: Organiser content-level duplicate detection (hash/compare backup folders against working folders) — v1 is visual-only per scope. `src/Spacesaver/Services/FolderSizeService.cs:1`
- 2026-07-17: Organiser automated actions (move/merge/delete redundant folders) — reconcile manually first; automate once the visual workflow proves itself. `src/Spacesaver/Pages/ProjectOrganiserPage.xaml.cs:1`
- 2026-07-17: Organiser Suggestions deep whole-disk crawl / indexed search — v1 is sibling + depth-3 name match only. `src/Spacesaver/Services/FolderSuggestionService.cs:1`
