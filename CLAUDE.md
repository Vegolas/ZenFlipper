# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ZenFlipper is a .NET 10 console tool that flips coordinates in Gothic 2 `.ZEN` world files for map mirroring mods. It transforms object positions, waypoints, directions, bounding boxes, and rotation matrices across configurable axes (X/Y/Z).

## Build & Run

```bash
dotnet build -c Release
dotnet run -- <input.zen> <output.zen> [options]
```

Output binary: `bin/Release/net10.0/ZenFlipper.exe`

CLI flags: `-x`, `-y`, `-z` (axis selection), `-rotation`/`-r` (flip rotation matrices), `-preview`/`-p` (dry run), `-verbose`/`-v`.

## Architecture

Single-project console app (`Gothic2ZenFlipper` namespace). Three source files:

- **Program.cs** — Entry point; registers Windows-1250 encoding provider, delegates to `ZenFlipper.Run()`.
- **ZenFileCoordinateFlipper.cs** — Core logic. Parses CLI args, applies sequential regex replacements on `.ZEN` text content for each data type: `trafoOSToWSPos` (positions) → `position` (waypoints) → `direction` → `trafoOSToWSRot` (rotation matrices as hex-encoded floats) → `bbox3DWS` (bounding boxes with min/max swap).
- **ZenDryRun.cs** — Diagnostic tool for encoding verification. Reads/writes a file unchanged and does byte-for-byte comparison.

## Key Technical Details

- **File encoding**: Windows-1250 by default (Gothic 2 standard). Uses `System.Text.Encoding.CodePages` package.
- **Rotation matrices**: Stored as 96-char hex strings (12 floats × 4 bytes × 2 hex chars). Flipping negates specific matrix columns depending on axis.
- **Bounding boxes**: When flipping an axis, min/max values for that axis are swapped and negated.
- **Number format**: All float parsing/formatting uses `CultureInfo.InvariantCulture` with `"G"` format specifier.

## Gothic 2 Coordinate System

- **X**: Left-Right (most common flip axis for mirroring)
- **Y**: Up-Down (vertical)
- **Z**: Forward-Backward (depth)
