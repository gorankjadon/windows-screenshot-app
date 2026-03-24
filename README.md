# Windows Screenshot App

Windows-first desktop screenshot app built for fast local capture with no cloud features.

## MVP Features

- Capture the full screen
- Capture a selected region
- Capture the active window
- Save PNG files locally
- Copy captures to the clipboard
- Configure the save folder
- Configure keyboard shortcuts

## Default Shortcuts

- `Ctrl+Alt+S`: region capture
- `Ctrl+Alt+F`: full-screen capture
- `Ctrl+Alt+A`: active-window capture

## Project Layout

- `src/GstackScreenshot`: WinForms app source
- `tests/GstackScreenshot.Tests`: lightweight regression test harness
- `plans/windows-screenshot-mvp-design.md`: reviewed MVP plan

## Build

```powershell
.\build.ps1
```

This writes the app to `dist/GstackScreenshot.exe`.

## Test

```powershell
.\build-tests.ps1
```

This compiles and runs the regression test harness.

## Run

```powershell
.\dist\GstackScreenshot.exe
```

The app runs as a tray application.
