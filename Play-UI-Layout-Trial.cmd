@echo off
setlocal
set "TRIAL_GODOT=F:\Projects-PCGames\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
if not exist "%~dp0.godot\mono\temp\bin\Debug\OpenAntares.dll" (
  echo The trial needs its queued build. See reports\UI_LAYOUT_TRIAL_2026-11-03.md.
  pause
  exit /b 1
)
"%TRIAL_GODOT%" --path "%~dp0."
