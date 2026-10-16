using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // All images are existing formal assets. This component owns presentation only.
    public sealed class ShopCounterDetail : MonoBehaviour
    {
        public const string TrayPath = "Art/FormalUIEmptyIllustrations/empty_archive_tray";
        private RectTransform[] coins;
        private Vector2[] rest;
        private Func<float> intensity;
        private float started = -1;
        public int CommittedPlays { get; private set; }
        public bool IsAnimating => started >= 0;

        public static Image ImageAt(string name, Transform parent, Sprite sprite, Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        public static void Offer(Transform parent, Sprite item, Vector2 topLeft, bool sold, string offerId)
        {
            Sprite tray = Resources.Load<Sprite>(TrayPath);
            if (tray == null || item == null) return;
            Image support = ImageAt("商品托盘_" + offerId, parent, tray, topLeft, new Vector2(192, 128));
            // These three fixed stock icons have measured alpha bottoms of 26/30, not 32.
            // Align the actual opaque subject to the tray floor, not the transparent canvas.
            int alphaBottom = item.name.StartsWith("aca_eq_hd01", StringComparison.Ordinal) ? 26 : 30;
            Image shadow = ImageAt("商品接触影_" + offerId, support.transform, null, new Vector2(84, -63), new Vector2(24, 2));
            shadow.color = new Color(0, 0, 0, .16f);
            shadow.gameObject.SetActive(!sold);
            Image product = ImageAt("托盘商品_" + offerId, support.transform, item, new Vector2(80, -(63 - alphaBottom)), new Vector2(32, 32));
            product.gameObject.SetActive(!sold);
            // The persistent empty tray is the sold-state cue; functional Sold label stays visible.
        }

        public static ShopCounterDetail Create(Transform parent, Func<float> motion)
        {
            Sprite tray = Resources.Load<Sprite>(TrayPath);
            Sprite money = Resources.Load<Sprite>(FormalArtRegistry.ResourceMetricPath("gold"));
            Sprite table = Resources.Load<Sprite>("Art/FormalAcademyStructures32/academy_map_table_2x1");
            if (tray == null || money == null || table == null) return null;
            // The accepted 64x32 table at 3x supports the tray's opaque lower edge.
            // Both sit outside the product cards and below the resource top bar.
            ImageAt("市集零钱柜台", parent, table, new Vector2(1664, -142), new Vector2(192, 96));
            Image support = ImageAt("柜台零钱托盘", parent, tray, new Vector2(1664, -66), new Vector2(192, 128));
            var detail = support.gameObject.AddComponent<ShopCounterDetail>();
            detail.intensity = motion;
            detail.coins = new RectTransform[3];
            detail.rest = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                Vector2 position = new Vector2(64 + 20 * i, -39 + (i == 1 ? 2 : 0));
                detail.coins[i] = ImageAt("柜台硬币" + i, support.transform, money, position, new Vector2(24, 24)).rectTransform;
                detail.rest[i] = position;
            }
            return detail;
        }

        public void PlayCommitted()
        {
            CommittedPlays++;
            ResetCoins();
            if ((intensity?.Invoke() ?? 0) > .01f) started = Time.unscaledTime;
        }

        private void Update()
        {
            if (started < 0) return;
            if ((intensity?.Invoke() ?? 0) <= .01f) { ResetCoins(); return; }
            float t = (Time.unscaledTime - started) / .32f;
            if (t >= 1) { ResetCoins(); return; }
            float envelope = 1 - t;
            for (int i = 0; i < coins.Length; i++)
            {
                float offset = Mathf.Round(Mathf.Sin(t * Mathf.PI * 4 + i * .9f) * 2 * envelope);
                coins[i].anchoredPosition = rest[i] + new Vector2(offset, Mathf.Max(0, offset));
            }
        }

        private void ResetCoins()
        {
            started = -1;
            if (coins == null) return;
            for (int i = 0; i < coins.Length; i++) if (coins[i] != null) coins[i].anchoredPosition = rest[i];
        }
        private void OnDisable() => ResetCoins();
    }
}
