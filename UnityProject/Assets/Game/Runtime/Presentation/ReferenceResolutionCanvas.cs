using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>One centered design frame shared by HUD, battlefield, menus and overlays.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class ReferenceResolutionCanvas : MonoBehaviour
    {
        private const string ContentName = "参考分辨率内容区";
        private CanvasScaler scaler;
        private RectTransform content;
        private int lastWidth = -1;
        private int lastHeight = -1;

        public RectTransform Content
        {
            get
            {
                if (content == null)
                {
                    content = transform.Find(ContentName) as RectTransform;
                    if (content == null)
                    {
                        content = new GameObject(ContentName, typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                        content.SetParent(transform, false);
                    }
                    content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f);
                    content.sizeDelta = new Vector2(UiLayoutContract.ReferenceWidth, UiLayoutContract.ReferenceHeight);
                    content.localScale = Vector3.one;
                }
                return content;
            }
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) Apply();
        }

        public void Apply()
        {
            if (scaler == null) scaler = GetComponent<CanvasScaler>();
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            float scale = UiResolutionLayout.ScreenScale(lastWidth, lastHeight);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = scale;
            Rect bounds = UiResolutionLayout.ScreenContentRect(lastWidth, lastHeight);
            Content.anchoredPosition = (bounds.center - new Vector2(lastWidth, lastHeight) * .5f) / scale;
        }

        public static RectTransform Attach(Canvas canvas)
        {
            if (canvas == null) return null;
            ReferenceResolutionCanvas adapter = canvas.GetComponent<ReferenceResolutionCanvas>();
            if (adapter == null) adapter = canvas.gameObject.AddComponent<ReferenceResolutionCanvas>();
            adapter.Apply();
            return adapter.Content;
        }
    }
}
