using System.Collections;
using System.IO;
using System.Linq;
using CircuitPuzzle.Circuit;
using CircuitPuzzle.Circuit.Input;
using CircuitPuzzle.Circuit.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace CircuitPuzzle.Tests
{
    // Send input through the installed Input System and real scene EventSystem,
    // rather than invoking controller methods to stand in for user interaction.
    public sealed class ConstructionInteractionTests
    {
        private Mouse mouse;
        private Touchscreen touch;
        private CircuitEditorController editor;
        private GridBoardView grid;
#if UNITY_EDITOR
        private uint previousWidth, previousHeight;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.GetRenderingResolution(out previousWidth, out previousHeight);
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1080, 1920, "Phase 2 portrait test");
#endif
            yield return SceneManager.LoadSceneAsync("Assets/CircuitPuzzle/Scenes/Gameplay.unity");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            editor = Object.FindAnyObjectByType<CircuitEditorController>();
            grid = Object.FindAnyObjectByType<GridBoardView>();
            Assert.That(editor, Is.Not.Null);
            Assert.That(grid.CellSize, Is.GreaterThan(100), "Tests need a portrait rendering area.");
            ComponentTray tray = Object.FindAnyObjectByType<ComponentTray>();
            foreach (var definition in tray.Definitions)
                Assert.That(tray.transform.Find("Items").Find(definition.StableId).GetComponentInChildren<Text>().text,
                    Is.EqualTo(definition.DisplayName), "Every tray entry must identify the part it creates.");
            mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            yield return MouseAt(new Vector2(-100, -100), false);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            yield return SceneManager.LoadSceneAsync("Assets/CircuitPuzzle/Scenes/MainMenu.unity");
#if UNITY_EDITOR
            if (previousWidth > 0 && previousHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(previousWidth, previousHeight, "Restored test resolution");
#endif
            yield return null;
        }

        [UnityTest]
        public IEnumerator MouseBuildMoveRotateWireDeleteUndoAndReset()
        {
            yield return Place("battery", new Vector2Int(0, 0));
            yield return Place("resistor", new Vector2Int(3, 0));
            yield return Place("led", new Vector2Int(0, 4));
            yield return Place("switch", new Vector2Int(3, 4));
            yield return Place("light-bulb", new Vector2Int(3, 6));
            Assert.That(editor.Board.Components.Count, Is.EqualTo(5));
            string battery = Id("battery");
            string resistor = Id("resistor");
            var from = new TerminalReference(battery, "negative");
            var to = new TerminalReference(resistor, "a");
            yield return Drag(TerminalPoint(from), TerminalPoint(to));
            Assert.That(editor.Board.Wires.Count, Is.EqualTo(1), "Terminal drag must create a wire.");

            yield return Drag(BodyPoint(battery), grid.GridToScreen(new Vector2(1.5f, 3.5f)));
            Assert.That(editor.Board.FindComponent(battery).Position, Is.EqualTo(new Vector2Int(0, 2)));
            yield return Click(BodyPoint(battery));
            yield return ClickButton("Rotate");
            Assert.That(editor.Board.FindComponent(battery).Rotation, Is.EqualTo(1));
            yield return Click(BodyPoint(resistor));
            yield return ClickButton("Rotate");
            Assert.That(editor.Board.FindComponent(resistor).Rotation, Is.EqualTo(1));
            WireView wire = Object.FindObjectsByType<WireView>()
                .Single(view => !string.IsNullOrEmpty(view.WireId));
            Assert.That(Vector2.Distance(wire.StartPoint, grid.GridToLocal(editor.Board.GetTerminalPosition(from))), Is.LessThan(.01f));
            Assert.That(Vector2.Distance(wire.EndPoint, grid.GridToLocal(editor.Board.GetTerminalPosition(to))), Is.LessThan(.01f));

            string screenshot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Phase2-Gameplay.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(screenshot));
            ScreenCapture.CaptureScreenshot(screenshot);
            for (int i = 0; i < 4; i++) yield return null;

            Vector2 midpoint = RectTransformUtility.WorldToScreenPoint(null,
                wire.rectTransform.TransformPoint((wire.StartPoint + wire.EndPoint) * .5f));
            yield return Click(midpoint);
            yield return ClickButton("Delete");
            Assert.That(editor.Board.Wires, Is.Empty);
            yield return ClickButton("Undo");
            Assert.That(editor.Board.Wires.Count, Is.EqualTo(1));
            yield return Click(BodyPoint(battery));
            yield return ClickButton("Delete");
            Assert.That(editor.Board.FindComponent(battery), Is.Null);
            Assert.That(editor.Board.Wires, Is.Empty);
            yield return ClickButton("Undo");
            Assert.That(editor.Board.FindComponent(battery), Is.Not.Null);
            Assert.That(editor.Board.Wires.Count, Is.EqualTo(1));
            yield return ClickButton("Reset");
            Assert.That(editor.Board.Components, Is.Empty);
            Assert.That(editor.Board.Wires, Is.Empty);
            Assert.That(Button("Undo").interactable, Is.False);
            yield return Place("battery", new Vector2Int(0, 0));
            yield return ClickButton("Undo");
            Assert.That(editor.Board.Components, Is.Empty);
        }

        [UnityTest]
        public IEnumerator InvalidDropsCancelAndDuplicateWiresAreRejected()
        {
            yield return Place("battery", new Vector2Int(0, 0));
            yield return Place("led", new Vector2Int(3, 0));
            string battery = Id("battery");
            Vector2 original = BodyPoint(battery);
            yield return Drag(original, BodyPoint(Id("led")));
            Assert.That(editor.Board.FindComponent(battery).Position, Is.EqualTo(Vector2Int.zero));
            yield return Drag(TrayPoint("switch"), grid.GridToScreen(new Vector2(-1, 4)));
            Assert.That(editor.Board.Components.Count, Is.EqualTo(2));
            yield return Drag(TrayPoint("resistor"), grid.GridToScreen(new Vector2(1, 1)));
            Assert.That(editor.Board.Components.Count, Is.EqualTo(2));
            var positive = new TerminalReference(battery, "positive");
            var negative = new TerminalReference(battery, "negative");
            yield return Click(TerminalPoint(positive));
            Assert.That(editor.Board.Wires, Is.Empty);
            yield return Drag(TerminalPoint(positive), grid.GridToScreen(new Vector2(4, 5)));
            Assert.That(editor.Board.Wires, Is.Empty);
            // Same-owner connection is intentionally editor-valid, and must also be selectable.
            yield return Drag(TerminalPoint(positive), TerminalPoint(negative));
            Assert.That(editor.Board.Wires.Count, Is.EqualTo(1));
            yield return Drag(TerminalPoint(negative), TerminalPoint(positive));
            Assert.That(editor.Board.Wires.Count, Is.EqualTo(1));
            yield return Click(grid.GridToScreen(new Vector2(1, 1)));
            yield return ClickButton("Delete");
            Assert.That(editor.Board.Wires, Is.Empty);
            Assert.That(editor.Board.Components.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TouchDragsUseTheSameConstructionPath()
        {
            InputSystem.RemoveDevice(mouse);
            mouse = null;
            touch = InputSystem.AddDevice<Touchscreen>();
            yield return TouchDrag(TrayPoint("battery"), grid.GridToScreen(new Vector2(1, 1)));
            yield return TouchDrag(TrayPoint("led"), grid.GridToScreen(new Vector2(4, 1)));
            Assert.That(editor.Board.Components.Count, Is.EqualTo(2));
            string battery = Id("battery");
            yield return TouchDrag(TerminalPoint(new TerminalReference(battery, "negative")),
                TerminalPoint(new TerminalReference(Id("led"), "anode")));
            Assert.That(editor.Board.Wires.Count, Is.EqualTo(1));
            yield return TouchDrag(BodyPoint(battery), grid.GridToScreen(new Vector2(1.5f, 3.5f)));
            Assert.That(editor.Board.FindComponent(battery).Position, Is.EqualTo(new Vector2Int(0, 2)));
        }

        [UnityTest]
        public IEnumerator ExistingNavigationRemainsUsable()
        {
            yield return ClickButton("Back to Levels");
            for (int i = 0; i < 10 && SceneManager.GetActiveScene().name != "LevelSelect"; i++) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("LevelSelect"));
            yield return ClickButton("levels.preview");
            for (int i = 0; i < 10 && SceneManager.GetActiveScene().name != "Gameplay"; i++) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Gameplay"));
            Assert.That(Object.FindAnyObjectByType<CircuitEditorController>().Board.Components, Is.Empty);
        }

        private string Id(string definition) => editor.Board.Components.Single(c => c.Definition.StableId == definition).Id;
        private Vector2 TerminalPoint(TerminalReference terminal) => grid.GridToScreen(editor.Board.GetTerminalPosition(terminal));
        private Vector2 BodyPoint(string id)
        {
            PlacedComponent component = editor.Board.FindComponent(id);
            return grid.GridToScreen((Vector2)component.Position +
                (Vector2)GridGeometry.RotatedSize(component.Definition.Footprint, component.Rotation) * .75f);
        }
        private Vector2 TrayPoint(string id)
        {
            Transform items = Object.FindAnyObjectByType<ComponentTray>().transform.Find("Items");
            return RectTransformUtility.WorldToScreenPoint(null, items.Find(id).position);
        }
        private Button Button(string name) => Object.FindObjectsByType<Button>().Single(b => b.name == name);
        private IEnumerator ClickButton(string name)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            Button button = Button(name);
            Assert.That(button.interactable, Is.True, name + " must be enabled.");
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, button.transform.position));
        }
        private IEnumerator Place(string definition, Vector2Int position)
        {
            var component = Object.FindAnyObjectByType<ComponentTray>().Definitions.Single(d => d.StableId == definition);
            int before = editor.Board.Components.Count;
            yield return Drag(TrayPoint(definition), grid.GridToScreen((Vector2)position + (Vector2)component.Footprint * .5f));
            Assert.That(editor.Board.Components.Count, Is.EqualTo(before + 1), definition + " placement via mouse failed.");
        }
        private IEnumerator Click(Vector2 point)
        {
            yield return MouseAt(point, false);
            yield return MouseAt(point, true);
            yield return MouseAt(point, false);
        }
        private IEnumerator Drag(Vector2 from, Vector2 to)
        {
            yield return MouseAt(from, false);
            yield return MouseAt(from, true);
            for (int i = 1; i <= 8; i++) yield return MouseAt(Vector2.Lerp(from, to, i / 8f), true);
            yield return MouseAt(to, false);
        }
        private IEnumerator MouseAt(Vector2 point, bool pressed)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, pressed));
            yield return null;
            yield return null;
        }
        private IEnumerator TouchDrag(Vector2 from, Vector2 to)
        {
            yield return TouchAt(from, TouchPhase.Began);
            for (int i = 1; i <= 8; i++) yield return TouchAt(Vector2.Lerp(from, to, i / 8f), TouchPhase.Moved);
            yield return TouchAt(to, TouchPhase.Ended);
        }
        private IEnumerator TouchAt(Vector2 point, TouchPhase phase)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, position = point, phase = phase });
            yield return null;
            yield return null;
        }
    }
}
