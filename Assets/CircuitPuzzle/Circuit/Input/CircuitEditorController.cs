using System.Collections;
using CircuitPuzzle.Circuit.Presentation;
using CircuitPuzzle.Components;
using CircuitPuzzle.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CircuitPuzzle.Circuit.Input
{
    /// <summary>Owns one construction session and connects UI/input to data and presentation.</summary>
    public sealed class CircuitEditorController : MonoBehaviour
    {
        [SerializeField] private GridBoardView boardView;
        [SerializeField] private BoardPresentation boardPresentation;
        [SerializeField] private ComponentTray tray;
        [SerializeField] private UiTextCatalog catalog;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text selectionLabel;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button deleteButton;
        private EditorHistory history;
        private EditorDragSession drag;
        private BoardSnapshot initial;
        private string selectedComponent;
        private string selectedWire;
        private Coroutine statusTimeout;
        public CircuitBoard Board { get; private set; }

        private void Awake()
        {
            Board = new CircuitBoard(boardView.Columns, boardView.Rows);
            initial = Board.Capture();
            history = new EditorHistory(Board);
            drag = new EditorDragSession(Board, history, boardView, boardPresentation,
                SetStatus, SelectComponent, SelectWire);
            boardPresentation.Bind(Board, boardView, BindComponent, SelectWire);
            tray.Bind(drag.BeginPlacement, drag.Update, drag.End, drag.CancelForPointer, InspectTrayItem);
            Board.Changed += OnBoardChanged;
            history.Changed += RefreshControls;
            drag.Changed += RefreshControls;
            boardView.LayoutChanged += boardPresentation.RefreshGeometry;
            boardView.BackgroundClicked += OnBackgroundClicked;
            SetStatus("editor.ready");
            RefreshControls();
        }

        private void BindComponent(ComponentView component)
        {
            component.Handle.Pressed = data => SelectComponent(component.ComponentId);
            component.Handle.DragStarted = data => drag.BeginMove(component.ComponentId, data);
            component.Handle.Dragged = drag.Update;
            component.Handle.Released = drag.End;
            component.Handle.Cancelled = drag.CancelForPointer;
            foreach (TerminalView terminal in component.Terminals)
            {
                terminal.Handle.Pressed = data =>
                {
                    if (drag.IsActive) return;
                    SelectComponent(component.ComponentId);
                    drag.BeginWire(terminal.Reference, data);
                };
                terminal.Handle.Dragged = drag.Update;
                terminal.Handle.Released = drag.End;
                terminal.Handle.Cancelled = drag.CancelForPointer;
            }
        }

        private void OnBackgroundClicked(UnityEngine.EventSystems.PointerEventData data)
        {
            if (!drag.IsActive)
            {
                selectedComponent = selectedWire = null;
                RefreshControls();
            }
        }

        private void SelectComponent(string id)
        {
            if (drag.IsActive) return;
            selectedComponent = id;
            selectedWire = null;
            RefreshControls();
        }

        private void SelectWire(string id)
        {
            if (drag.IsActive) return;
            selectedWire = id;
            selectedComponent = null;
            RefreshControls();
        }

        private void InspectTrayItem(ComponentDefinition definition)
        {
            if (!drag.IsActive) ShowStatus(definition.Description);
        }

        public void RotateSelected()
        {
            if (drag.IsActive || selectedComponent == null) return;
            bool changed = history.Execute(() => Board.TryRotateComponent(selectedComponent));
            SetStatus(changed ? "editor.rotated" : "editor.rotate.invalid");
        }

        public void DeleteSelected()
        {
            if (drag.IsActive) return;
            bool changed = selectedComponent != null
                ? history.Execute(() => Board.TryRemoveComponent(selectedComponent))
                : selectedWire != null && history.Execute(() => Board.TryRemoveWire(selectedWire));
            if (changed) SetStatus("editor.deleted");
        }

        public void Undo()
        {
            drag.Cancel();
            selectedComponent = selectedWire = null;
            SetStatus(history.Undo() ? "editor.undo.done" : "editor.nothing");
            RefreshControls();
        }

        public void ResetBoard()
        {
            drag.Cancel();
            selectedComponent = selectedWire = null;
            history.ResetTo(initial);
            SetStatus("editor.reset.done");
            RefreshControls();
        }

        private void OnBoardChanged()
        {
            if (Board.FindComponent(selectedComponent) == null) selectedComponent = null;
            if (Board.FindWire(selectedWire) == null) selectedWire = null;
            boardPresentation.Rebuild();
            RefreshControls();
        }

        private void RefreshControls()
        {
            if (history == null) return;
            bool idle = !drag.IsActive;
            undoButton.interactable = idle && history.CanUndo;
            resetButton.interactable = idle && (Board.Components.Count > 0 || Board.Wires.Count > 0);
            rotateButton.interactable = idle && selectedComponent != null;
            deleteButton.interactable = idle && (selectedComponent != null || selectedWire != null);
            PlacedComponent component = Board.FindComponent(selectedComponent);
            selectionLabel.text = component != null
                ? string.Format(catalog.Get("editor.selection.component"), component.Definition.DisplayName)
                : string.Empty;
            boardPresentation.SetSelection(selectedComponent, selectedWire);
        }

        private void SetStatus(string key) => ShowStatus(catalog.Get(key));

        private void ShowStatus(string message)
        {
            if (statusTimeout != null) StopCoroutine(statusTimeout);
            statusLabel.text = message;
            if (isActiveAndEnabled) statusTimeout = StartCoroutine(ClearStatusAfterDelay());
        }

        private IEnumerator ClearStatusAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            statusLabel.text = string.Empty;
            statusTimeout = null;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) drag?.Cancel(); }
        private void OnApplicationPause(bool paused) { if (paused) drag?.Cancel(); }
        private void OnDisable()
        {
            drag?.Cancel();
            if (statusTimeout != null) StopCoroutine(statusTimeout);
            statusTimeout = null;
            if (statusLabel != null) statusLabel.text = string.Empty;
        }

        private void OnDestroy()
        {
            if (Board == null) return;
            Board.Changed -= OnBoardChanged;
            history.Changed -= RefreshControls;
            drag.Changed -= RefreshControls;
            boardView.LayoutChanged -= boardPresentation.RefreshGeometry;
            boardView.BackgroundClicked -= OnBackgroundClicked;
        }
    }
}
