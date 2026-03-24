# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog.

## [0.1.0.0] - 2026-03-24

### Added
- Initial Windows screenshot MVP with tray-based full-screen, region, and active-window capture.
- Configurable hotkeys, local PNG saving, and clipboard copy support.
- Build and regression test scripts for local development.

### Fixed
- Hotkey registration rollback and settings-save rollback safety.
- Storage path fallback, startup crash handling, and save-folder failure handling.
- Filename collision handling and regression coverage for core failure paths.
