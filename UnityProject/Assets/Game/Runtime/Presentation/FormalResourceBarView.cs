using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// Shared formal resource-bar prefab contract. HUD and battlefield variants use the
    /// same hierarchy and only switch layout density, label visibility and fill skin.
    /// </summary>
    public sealed class FormalResourceBarView : MonoBehaviour
    {
        public const string HudResourcePath = "UI/Prefabs/FormalHudResourceBar";
        public const string UnitResourcePath = "UI/Prefabs/FormalUnitHealthBar";

        public RectTransform Root;
        public Image Track;
        public Image Fill;
        public Image Forecast;
        public Image Marker;
        public Text Value;
        public Image[] Ticks;

        public static FormalResourceBarView InstantiateHud(Transform parent) => Instantiate(HudResourcePath, parent);

        public static FormalResourceBarView InstantiateUnit(Transform parent) => Instantiate(UnitResourcePath, parent);

        private static FormalResourceBarView Instantiate(string resourcePath, Transform parent)
        {
            FormalResourceBarView template = Resources.Load<FormalResourceBarView>(resourcePath);
            if (template == null)
                throw new MissingReferenceException("Formal resource-bar prefab is missing at Resources/" + resourcePath + ".");
            FormalResourceBarView instance = Object.Instantiate(template, parent, false);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void ConfigureHud(string title, Color fillColor)
        {
            gameObject.name = title + "轨道";
            FormalUiKit.ApplySkin(Track, "bar_track", FormalUiTheme.ResourceTrack);
            Image skinOverlay = FormalUiKit.SkinOverlay(Track);
            if (skinOverlay != null) skinOverlay.transform.SetAsFirstSibling();
            ConfigureFill(null, 3f, fillColor);
            SetTicksVisible(true);
            Value.gameObject.SetActive(false);
            Forecast.gameObject.SetActive(false);
            Marker.rectTransform.sizeDelta = new Vector2(8f, -6f);
            Fill.gameObject.name = title + "填充";
            Forecast.gameObject.name = title + "预估损失";
            Marker.gameObject.name = title + "变化落点";
            for (int i = 0; i < Ticks.Length; i++) Ticks[i].gameObject.name = title + "比例刻度_" + (i + 1);
        }

        public void ConfigureCompact(string name, string fillSkinId, Color? fillColor = null)
        {
            gameObject.name = name;
            Value.gameObject.SetActive(true);
            Track.sprite = null;
            Track.type = Image.Type.Simple;
            Track.color = FormalUiTheme.Ink;
            ConfigureFill(fillSkinId, 1f, fillColor ?? Color.white);
            FormalUiKit.Stretch(Value.rectTransform);
            SetTicksVisible(false);
            Forecast.gameObject.SetActive(false);
            Marker.rectTransform.sizeDelta = new Vector2(6f, -4f);
            Fill.gameObject.name = "当前";
            Forecast.gameObject.name = "预估损失";
            Marker.gameObject.name = "变化落点";
        }

        private void ConfigureFill(string fillSkinId, float inset, Color fillColor)
        {
            FormalUiKit.Stretch(Fill.rectTransform);
            Fill.rectTransform.offsetMin = new Vector2(inset, inset);
            Fill.rectTransform.offsetMax = new Vector2(-inset, -inset);
            if (string.IsNullOrEmpty(fillSkinId)) Fill.sprite = FormalUiKit.SolidFillSprite;
            else FormalUiKit.ApplyBarFillSkin(Fill, fillSkinId);
            Fill.type = Image.Type.Filled;
            Fill.fillMethod = Image.FillMethod.Horizontal;
            Fill.fillOrigin = 0;
            Fill.fillClockwise = true;
            Fill.fillAmount = 1f;
            Fill.color = fillColor;
            FormalUiKit.Stretch(Forecast.rectTransform);
            Forecast.rectTransform.offsetMin = new Vector2(inset, inset);
            Forecast.rectTransform.offsetMax = new Vector2(-inset, -inset);
            Forecast.color = FormalUiTheme.WithAlpha(FormalUiTheme.Danger, .82f);
            Marker.rectTransform.anchorMin = new Vector2(1f, 0f);
            Marker.rectTransform.anchorMax = new Vector2(1f, 1f);
            Marker.rectTransform.pivot = new Vector2(.5f, .5f);
        }

        private void SetTicksVisible(bool visible)
        {
            if (Ticks == null) return;
            for (int i = 0; i < Ticks.Length; i++)
                if (Ticks[i] != null) Ticks[i].gameObject.SetActive(visible);
        }

    }
}
