# Circuit Puzzle

Open `Scenes/MainMenu.unity` and enter Play Mode: **Play → Level Select →
Preview Workspace**. Gameplay now contains the Phase 2 construction editor.
See [PHASE2.md](PHASE2.md) for its architecture, controls, tests and manual
acceptance steps. No electrical simulation or puzzle evaluation is implemented.

## Layout

Runtime code is grouped by responsibility: `Core`, `Circuit`, `Components`,
`Simulation`, `Levels`, `Objectives`, `UI`, `Tutorial`, `Save`, and `Audio`.
`Editor` contains editor-only setup code. `Tests`, `Data`, `Prefabs`, `Scenes`,
and `Art` provide space for later content. Empty areas are intentional. Runtime,
Editor and test assemblies are separated so editor tooling and NUnit tests stay
out of normal player builds.

The original Universal 2D template scenes, renderer assets, input settings and
packages remain in their existing locations.

## Foundation setup

`Tools > Circuit Puzzle > Set Up Phase 1 Foundation` creates missing folders,
the English text asset and missing foundation scenes using Unity Editor APIs.
It enables the three scenes at the start of the build scene list, retaining
other scenes afterward, and applies portrait-only mobile orientation settings.
It does not overwrite existing scene files or the text asset, or save/replace
the scenes you already have open. Run it only in Edit Mode.
Save any untitled scene first; Unity cannot create scenes additively beside one.

For automation with this project closed in the Editor:

```text
Unity.exe -batchmode -nographics -quit -projectPath "<project path>" -executeMethod CircuitPuzzle.Editor.FoundationSetup.SetUp -logFile "<log path>"
```

Scene layout and text changes after initial creation should be made in the
Unity Inspector, not by editing generated scene YAML. Rerunning setup preserves
those edits.

## Mobile UI and builds

- All foundation canvases use Scale With Screen Size, reference **1080 × 1920**,
  and width/height matching of 0.5. Large buttons support touch and mouse through
  the already installed Input System UI module.
- `SafeAreaPanel` belongs directly under a full-screen overlay Canvas. Put UI
  content inside it. It adapts to `Screen.safeArea` at runtime, including changes
  without a resolution change; edit-mode notch preview is not implemented.
- In the Game view, select/add a 1080 × 1920 resolution or 9:16 aspect ratio.
  Also inspect taller phones and portrait tablets. Physical notch/home indicator
  and touch checks still require a device or Unity's Device Simulator.
- Android and iOS build support modules are not installed on this workstation.
  Add the matching modules through Unity Hub before selecting the target in
  **File > Build Profiles**. Android also requires its SDK/NDK/OpenJDK tools.
  Use the shared scene list; MainMenu is the entry scene. The setup leaves the
  current active target unchanged.
- Set final application identifiers and signing when making device/distribution
  builds. Existing identifiers, SDK minimums, Android IL2CPP/ARM64, rendering and
  quality settings are preserved. Final iOS compilation/signing requires macOS
  with Xcode. No mobile build is claimed by this foundation setup.

## Later extension points

`Data/Localization/EnglishUi.asset` holds all current screen text under stable
keys. `UiLabel` reads this catalog when enabled; use **Refresh Text** on a label
after editing the catalog to preview changes in Edit Mode. A localization
package, language switching and Turkish translations are deferred.

`Save` and `Levels` are reserved for separate save/progression services and data
in later phases. Nothing writes progress or PlayerPrefs during Phase 1. Keep
future storage independent of UI and store runtime files in
`Application.persistentDataPath`, rather than under Assets.

There is no bootstrap singleton, dependency framework, simulator, puzzle
objective or save system. The construction editor is described in PHASE2.md.

## Phase 1 verification

Unity 6000.6.0f1 compiled the runtime and Editor scripts and generated all three
scenes successfully (batch exit code 0). The final setup log contains no C#
compiler errors or warnings. Navigation event paths, persistent Input System
references and canvas reference resolutions were also inspected. The log is
stored locally at `Logs/Phase1-Setup.log` (ignored by Git).

That foundation run did not test Play Mode or device builds. See PHASE2.md for
the later interaction test results. Unity emitted a non-blocking licensing
access-token message; its license check and the setup completed successfully.
