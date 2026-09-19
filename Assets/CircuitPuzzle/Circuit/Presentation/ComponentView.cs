using System.Collections.Generic;
using CircuitPuzzle.Circuit.Input;
using CircuitPuzzle.Components;
using UnityEngine;
using UnityEngine.UI;

namespace CircuitPuzzle.Circuit.Presentation
{
    public sealed class ComponentView : MonoBehaviour
    {
        public string ComponentId { get; private set; }
        public PointerDragHandle Handle { get; private set; }
        public IReadOnlyList<TerminalView> Terminals => terminals;
        private readonly List<TerminalView> terminals = new List<TerminalView>();
        private readonly List<RectTransform> leads = new List<RectTransform>();
        private ComponentDefinition definition;
        private RectTransform rect;
        private Image body;
        private Outline outline;
        private Text title;
        private CanvasGroup group;

        public void Initialize(string id, ComponentDefinition componentDefinition, bool preview = false)
        {
            ComponentId = id;
            definition = componentDefinition;
            rect = (RectTransform)transform;
            gameObject.AddComponent<Image>().color = Color.clear;
            Handle = gameObject.AddComponent<PointerDragHandle>();
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = !preview;
            foreach (TerminalDefinition terminal in definition.Terminals)
            {
                Image lead = EditorViewElements.Rect(terminal.Id + " Lead", transform).gameObject.AddComponent<Image>();
                lead.raycastTarget = false;
                lead.color = new Color(.34f, .49f, .52f);
                leads.Add(lead.rectTransform);
            }
            body = EditorViewElements.Rect("Body", transform).gameObject.AddComponent<Image>();
            body.raycastTarget = false;
            body.sprite = definition.Sprite;
            body.preserveAspect = true;
            body.color = Color.Lerp(new Color(.10f, .16f, .20f), definition.Tint, .42f);
            outline = body.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(3, -3);
            title = EditorViewElements.Label("Name", transform, 25);
            title.text = definition.DisplayName;
            title.fontStyle = FontStyle.Bold;
            foreach (TerminalDefinition terminal in definition.Terminals)
            {
                var view = EditorViewElements.Rect(terminal.Id, transform).gameObject.AddComponent<TerminalView>();
                view.Initialize(new TerminalReference(id, terminal.Id), terminal);
                terminals.Add(view);
            }
            SetSelected(false);
        }

        public void SetGeometry(Vector2Int position, int rotation, GridBoardView grid)
        {
            Vector2Int size = GridGeometry.RotatedSize(definition.Footprint, rotation);
            rect.sizeDelta = (Vector2)size * grid.CellSize;
            rect.anchoredPosition = grid.GridToLocal((Vector2)position + (Vector2)size * .5f);
            // Keep the placement and drag footprint intact; only the placeholder body is compact.
            Vector2 bodySize = new Vector2(Mathf.Max(.75f, definition.Footprint.x - 1.05f),
                Mathf.Min(.8f, definition.Footprint.y)) * grid.CellSize;
            body.rectTransform.sizeDelta = bodySize;
            body.rectTransform.localRotation = Quaternion.Euler(0, 0, rotation * 90);
            bool vertical = rotation % 2 != 0;
            float labelWidth = (vertical ? bodySize.y : bodySize.x) - 12;
            title.fontSize = Mathf.Clamp(Mathf.RoundToInt(grid.CellSize * .15f), 20, 25);
            title.rectTransform.sizeDelta = new Vector2(Mathf.Max(60, labelWidth), 60);
            for (int i = 0; i < terminals.Count; i++)
            {
                Vector2 local = GridGeometry.RotateLocalPoint(definition.Terminals[i].LocalPosition,
                    definition.Footprint, rotation);
                Vector2 terminalPoint = (local - (Vector2)size * .5f) * grid.CellSize;
                terminals[i].Rect.anchoredPosition = terminalPoint;
                leads[i].anchoredPosition = terminalPoint * .5f;
                leads[i].sizeDelta = new Vector2(terminalPoint.magnitude, 3);
                leads[i].localRotation = Quaternion.Euler(0, 0,
                    Mathf.Atan2(terminalPoint.y, terminalPoint.x) * Mathf.Rad2Deg);
                terminals[i].SetCaptionLayout(vertical);
            }
        }

        public void SetSelected(bool selected)
        {
            outline.enabled = selected;
            outline.effectColor = new Color(1f, .82f, .3f);
            foreach (TerminalView terminal in terminals) terminal.SetDetailsVisible(selected);
        }

        public void SetFaded(bool faded) => group.alpha = faded ? .25f : 1f;
        public void SetPreviewState(bool valid)
        {
            group.alpha = .8f;
            outline.enabled = true;
            outline.effectColor = valid ? new Color(.2f, 1f, .7f) : new Color(1f, .3f, .25f);
        }
    }
}
