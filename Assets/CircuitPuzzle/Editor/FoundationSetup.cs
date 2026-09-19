using System;
using System.Collections.Generic;
using System.IO;
using CircuitPuzzle.Core;
using CircuitPuzzle.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CircuitPuzzle.Editor
{
    /// <summary>Explicit, repeatable setup. Existing scenes and text assets are never overwritten.</summary>
    public static class FoundationSetup
    {
        private const string Root = "Assets/CircuitPuzzle";
        private const string MainMenu = Root + "/Scenes/MainMenu.unity";
        private const string LevelSelect = Root + "/Scenes/LevelSelect.unity";
        private const string Gameplay = Root + "/Scenes/Gameplay.unity";
        private const string CatalogPath = Root + "/Data/Localization/EnglishUi.asset";

        [MenuItem("Tools/Circuit Puzzle/Set Up Phase 1 Foundation")]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before running foundation setup.");

            PrepareSceneCreation();
            CreateFolders();
            UiTextCatalog catalog = GetOrCreateCatalog();
            CreateSceneIfMissing(MainMenu, "menu.title", "menu.description", catalog);
            CreateSceneIfMissing(LevelSelect, "levels.title", "levels.description", catalog);
            CreateSceneIfMissing(Gameplay, "gameplay.title", "gameplay.description", catalog);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Shared by both mobile platforms; no target switch or SDK installation required.
            // Keep existing signing, identifiers, renderer, SDK levels and build backend intact.
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 1 foundation ready. Open MainMenu and enter Play Mode to navigate the three screens.");
        }

        private static void PrepareSceneCreation()
        {
            if (File.Exists(MainMenu) && File.Exists(LevelSelect) && File.Exists(Gameplay))
                return;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene openScene = SceneManager.GetSceneAt(i);
                if (!string.IsNullOrEmpty(openScene.path))
                    continue;

                // A fresh batch editor starts with an untitled scene. Unity rejects additive
                // creation beside it, so load an existing configured scene in that case only.
                if (Application.isBatchMode && SceneManager.sceneCount == 1 && !openScene.isDirty)
                {
                    foreach (EditorBuildSettingsScene configured in EditorBuildSettings.scenes)
                    {
                        if (File.Exists(configured.path))
                        {
                            EditorSceneManager.OpenScene(configured.path, OpenSceneMode.Single);
                            return;
                        }
                    }
                }

                throw new InvalidOperationException("Save untitled scenes before running foundation setup. No open scene was changed.");
            }
        }

        private static void CreateFolders()
        {
            string[] folders =
            {
                "Core", "Circuit", "Components", "Simulation", "Levels", "Objectives",
                "UI", "Tutorial", "Save", "Audio", "Editor", "Tests", "Data",
                "Data/Localization", "Prefabs", "Scenes", "Art"
            };

            EnsureFolder(Root);
            foreach (string folder in folders)
                EnsureFolder(Root + "/" + folder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        private static UiTextCatalog GetOrCreateCatalog()
        {
            UiTextCatalog existing = AssetDatabase.LoadAssetAtPath<UiTextCatalog>(CatalogPath);
            if (existing != null)
                return existing;
            if (File.Exists(CatalogPath))
                throw new InvalidOperationException("A different asset already exists at " + CatalogPath);

            var catalog = ScriptableObject.CreateInstance<UiTextCatalog>();
            var serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("entries");
            string[,] text =
            {
                { "menu.title", "Circuit Puzzle" },
                { "menu.description", "Discover how circuits work, one connection at a time." },
                { "menu.play", "Play" },
                { "levels.title", "Level Select" },
                { "levels.description", "Your circuit journey will begin here.\nLevels are coming soon." },
                { "levels.preview", "Preview Workspace" },
                { "gameplay.title", "Circuit Workspace" },
                { "gameplay.description", "Your future workspace for exploring circuits.\nComing soon." },
                { "navigation.menu", "Main Menu" },
                { "navigation.levels", "Back to Levels" }
            };
            entries.arraySize = text.GetLength(0);
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("key").stringValue = text[i, 0];
                entry.FindPropertyRelative("value").stringValue = text[i, 1];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static void CreateSceneIfMissing(string path, string titleKey, string descriptionKey,
            UiTextCatalog catalog)
        {
            if (File.Exists(path))
                return;

            // Additive creation preserves open scenes, including their unsaved changes.
            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.04f, 0.08f, 0.11f);
                cameraObject.AddComponent<UniversalAdditionalCameraData>();

                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                RectTransform safeArea = CreateRect("Safe Area", canvasObject.transform);
                Stretch(safeArea);
                safeArea.gameObject.AddComponent<SafeAreaPanel>();

                RectTransform content = CreateRect("Content", safeArea);
                content.anchorMin = new Vector2(0.07f, 0.08f);
                content.anchorMax = new Vector2(0.93f, 0.92f);
                content.offsetMin = content.offsetMax = Vector2.zero;
                VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 32;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                CreateLabel(content, "Title", catalog, titleKey, 72, 130);
                CreateLabel(content, "Description", catalog, descriptionKey, 36, 200);
                SceneNavigation navigation = canvasObject.AddComponent<SceneNavigation>();
                Button firstButton;
                if (path == MainMenu)
                {
                    firstButton = CreateButton(content, catalog, "menu.play", navigation, LevelSelect);
                }
                else if (path == LevelSelect)
                {
                    firstButton = CreateButton(content, catalog, "levels.preview", navigation, Gameplay);
                    CreateButton(content, catalog, "navigation.menu", navigation, MainMenu);
                }
                else
                {
                    firstButton = CreateButton(content, catalog, "navigation.levels", navigation, LevelSelect);
                }

                var eventObject = new GameObject("EventSystem", typeof(EventSystem));
                // Editor creation binds the package's persistent default UI actions (mouse/touch/keyboard).
                InputSystemUIInputModule input = ObjectFactory.AddComponent<InputSystemUIInputModule>(eventObject);
                if (!AssetDatabase.Contains(input.actionsAsset))
                    throw new InvalidOperationException("The installed Input System default UI actions could not be assigned.");
                eventObject.GetComponent<EventSystem>().firstSelectedGameObject = firstButton.gameObject;

                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new IOException("Could not save foundation scene: " + path);
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Text CreateLabel(Transform parent, string name, UiTextCatalog catalog,
            string key, int fontSize, float height)
        {
            RectTransform rect = CreateRect(name, parent);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.92f, 0.96f, 0.97f);
            label.raycastTarget = false;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;

            UiLabel binding = rect.gameObject.AddComponent<UiLabel>();
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            binding.Refresh();
            return label;
        }

        private static Button CreateButton(Transform parent, UiTextCatalog catalog, string key,
            SceneNavigation navigation, string destination)
        {
            RectTransform rect = CreateRect(key, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.32f, 0.35f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 132;
            element.preferredHeight = 132;
            Text label = CreateLabel(rect, "Label", catalog, key, 42, 0);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(24, 12);
            label.rectTransform.offsetMax = new Vector2(-24, -12);
            UnityEventTools.AddStringPersistentListener(button.onClick, navigation.LoadScene, destination);
            return button;
        }

        private static void RegisterBuildScenes()
        {
            var paths = new HashSet<string> { MainMenu, LevelSelect, Gameplay };
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainMenu, true),
                new EditorBuildSettingsScene(LevelSelect, true),
                new EditorBuildSettingsScene(Gameplay, true)
            };
            // Keep template/user scenes and their enabled flags after the foundation entry scene.
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (paths.Add(scene.path))
                    scenes.Add(scene);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
