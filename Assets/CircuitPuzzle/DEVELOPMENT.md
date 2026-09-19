# Development progress

## Phase 2.1 — Editor UX cleanup

Completed and verified in Unity 6000.6.0f1. The existing 33 EditMode cases and
4 PlayMode interaction tests pass; the final logs contain no C# compiler errors
or warnings. Rendered Gameplay at 1080 × 1920 was inspected.

- More space for the same 6 × 8 board, within the existing mobile safe area.
- Compact header, action row and five consistent tray cards.
- Smaller muted bodies, simple leads, upright names and contextual terminal names.
- 36px terminal graphics retain their 120px invisible input targets.
- Thin selection outlines, subtle grid, and outlined direct wires.
- Status messages expire after 2.5 seconds; wire selection uses visual feedback.

The scene was updated through `CircuitEditorSetup.ApplyUxCleanup`, exposed under
**Tools > Circuit Puzzle > Apply Phase 2.1 Compact Layout**. This explicit command
updates the known editor layout and preserves the construction model and assets.
Ordinary Phase 2 setup still preserves an existing editor.

Local evidence: `Logs/Phase21-Setup.log`, `Logs/Phase21-EditMode.xml`,
`Logs/Phase21-PlayMode.xml`, `Logs/Phase21-Gameplay.png`.
No packages, PlayerSettings or instruction/specification files were changed.
Android/iOS builds and physical device ergonomics remain untested; the platform
modules are not installed on this workstation.
