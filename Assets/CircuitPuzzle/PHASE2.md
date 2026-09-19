# Phase 2: circuit construction editor

Open `Assets/CircuitPuzzle/Scenes/Gameplay.unity` and enter Play Mode, or navigate through MainMenu → LevelSelect → Preview Workspace. The editor starts empty each session. This phase constructs components and terminal-to-terminal connections; it does not evaluate electrical behavior.

## Structure

- `Components/ComponentDefinition.cs` contains authored display, footprint, rotation and terminal metadata. The five assets in `Data/Components` provide Battery, Resistor, LED, Switch and Light Bulb. Add future properties to these definitions without moving placement logic into views.
- `Circuit/Model` owns the construction state, deterministic grid geometry, placement rules, terminal references, connection records, snapshots and session undo history. It has no dependency on scene objects, UI or a simulator.
- `Circuit/Presentation` draws the grid, components, terminal targets, wires and tray. Simple reusable UI visuals are created at runtime; component definition sprites can replace the placeholder body art. No external assets or drawing package is needed.
- `Circuit/Input` coordinates pointer gestures, selection, editor actions and status feedback. Unity UI pointer events flow through the existing Input System UI module, supporting mouse and touch.
- `Editor/CircuitEditorSetup.cs` creates the scene hierarchy and definition assets through Unity Editor APIs. UI wording is stored centrally in the existing `Data/Localization/EnglishUi.asset` catalog.
- `Tests/EditMode` covers editor state and geometry independently from presentation.
- `Tests/PlayMode` sends synthetic mouse and touchscreen events through the real
  Input System UI module in Gameplay and checks the resulting board and visuals.

New source files are grouped as follows (all paths below this project folder):

- `Circuit/Model`: BoardSnapshot, CircuitBoard, CircuitObjects, EditorHistory, GridGeometry.
- `Circuit/Input`: CircuitEditorController, EditorDragSession, PointerDragHandle.
- `Circuit/Presentation`: BoardPresentation, ComponentTray, ComponentView,
  EditorViewElements, GridBoardView, TerminalView, WireView.
- `Components/ComponentDefinition.cs`; `Editor/CircuitEditorSetup.cs`.
- `Tests/EditMode`: BoardTestFixture, ConnectionTests, EditorHistoryTests,
  GridAndPlacementTests; `Tests/PlayMode/ConstructionInteractionTests.cs`.
- Runtime, Editor, EditMode-test and PlayMode-test assembly definitions and Unity metadata.

Authored content changed: `Scenes/Gameplay.unity` and
`Data/Localization/EnglishUi.asset`; five component assets were added under
`Data/Components`. Views use reusable C# UI construction, so no prefab files
were added. The original Phase 1 screen content is retained inactive in Gameplay.

The board uses a configurable 6 × 8 grid with its origin at the lower left. The maximum cell size is 156 Canvas units, reduced to fit the available safe-area space. Resistor uses a 3 × 2 footprint so its occupied cells visibly change on rotation; other initial parts use 2 × 2. Rotations are quarter turns. Wires retain component/terminal identities and derive their endpoints from current component transforms.

## Scene setup

The delivered Gameplay scene contains the editor. To create the same setup in the existing Phase 1 scene, run **Tools → Circuit Puzzle → Setup Phase 2 Circuit Editor**. The batch entry point is `CircuitPuzzle.Editor.CircuitEditorSetup.SetUp`.

Setup uses the existing Canvas, SafeAreaPanel, navigation and EventSystem. It adds the `Circuit Editor` root, disables the recognized original placeholder, and saves Gameplay. It does not change MainMenu, LevelSelect, rendering, build targets, packages or project settings. Save or revert any unsaved Gameplay changes before using the utility; it refuses to silently save them. Other saved/open scenes are preserved. Repeat runs preserve the existing editor and authored assets, ensure its grid has the required CanvasRenderer, and save Gameplay through Unity APIs.

To tune the editor after setup, edit the existing objects and definition assets in the Inspector. Running setup again is deliberately not an overwrite or migration operation.

## Manual acceptance checks

Use a portrait Game view, initially 1080 × 1920:

1. Enter MainMenu Play Mode, choose **Play**, then **Preview Workspace**. Verify the grid, five tray parts, Undo, Reset, Rotate and Delete appear. **Back to Levels** must retain the original navigation.
2. Drag Battery onto the lower-left part of the board. The preview follows the pointer and snaps; a valid release creates one component with +/− terminals. Repeat with Resistor and LED in separate free areas, and also try Switch and Light Bulb.
3. Drag another tray part beyond the board, then onto an occupied part. The preview must mark invalid placement; releasing must not add a component.
4. Tap a component body to select it. Drag that body to free grid space and release; only the existing component should move. Drag it onto another part or outside the board; it must keep its previous position.
5. Select Resistor and press **Rotate**. Its 3 × 2 footprint becomes 2 × 3, the text remains readable, and terminals move with it. Try rotation next to an occupied area or board edge where the new footprint cannot fit; rotation must be rejected.
6. Press a Battery terminal, drag to a Resistor terminal, and release. Verify a temporary wire becomes a persistent wire. Repeat from Resistor to LED.
7. Start a wire and release over empty space; it must cancel. Release on the same terminal or repeat an existing pair in either direction; no duplicate/self connection should appear. Connecting Battery + directly to Battery − is allowed at this construction-only stage.
8. Move and rotate connected parts. Both finished wire endpoints must remain attached to their original terminals.
9. Tap an exposed wire segment. Its selected state should appear; **Delete** removes the wire. Press **Undo** to restore it.
10. Delete a connected component. Its attached wires must disappear. Press **Undo** to restore the component and connections together.
11. Make a placement, move, rotation and wire creation; press **Undo** after each to check that its previous state is restored.
12. Press **Reset**. All placed parts and wires disappear, and the session undo history is cleared. A new tray drag must still work.
13. Repeat at a taller portrait aspect ratio and, when available, on a touch device with a notch/safe area. Controls must remain reachable inside the existing safe-area panel. Try moving a part and drawing a wire with a finger.

## Scope and limitations

Inventory is unlimited for the five authored part types. Editing uses straight wires and one active pointer gesture; overlapping wire segments can require selecting a different exposed segment. There is no zoom, pan, orthogonal routing, persistent save, electrical evaluation, Power mode, puzzle objective, tutorial or progression behavior. Switch, LED and bulb are construction placeholders only. Reset establishes the empty initial construction state and clears the undo history; it is not an undoable edit.

## Verified results

- Unity 6000.6.0f1: runtime, Editor and test assemblies compile with no C# errors
  or warnings in the final run.
- **33/33 EditMode cases passed**: grid transforms, placement/bounds/overlap,
  allowed rotations, terminal identity, duplicate/self-wire rejection,
  same-component wiring, cascading deletion, all six undoable actions,
  history capacity and reset to empty or predefined construction snapshots.
- **4/4 PlayMode integration tests passed**: complete mouse construction/editing
  workflow, invalid drops and duplicate wires, synthetic touchscreen placement/
  wiring/movement, and Gameplay ↔ LevelSelect navigation. Tests use the real
  Input System and EventSystem. All five tray entries are checked for names.
- Inspected the final 1080 × 1920 Gameplay screenshot, including connected and
  rotated parts, terminal names, the tray and utility buttons.
- MainMenu, LevelSelect, both project instructions/specification files, package
  manifests and mobile PlayerSettings retain their pre-Phase-2 file hashes.

Local results: `Logs/Phase2-EditMode.xml`, `Logs/Phase2-PlayMode.xml`,
`Logs/Phase2-Setup.log`, `Logs/Phase2-PlayMode.log`, and
`Logs/Phase2-Gameplay.png`. Logs are ignored by Git. Tests are available in
Unity's Test Runner under the CircuitPuzzle test assemblies.

Android/iOS builds and physical touch/notch-device checks have not been run;
the existing workstation still needs the platform build modules. Synthetic
touch tests validate the input path, not physical-device ergonomics.
