using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CircuitPuzzle.Circuit.Presentation
{
    /// <summary>Draws the construction grid and converts between pointer and board coordinates.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GridBoardView : MaskableGraphic, IPointerClickHandler
    {
        [SerializeField, Min(1)] private int columns = 6;
        [SerializeField, Min(1)] private int rows = 8;
        [SerializeField, Min(1f)] private float cellSize = 156f;
        [SerializeField] private Color backgroundColor = new Color(0.035f, 0.075f, 0.095f, 1f);
        [SerializeField] private Color gridColor = new Color(0.09f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color borderColor = new Color(0.23f, 0.36f, 0.40f, 1f);

        private float actualCellSize = 156f;
        private bool applyingLayout;

        public event Action LayoutChanged;
        public event Action<PointerEventData> BackgroundClicked;

        public int Columns => Mathf.Max(1, columns);
        public int Rows => Mathf.Max(1, rows);
        public float CellSize => actualCellSize;
        public RectTransform Rect => rectTransform;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = true;
            FitToParent();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            FitToParent();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            columns = Mathf.Max(1, columns);
            rows = Mathf.Max(1, rows);
            cellSize = Mathf.Max(1f, cellSize);
            base.OnValidate();
        }
#endif

        private void FitToParent()
        {
            if (applyingLayout || !(transform.parent is RectTransform parent))
                return;

            Rect available = parent.rect;
            if (available.width <= 0f || available.height <= 0f)
                return;

            float nextCellSize = Mathf.Min(Mathf.Max(1f, cellSize),
                available.width / Columns, available.height / Rows);
            Vector2 nextSize = new Vector2(Columns * nextCellSize, Rows * nextCellSize);
            Vector2 center = new Vector2(0.5f, 0.5f);
            bool changed = !Mathf.Approximately(actualCellSize, nextCellSize) ||
                rectTransform.sizeDelta != nextSize || rectTransform.anchorMin != center ||
                rectTransform.anchorMax != center || rectTransform.pivot != center ||
                rectTransform.anchoredPosition != Vector2.zero;
            if (!changed)
                return;

            // RectTransform setters trigger this callback again. Apply the full layout once,
            // then notify consumers so components and wire endpoints resize together.
            applyingLayout = true;
            actualCellSize = nextCellSize;
            rectTransform.anchorMin = center;
            rectTransform.anchorMax = center;
            rectTransform.pivot = center;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = nextSize;
            applyingLayout = false;
            SetVerticesDirty();
            LayoutChanged?.Invoke();
        }

        /// <summary>Grid (0, 0) is the board's lower left corner; cell centers use halves.</summary>
        public Vector2 GridToLocal(Vector2 gridPoint)
        {
            return rectTransform.rect.min + gridPoint * actualCellSize;
        }

        public Vector2 ScreenToGrid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
                return new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            return (localPoint - rectTransform.rect.min) / actualCellSize;
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera = null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, eventCamera);
        }

        public Vector2 GridToScreen(Vector2 gridPoint, Camera eventCamera = null)
        {
            return RectTransformUtility.WorldToScreenPoint(eventCamera,
                rectTransform.TransformPoint(GridToLocal(gridPoint)));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                BackgroundClicked?.Invoke(eventData);
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect bounds = rectTransform.rect;
            AddQuad(vertexHelper, bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax, backgroundColor * color);

            const float halfLine = 0.5f;
            for (int x = 1; x < Columns; x++)
            {
                float position = bounds.xMin + x * actualCellSize;
                AddQuad(vertexHelper, position - halfLine, bounds.yMin,
                    position + halfLine, bounds.yMax, gridColor * color);
            }
            for (int y = 1; y < Rows; y++)
            {
                float position = bounds.yMin + y * actualCellSize;
                AddQuad(vertexHelper, bounds.xMin, position - halfLine,
                    bounds.xMax, position + halfLine, gridColor * color);
            }

            const float border = 2f;
            Color tint = borderColor * color;
            AddQuad(vertexHelper, bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMin + border, tint);
            AddQuad(vertexHelper, bounds.xMin, bounds.yMax - border, bounds.xMax, bounds.yMax, tint);
            AddQuad(vertexHelper, bounds.xMin, bounds.yMin, bounds.xMin + border, bounds.yMax, tint);
            AddQuad(vertexHelper, bounds.xMax - border, bounds.yMin, bounds.xMax, bounds.yMax, tint);
        }

        private static void AddQuad(VertexHelper helper, float left, float bottom,
            float right, float top, Color tint)
        {
            int first = helper.currentVertCount;
            helper.AddVert(new Vector3(left, bottom), tint, Vector2.zero);
            helper.AddVert(new Vector3(left, top), tint, Vector2.zero);
            helper.AddVert(new Vector3(right, top), tint, Vector2.zero);
            helper.AddVert(new Vector3(right, bottom), tint, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2);
            helper.AddTriangle(first + 2, first + 3, first);
        }
    }
}
