using System;
using System.Collections.Generic;
using System.IO;
using CircuitPuzzle.Circuit.Input;
using CircuitPuzzle.Circuit.Presentation;
using CircuitPuzzle.Components;
using CircuitPuzzle.Core;
using CircuitPuzzle.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CircuitPuzzle.Editor
{
    /// <summary>Opt-in Phase 2 authoring through Unity APIs; never overwrites an existing editor.</summary>
    public static class CircuitEditorSetup
    {
        private const string Root = "Assets/CircuitPuzzle";
        private const string GameplayPath = Root + "/Scenes/Gameplay.unity";
        private const string LevelSelectPath = Root + "/Scenes/LevelSelect.unity";
        private const string CatalogPath = Root + "/Data/Localization/EnglishUi.asset";
        private static readonly Color Foreground = new Color(0.91f, 0.97f, 0.96f);

        [MenuItem("Tools/Circuit Puzzle/Setup Phase 2 Circuit Editor")]
        public static void SetUp() => SetUp(false);

        [MenuItem("Tools/Circuit Puzzle/Apply Phase 2.1 Compact Layout")]
        public static void ApplyUxCleanup() => SetUp(true);

        private static void SetUp(bool applyUxCleanup)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before running circuit editor setup.");
            if (!File.Exists(GameplayPath))
                throw new FileNotFoundException("Phase 1 Gameplay scene is required.", GameplayPath);

            Scene scene = SceneManager.GetSceneByPath(GameplayPath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (!openedHere && scene.isDirty)
                throw new InvalidOperationException("Save or revert Gameplay changes before running setup. No open scene was changed.");
            if (openedHere)
                PrepareAdditiveOpen();

            Scene previousActive = SceneManager.GetActiveScene();
            if (openedHere)
                scene = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Additive);

            RectTransform editorRoot = null;
            GameObject placeholder = null;
            bool placeholderWasActive = false;
            EventSystem eventSystem = null;
            GameObject previousFirstSelected = null;
            bool saved = false;
            try
            {
                SceneManager.SetActiveScene(scene);
                Canvas canvas = FindInScene<Canvas>(scene);
                SafeAreaPanel safeArea = FindInScene<SafeAreaPanel>(scene);
                SceneNavigation navigation = FindInScene<SceneNavigation>(scene);
                eventSystem = FindInScene<EventSystem>(scene);
                if (canvas == null || safeArea == null || navigation == null || eventSystem == null ||
                    !safeArea.transform.IsChildOf(canvas.transform))
                    throw new InvalidOperationException("Gameplay must retain its Phase 1 Canvas, Safe Area, navigation and EventSystem.");
                previousFirstSelected = eventSystem.firstSelectedGameObject;

                Transform existing = safeArea.transform.Find("Circuit Editor");
                if (existing != null)
                {
                    if (existing.GetComponent<CircuitEditorController>() == null)
                        throw new InvalidOperationException("An unrelated object is named 'Circuit Editor'. Rename it before setup.");
                    if (applyUxCleanup) ApplyCompactLayout(existing);
                    foreach (GridBoardView existingGrid in existing.GetComponentsInChildren<GridBoardView>(true))
                    {
                        if (existingGrid.GetComponent<CanvasRenderer>() != null) continue;
                        existingGrid.gameObject.AddComponent<CanvasRenderer>();
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                    // Unity can auto-repair RequireComponent on load without marking the scene dirty.
                    if (!EditorSceneManager.SaveScene(scene, GameplayPath))
                        throw new IOException("Could not save the grid renderer repair.");
                    Debug.Log("Phase 2 Circuit Editor already exists. Existing scene content and authored assets were preserved.");
                    return;
                }

                UiTextCatalog catalog = AssetDatabase.LoadAssetAtPath<UiTextCatalog>(CatalogPath);
                if (catalog == null)
                    throw new InvalidOperationException("The Phase 1 English UI catalog is required: " + CatalogPath);

                EnsureFolder(Root + "/Data/Components");
                ComponentDefinition[] definitions = CreateDefinitions();
                AddMissingUiText(catalog);

                editorRoot = CreateRect("Circuit Editor", safeArea.transform);
                Anchor(editorRoot, Vector2.zero, Vector2.one);
                CircuitEditorController controller = editorRoot.gameObject.AddComponent<CircuitEditorController>();

                RectTransform header = Area("Header", editorRoot, 0.035f, 0.925f, 0.965f, 0.99f);
                Text title = Label(header, "Title", catalog, "editor.title", 48);
                Anchor(title.rectTransform, Vector2.zero, new Vector2(0.64f, 1));
                title.alignment = TextAnchor.MiddleLeft;
                Button back = Button(header, "Back to Levels", catalog, "navigation.levels", 30);
                Anchor(back.GetComponent<RectTransform>(), new Vector2(0.66f, 0), Vector2.one);
                UnityEventTools.AddStringPersistentListener(back.onClick, navigation.LoadScene, LevelSelectPath);

                Text subtitle = Label(editorRoot, "Instructions", catalog, "editor.subtitle", 27);
                Anchor(subtitle.rectTransform, new Vector2(0.035f, 0.88f), new Vector2(0.965f, 0.924f));

                RectTransform viewport = Area("Board Area", editorRoot, 0.035f, 0.29f, 0.965f, 0.88f);
                RectTransform boardRect = CreateRect("Board", viewport);
                boardRect.anchorMin = boardRect.anchorMax = new Vector2(0.5f, 0.5f);
                boardRect.pivot = new Vector2(0.5f, 0.5f);
                boardRect.sizeDelta = new Vector2(936, 1248);
                GridBoardView boardView = boardRect.gameObject.AddComponent<GridBoardView>();
                var grid = new SerializedObject(boardView);
                Required(grid, "columns").intValue = 6;
                Required(grid, "rows").intValue = 8;
                Required(grid, "cellSize").floatValue = 156;
                grid.ApplyModifiedPropertiesWithoutUndo();
                boardView.raycastTarget = true;

                // Wires stay visible over component bodies; their hit filter yields to terminals.
                RectTransform componentLayer = Layer("Components", boardRect);
                RectTransform wireLayer = Layer("Wires", boardRect);
                RectTransform previewLayer = Layer("Preview", boardRect);
                previewLayer.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
                BoardPresentation presentation = boardRect.gameObject.AddComponent<BoardPresentation>();
                var display = new SerializedObject(presentation);
                Required(display, "wireLayer").objectReferenceValue = wireLayer;
                Required(display, "componentLayer").objectReferenceValue = componentLayer;
                Required(display, "previewLayer").objectReferenceValue = previewLayer;
                display.ApplyModifiedPropertiesWithoutUndo();

                Text selection = Label(editorRoot, "Selection", null, null, 29);
                Anchor(selection.rectTransform, new Vector2(0.035f, 0.254f), new Vector2(0.965f, 0.286f));
                selection.text = catalog.Get("editor.selection.none");
                Text status = Label(editorRoot, "Status", null, null, 28);
                Anchor(status.rectTransform, new Vector2(0.035f, 0.219f), new Vector2(0.965f, 0.254f));
                status.text = catalog.Get("editor.ready");

                RectTransform toolbar = Area("Toolbar", editorRoot, 0.035f, 0.15f, 0.965f, 0.213f);
                HorizontalLayoutGroup toolbarLayout = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
                ConfigureHorizontal(toolbarLayout, 14);
                Button undo = Button(toolbar, "Undo", catalog, "editor.undo", 34);
                Button reset = Button(toolbar, "Reset", catalog, "editor.reset", 34);
                Button rotate = Button(toolbar, "Rotate", catalog, "editor.rotate", 34);
                Button delete = Button(toolbar, "Delete", catalog, "editor.delete", 34);
                UnityEventTools.AddPersistentListener(undo.onClick, controller.Undo);
                UnityEventTools.AddPersistentListener(reset.onClick, controller.ResetBoard);
                UnityEventTools.AddPersistentListener(rotate.onClick, controller.RotateSelected);
                UnityEventTools.AddPersistentListener(delete.onClick, controller.DeleteSelected);

                RectTransform trayArea = Area("Component Tray", editorRoot, 0.035f, 0.015f, 0.965f, 0.14f);
                Text trayTitle = Label(trayArea, "Tray Title", catalog, "editor.tray", 25);
                Anchor(trayTitle.rectTransform, new Vector2(0, 0.82f), Vector2.one);
                RectTransform items = Area("Items", trayArea, 0, 0, 1, 0.8f);
                ConfigureHorizontal(items.gameObject.AddComponent<HorizontalLayoutGroup>(), 12);
                ComponentTray tray = trayArea.gameObject.AddComponent<ComponentTray>();
                var trayData = new SerializedObject(tray);
                Required(trayData, "itemsRoot").objectReferenceValue = items;
                SerializedProperty available = Required(trayData, "definitions");
                available.arraySize = definitions.Length;
                for (int i = 0; i < definitions.Length; i++)
                    available.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
                trayData.ApplyModifiedPropertiesWithoutUndo();

                var input = new SerializedObject(controller);
                Required(input, "boardView").objectReferenceValue = boardView;
                Required(input, "boardPresentation").objectReferenceValue = presentation;
                Required(input, "tray").objectReferenceValue = tray;
                Required(input, "catalog").objectReferenceValue = catalog;
                Required(input, "statusLabel").objectReferenceValue = status;
                Required(input, "selectionLabel").objectReferenceValue = selection;
                Required(input, "undoButton").objectReferenceValue = undo;
                Required(input, "resetButton").objectReferenceValue = reset;
                Required(input, "rotateButton").objectReferenceValue = rotate;
                Required(input, "deleteButton").objectReferenceValue = delete;
                input.ApplyModifiedPropertiesWithoutUndo();
                ApplyCompactLayout(editorRoot);

                // Keep the original placeholder available for reference and preserve unrelated children.
                Transform content = safeArea.transform.Find("Content");
                if (content != null && content.childCount == 3 && content.Find("Title") != null &&
                    content.Find("Description") != null && content.Find("navigation.levels") != null)
                {
                    placeholder = content.gameObject;
                    placeholderWasActive = placeholder.activeSelf;
                    placeholder.SetActive(false);
                }
                eventSystem.firstSelectedGameObject = back.gameObject;
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, GameplayPath))
                    throw new IOException("Could not save the Phase 2 Gameplay scene.");
                saved = true;
                Debug.Log("Phase 2 Circuit Editor ready. Open Gameplay and enter Play Mode. No simulation was added.");
            }
            finally
            {
                if (!saved && editorRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(editorRoot.gameObject);
                    if (placeholder != null)
                        placeholder.SetActive(placeholderWasActive);
                    if (eventSystem != null)
                        eventSystem.firstSelectedGameObject = previousFirstSelected;
                }
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ApplyCompactLayout(Transform root)
        {
            // Validate the known hierarchy before applying this explicit layout migration.
            string[] paths = { "Header", "Header/Title", "Header/Back to Levels", "Instructions",
                "Board Area", "Board Area/Board", "Selection", "Status", "Toolbar",
                "Component Tray", "Component Tray/Tray Title", "Component Tray/Items" };
            foreach (string path in paths)
                if (root.Find(path) == null)
                    throw new InvalidOperationException("Expected editor object missing: " + path);

            Place("Header", 0.025f, 0.930f, 0.975f, 0.994f);
            Place("Instructions", 0.025f, 0.900f, 0.975f, 0.929f);
            Place("Board Area", 0.025f, 0.199f, 0.975f, 0.897f);
            Place("Selection", 0.025f, 0.175f, 0.37f, 0.199f);
            Place("Status", 0.39f, 0.175f, 0.975f, 0.199f);
            Place("Toolbar", 0.025f, 0.110f, 0.975f, 0.173f);
            Place("Component Tray", 0.025f, 0.010f, 0.975f, 0.104f);
            Place("Component Tray/Tray Title", 0, 0.80f, 1, 1);
            Place("Component Tray/Items", 0, 0, 1, 0.77f);
            root.Find("Header/Title").GetComponent<Text>().fontSize = 40;
            root.Find("Instructions").GetComponent<Text>().fontSize = 25;
            Text selection = root.Find("Selection").GetComponent<Text>();
            selection.fontSize = 23;
            selection.alignment = TextAnchor.MiddleLeft;
            selection.text = string.Empty;
            Text status = root.Find("Status").GetComponent<Text>();
            status.fontSize = 23;
            status.alignment = TextAnchor.MiddleRight;
            status.color = new Color(0.72f, 0.83f, 0.85f);
            status.text = string.Empty;
            root.Find("Component Tray/Tray Title").GetComponent<Text>().fontSize = 22;
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                button.GetComponent<Image>().color = new Color(0.10f, 0.20f, 0.24f);
                button.GetComponentInChildren<Text>().fontSize = 30;
            }
            var grid = new SerializedObject(root.Find("Board Area/Board").GetComponent<GridBoardView>());
            Required(grid, "cellSize").floatValue = 170;
            Required(grid, "backgroundColor").colorValue = new Color(0.035f, 0.075f, 0.095f);
            Required(grid, "gridColor").colorValue = new Color(0.09f, 0.15f, 0.18f);
            Required(grid, "borderColor").colorValue = new Color(0.23f, 0.36f, 0.40f);
            grid.ApplyModifiedPropertiesWithoutUndo();

            void Place(string path, float xMin, float yMin, float xMax, float yMax) =>
                Anchor((RectTransform)root.Find(path), new Vector2(xMin, yMin), new Vector2(xMax, yMax));
        }

        private static void PrepareAdditiveOpen()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!string.IsNullOrEmpty(scene.path))
                    continue;
                string mainMenu = Root + "/Scenes/MainMenu.unity";
                if (Application.isBatchMode && SceneManager.sceneCount == 1 && !scene.isDirty && File.Exists(mainMenu))
                {
                    EditorSceneManager.OpenScene(mainMenu, OpenSceneMode.Single);
                    return;
                }
                throw new InvalidOperationException("Save untitled scenes before running Phase 2 setup. No open scene was changed.");
            }
        }

        private static T FindInScene<T>(Scene scene) where T : UnityEngine.Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }

        private static ComponentDefinition[] CreateDefinitions()
        {
            return new[]
            {
                Definition("Battery", "battery", "Battery", ComponentType.Battery, new Color(0.26f, 0.58f, 0.44f), 2,
                    "Positive and negative terminals.", "positive", "Positive", "+", "negative", "Negative", "-"),
                Definition("Resistor", "resistor", "Resistor", ComponentType.Resistor, new Color(0.70f, 0.48f, 0.28f), 3,
                    "Two labelled connection terminals.", "a", "A", "A", "b", "B", "B"),
                Definition("LED", "led", "LED", ComponentType.Led, new Color(0.44f, 0.44f, 0.70f), 2,
                    "Anode and cathode terminals.", "anode", "Anode", "A", "cathode", "Cathode", "K"),
                Definition("Switch", "switch", "Switch", ComponentType.Switch, new Color(0.28f, 0.53f, 0.64f), 2,
                    "Two labelled connection terminals.", "a", "A", "A", "b", "B", "B"),
                Definition("LightBulb", "light-bulb", "Light Bulb", ComponentType.LightBulb, new Color(0.69f, 0.57f, 0.26f), 2,
                    "Two labelled connection terminals.", "a", "A", "A", "b", "B", "B")
            };
        }

        private static ComponentDefinition Definition(string assetName, string id, string displayName,
            ComponentType type, Color tint, int width, string description,
            string firstId, string firstName, string firstLabel, string secondId, string secondName, string secondLabel)
        {
            string path = Root + "/Data/Components/" + assetName + ".asset";
            ComponentDefinition existing = AssetDatabase.LoadAssetAtPath<ComponentDefinition>(path);
            if (existing != null)
                return existing;
            if (File.Exists(path))
                throw new InvalidOperationException("A different asset already exists at " + path);
            ComponentDefinition definition = ScriptableObject.CreateInstance<ComponentDefinition>();
            definition.Configure(id, displayName, type, description, string.Empty, null, tint,
                new Vector2Int(width, 2), new[] { 0, 1, 2, 3 }, new[]
                {
                    new TerminalDefinition(firstId, firstName, firstLabel, new Vector2(0.35f, 1), firstId),
                    new TerminalDefinition(secondId, secondName, secondLabel, new Vector2(width - 0.35f, 1), secondId)
                });
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static void AddMissingUiText(UiTextCatalog catalog)
        {
            string[,] additions =
            {
                { "editor.title", "Circuit Editor" },
                { "editor.subtitle", "Drag parts onto the grid. Drag between terminals to connect." },
                { "editor.tray", "COMPONENTS - Drag to place" },
                { "editor.undo", "Undo" }, { "editor.reset", "Reset" },
                { "editor.rotate", "Rotate" }, { "editor.delete", "Delete" },
                { "editor.ready", "Choose a part from the tray." },
                { "editor.selection.none", "Nothing selected" },
                { "editor.selection.component", "{0} selected" },
                { "editor.selection.wire", "Wire selected" },
                { "editor.selection.terminal", "{0}: {1}" },
                { "editor.placement.valid", "OK - Release to place" },
                { "editor.placement.invalid", "X - Outside board or overlapping a part" },
                { "editor.wire.ready", "Drag to another terminal." },
                { "editor.wire.valid", "OK - Release to connect" },
                { "editor.wire.invalid", "X - Choose a different, unconnected terminal pair" },
                { "editor.rotate.invalid", "Cannot rotate here. Move the part away from the edge or other parts." },
                { "editor.undo.done", "Last edit undone." },
                { "editor.reset.done", "Board cleared. Undo history reset." },
                { "editor.cancelled", "Edit cancelled." },
                { "editor.placed", "Part placed." }, { "editor.moved", "Part moved." },
                { "editor.rotated", "Part rotated." }, { "editor.deleted", "Selection deleted." },
                { "editor.connected", "Wire connected." }, { "editor.nothing", "No changes to undo." }
            };
            var serialized = new SerializedObject(catalog);
            SerializedProperty entries = Required(serialized, "entries");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.arraySize; i++)
                keys.Add(entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue);
            for (int i = 0; i < additions.GetLength(0); i++)
            {
                if (!keys.Add(additions[i, 0]))
                    continue;
                int index = entries.arraySize++;
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("key").stringValue = additions[i, 0];
                entry.FindPropertyRelative("value").stringValue = additions[i, 1];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Required(SerializedObject serialized, string field)
        {
            return serialized.FindProperty(field) ?? throw new InvalidOperationException(
                serialized.targetObject.GetType().Name + " is missing its setup field: " + field);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Anchor(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Area(string name, Transform parent, float xMin, float yMin, float xMax, float yMax)
        {
            RectTransform rect = CreateRect(name, parent);
            Anchor(rect, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
            return rect;
        }

        private static RectTransform Layer(string name, Transform parent) => Area(name, parent, 0, 0, 1, 1);

        private static void ConfigureHorizontal(HorizontalLayoutGroup layout, float spacing)
        {
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
        }

        private static Text Label(Transform parent, string name, UiTextCatalog catalog, string key, int fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            Anchor(rect, Vector2.zero, Vector2.one);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Foreground;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            if (catalog != null)
            {
                UiLabel binding = rect.gameObject.AddComponent<UiLabel>();
                var serialized = new SerializedObject(binding);
                Required(serialized, "catalog").objectReferenceValue = catalog;
                Required(serialized, "key").stringValue = key;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                binding.Refresh();
            }
            return text;
        }

        private static Button Button(Transform parent, string name, UiTextCatalog catalog, string key, int fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.30f, 0.33f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.79f, 1, 0.96f);
            colors.pressedColor = new Color(0.57f, 0.84f, 0.79f);
            button.colors = colors;
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1;
            Text text = Label(rect, "Label", catalog, key, fontSize);
            text.rectTransform.offsetMin = new Vector2(8, 6);
            text.rectTransform.offsetMax = new Vector2(-8, -6);
            return button;
        }
    }
}
