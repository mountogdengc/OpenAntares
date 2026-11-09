### Task 10: Copy pass, scanline effect and settings

**Files:** user-facing strings in touched components; `console_screen.gdshader`; `UiSettings.cs`; `docs/DESIGN_GUIDE.md` (exception note and voice guide).

- [ ] Add failing `NoSpecVoiceStringsInMainLoop` (allowlisted phrase list from the baseline inventory), `PluralsAreCorrect`, `ScanlinesToggleAndReducedEffectsPersistOutsideSaves`.
- [ ] Rewrite labels to short in-world instrument language with correct plurals; move rules explanations to tooltips without dropping information. Add the scanline/vignette shader on `ScreenInset` areas only, with the D2 toggle.
- [ ] Rerun cases; confirm saves are byte-identical to baseline for the same commands.
