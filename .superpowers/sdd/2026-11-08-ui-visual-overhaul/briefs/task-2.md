### Task 2: Fonts, frame art and icons

**Files:** `art/fonts/*`, `art/ui/**`, `LICENSING.md`, `art/ui/README.md`.

- [ ] Add the three D3 font families (needed weights only) with OFL license texts; record each in LICENSING.md with source URL and version.
- [ ] Produce original nine-patch frames at 2x (outer frame plate with highlight/shadow bevel, screen inset, command key normal/hover/pressed/disabled/focus, tab latched/unlatched, End Turn key) and the icon set (production, research, population, alert, gear, back, each vessel role in `content/core` roles, fleet status: idle/moving/combat/damaged). Record authorship and CC0 dedication in `art/ui/README.md`.
- [ ] Verify imports at 1x and 2x (filter, mipmaps off for UI), file sizes, and that no asset is referenced by C# path except through the theme or the existing icon loader.
