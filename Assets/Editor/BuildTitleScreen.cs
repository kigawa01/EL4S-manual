using System.Collections.Generic;
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

            var canvasGo = GameObject.Find("TitleCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            CreateTitleText(canvasGo.transform);
            CreateStartButton(canvasGo.transform);

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
            if (parent.Find("TitleText") != null) return;

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

        private static void CreateStartButton(Transform parent)
        {
            if (parent.Find("StartButton") != null) return;

            var go = new GameObject("StartButton", typeof(Image), typeof(Button), typeof(TitleScreenController));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.3f);
            rect.anchorMax = new Vector2(0.5f, 0.3f);
            rect.sizeDelta = new Vector2(320, 100);
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            var text = textGo.GetComponent<Text>();
            text.text = "Start";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var controller = go.GetComponent<TitleScreenController>();
            var button = go.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, controller.StartGame);
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
