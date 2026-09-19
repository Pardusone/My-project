using UnityEngine;

namespace CircuitPuzzle.UI
{
    /// <summary>
    /// Keeps content inside the screen safe area. Place this panel directly under
    /// a full-screen, Screen Space - Overlay Canvas; CanvasScaler is supported.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform panel;
        private Rect lastSafeArea;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        private void OnEnable()
        {
            panel = GetComponent<RectTransform>();
            ApplySafeArea(true);
        }

        private void Update()
        {
            // Safe area can change without a resolution change on mobile devices.
            // Polling only changes the layout when the reported screen values change.
            ApplySafeArea(false);
        }

        private void ApplySafeArea(bool force)
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            if (!force && safeArea == lastSafeArea &&
                screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
            {
                return;
            }

            panel.anchorMin = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
            panel.anchorMax = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            lastSafeArea = safeArea;
            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;
        }
    }
}
