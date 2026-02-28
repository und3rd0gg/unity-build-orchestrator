#if TL_BUILD_DEV || BUILD_DEV
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BuildOrchestrator.Runtime
{
    public static class DevBuildVersionOverlayBootstrap
    {
        private const string RootName = "DEV_VERSION_OVERLAY";
        private const string LabelName = "VersionLabel";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOverlay()
        {
            if (Application.isBatchMode || Application.isEditor)
            {
                return;
            }

            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                return;
            }

            GameObject root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(root);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            GameObject labelObject = new GameObject(LabelName, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(16f, 12f);
            rect.sizeDelta = new Vector2(420f, 40f);

            TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.BottomLeft;
            text.fontSize = 22f;
            text.enableWordWrapping = false;
            text.text = $"v{Application.version}";
        }
    }
}
#endif




