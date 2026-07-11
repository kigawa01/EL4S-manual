using System.Collections.Generic;
using EL4S.Realtime;
using EL4S.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EL4S.EditorTools
{
    // Builds the title screen UI hierarchy in-place. Run via the menu item
    // while Assets/Scenes/Title.unity is the active scene, then save/commit
    // the scene as usual. Hand-editing the .unity YAML directly is too easy
    // to corrupt, especially with other people also touching this scene.
    public static class BuildTitleScreen
    {
        private const string TitleScenePath = "Assets/Scenes/Title.unity";

        [MenuItem("Tools/EL4S/Build Title Screen")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Title")
            {
                Debug.LogError("Open Assets/Scenes/Title.unity as the active scene before running this.");
                return;
            }

            EnsureEventSystem();

            var canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject canvasGo;
            if (canvas == null)
            {
                canvasGo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
            }
            else
            {
                canvasGo = canvas.gameObject;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (canvasGo.GetComponent<GraphicRaycaster>() == null) canvasGo.AddComponent<GraphicRaycaster>();

            CreateTitleText(canvasGo.transform);
            var roomCodeInput = CreateRoomCodeInput(canvasGo.transform);
            var joinButton = CreateJoinButton(canvasGo.transform);
            var statusText = CreateStatusText(canvasGo.transform);
            var connection = EnsureRealtimeConnection();
            var controller = EnsureTitleScreenController();

            WireController(controller, connection, roomCodeInput, joinButton, statusText);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AddTitleSceneToBuildSettings();

            Debug.Log("Title screen built and scene saved.");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void CreateTitleText(Transform parent)
        {
            if (FindDeep(parent, "TitleText") != null) return;

            var go = new GameObject("TitleText", typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.text = "EL4S";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 96;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.7f);
            rect.anchorMax = new Vector2(0.5f, 0.7f);
            rect.sizeDelta = new Vector2(800, 200);
            rect.anchoredPosition = Vector2.zero;
        }

        private static InputField CreateRoomCodeInput(Transform parent)
        {
            var existing = FindDeep(parent, "RoomCodeInput");
            if (existing != null) return existing.GetComponent<InputField>();

            var go = new GameObject("RoomCodeInput", typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.45f);
            rect.anchorMax = new Vector2(0.5f, 0.45f);
            rect.sizeDelta = new Vector2(400, 60);
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = Color.white;

            var placeholderGo = new GameObject("Placeholder", typeof(Text));
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholderText = placeholderGo.GetComponent<Text>();
            placeholderText.text = "合言葉を入力";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 24;
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.alignment = TextAnchor.MiddleCenter;
            placeholderText.color = new Color(0f, 0f, 0f, 0.5f);
            StretchToParent(placeholderGo.GetComponent<RectTransform>());

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            StretchToParent(textGo.GetComponent<RectTransform>());

            var inputField = go.GetComponent<InputField>();
            inputField.targetGraphic = image;
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;

            return inputField;
        }

        private static Button CreateJoinButton(Transform parent)
        {
            var existing = FindDeep(parent, "JoinButton");
            if (existing != null) return existing.GetComponent<Button>();

            var go = new GameObject("JoinButton", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.3f);
            rect.anchorMax = new Vector2(0.5f, 0.3f);
            rect.sizeDelta = new Vector2(240, 70);
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            var text = textGo.GetComponent<Text>();
            text.text = "参加";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            StretchToParent(textGo.GetComponent<RectTransform>());

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            return button;
        }

        private static Text CreateStatusText(Transform parent)
        {
            var existing = FindDeep(parent, "StatusText");
            if (existing != null) return existing.GetComponent<Text>();

            var go = new GameObject("StatusText", typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.2f);
            rect.anchorMax = new Vector2(0.5f, 0.2f);
            rect.sizeDelta = new Vector2(600, 60);
            rect.anchoredPosition = Vector2.zero;

            return text;
        }

        private static RealtimeConnection EnsureRealtimeConnection()
        {
            var existing = Object.FindFirstObjectByType<RealtimeConnection>();
            if (existing != null) return existing;

            var go = new GameObject("RealtimeConnection", typeof(RealtimeConnection));
            return go.GetComponent<RealtimeConnection>();
        }

        private static TitleScreenController EnsureTitleScreenController()
        {
            var existing = Object.FindFirstObjectByType<TitleScreenController>();
            if (existing != null) return existing;

            var go = new GameObject("TitleScreenController", typeof(TitleScreenController));
            return go.GetComponent<TitleScreenController>();
        }

        private static void WireController(
            TitleScreenController controller,
            RealtimeConnection connection,
            InputField roomCodeInput,
            Button joinButton,
            Text statusText)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("connection").objectReferenceValue = connection;
            serialized.FindProperty("roomCodeInput").objectReferenceValue = roomCodeInput;
            serialized.FindProperty("joinButton").objectReferenceValue = joinButton;
            serialized.FindProperty("statusText").objectReferenceValue = statusText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }

            return null;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void AddTitleSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (var s in scenes)
            {
                if (s.path == TitleScenePath) return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(TitleScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
