using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CircuitPuzzle.Circuit.Presentation
{
    /// <summary>A direct visual connection. Endpoints come from terminal views, never simulation.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WireView : MaskableGraphic, IPointerClickHandler, ICanvasRaycastFilter
    {
        [SerializeField, Min(1f)] private float thickness = 7f;
        [SerializeField, Min(1f)] private float selectedThickness = 11f;
        [SerializeField, Min(1f)] private float hitTolerance = 28f;
        [SerializeField] private Color wireColor = new Color(0.46f, 0.87f, 0.92f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0.34f, 1f);
        [SerializeField] private Color validPreviewColor = new Color(0.49f, 0.96f, 0.71f, 0.9f);
        [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.43f, 0.38f, 0.85f);

        private Action<string> onSelected;
        private bool selected;
        private bool preview;
        private bool previewValid;

        public string WireId { get; private set; }
        public Vector2 StartPoint { get; private set; }
        public Vector2 EndPoint { get; private set; }
        public Func<Vector2, bool> IsOverTerminal;

        public void Bind(string id, Action<string> selectedCallback)
        {
            WireId = id;
            onSelected = selectedCallback;
            preview = false;
            selected = false;
            raycastTarget = true;
            SetVerticesDirty();
        }

        /// <summary>Endpoints are in this graphic's local space; stretch its rect over the board.</summary>
        public void SetEndpoints(Vector2 start, Vector2 end)
        {
            if (StartPoint == start && EndPoint == end)
                return;
            StartPoint = start;
            EndPoint = end;
            SetVerticesDirty();
        }

        public void SetSelected(bool value)
        {
            if (selected == value)
                return;
            selected = value;
            SetVerticesDirty();
        }

        public void SetPreview(bool valid)
        {
            bool changed = !preview || previewValid != valid;
            preview = true;
            previewValid = valid;
            raycastTarget = false;
            if (changed)
                SetVerticesDirty();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!preview && eventData.button == PointerEventData.InputButton.Left)
                onSelected?.Invoke(WireId);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (preview || !raycastTarget ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPoint, eventCamera, out Vector2 localPoint) ||
                !rectTransform.rect.Contains(localPoint))
                return false;

            // A wire crossing a terminal must not steal the terminal's touch target.
            if (IsOverTerminal != null && IsOverTerminal(localPoint)) return false;

            Vector2 segment = EndPoint - StartPoint;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.0001f)
                return false;

            float along = Mathf.Clamp01(Vector2.Dot(localPoint - StartPoint, segment) / lengthSquared);
            Vector2 closest = StartPoint + segment * along;
            float radius = Mathf.Max(hitTolerance, (selected ? selectedThickness : thickness) * 0.5f);
            return (localPoint - closest).sqrMagnitude <= radius * radius;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Vector2 segment = EndPoint - StartPoint;
            if (segment.sqrMagnitude < 0.0001f)
                return;

            float width = selected && !preview ? selectedThickness : thickness;
            Color tint = preview ? (previewValid ? validPreviewColor : invalidPreviewColor) :
                (selected ? selectedColor : wireColor);
            Vector2 normal = new Vector2(-segment.y, segment.x).normalized * (width * 0.5f);
            // A narrow dark edge keeps direct wires legible over both the board and component bodies.
            AddSegment(vertexHelper, normal.normalized * (width * .5f + 2),
                new Color(.02f, .04f, .055f, tint.a * color.a));
            AddSegment(vertexHelper, normal, tint * color);

            // Width and a light center stripe make selection legible without relying on color alone.
            if (selected && !preview)
                AddSegment(vertexHelper, normal.normalized, new Color(1f, 1f, 0.93f, color.a));
        }

        private void AddSegment(VertexHelper helper, Vector2 normal, Color tint)
        {
            int first = helper.currentVertCount;
            helper.AddVert(StartPoint - normal, tint, Vector2.zero);
            helper.AddVert(StartPoint + normal, tint, Vector2.zero);
            helper.AddVert(EndPoint + normal, tint, Vector2.zero);
            helper.AddVert(EndPoint - normal, tint, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2);
            helper.AddTriangle(first + 2, first + 3, first);
        }
    }
}
