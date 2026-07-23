using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Runtime-only presentation layer: it adds readable feedback without changing the authored scene HUD.
    public sealed class CombatVisualFeedback : MonoBehaviour
    {
        private readonly Dictionary<string, int> healthCache = new Dictionary<string, int>();
        private readonly Dictionary<GridPosition, int> durabilityCache = new Dictionary<GridPosition, int>();
        private readonly Dictionary<string, int> statusCache = new Dictionary<string, int>();
        private CombatPrototypeBootstrap bootstrap;
        private Canvas canvas;
        private string lastOutcome;

        public void Initialize(CombatPrototypeBootstrap source)
        {
            bootstrap = source;
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(160, 32);
        }

        private void Update()
        {
            if (bootstrap == null || !bootstrap.IsDeveloperCombatActive || bootstrap.CurrentState == null) return;
            foreach (UnitState unit in bootstrap.CurrentState.Units.Values)
            {
                if (healthCache.TryGetValue(unit.Id, out int previous) && unit.Health < previous)
                    ShowFloatingText(unit.Position, "-" + (previous - unit.Health), unit.IsHero ? new Color(.8f, .94f, 1f) : new Color(.9f, .34f, .3f));
                healthCache[unit.Id] = unit.Health;
                int statusCount = unit.Statuses.Count;
                if (statusCache.TryGetValue(unit.Id, out int oldStatusCount) && statusCount != oldStatusCount && statusCount > 0)
                    ShowFloatingText(unit.Position, "状态", new Color(1f, .8f, .25f));
                statusCache[unit.Id] = statusCount;
            }
            for (int y = 0; y < bootstrap.CurrentState.Map.Height; y++) for (int x = 0; x < bootstrap.CurrentState.Map.Width; x++)
            {
                GridPosition position = new GridPosition(x, y); TileState tile = bootstrap.CurrentState.Map.GetTile(position);
                if (!durabilityCache.TryGetValue(position, out int oldDurability)) durabilityCache[position] = tile.Durability;
                else if (tile.Durability < oldDurability) { NotifyDestructible(position, tile.IsDestroyed); durabilityCache[position] = tile.Durability; }
            }
        }

        public void PlayOutcome(bool victory)
        {
            string outcome = victory ? "victory" : "defeat";
            if (lastOutcome == outcome) return;
            lastOutcome = outcome;
            EnsureCanvas();
            GameObject card = new GameObject("战斗结果反馈"); card.transform.SetParent(canvas.transform, false);
            RectTransform rect = card.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(520, 100);
            Text label = card.AddComponent<Text>(); label.font = Resources.Load<Font>("Fonts/SimHei"); label.fontSize = 36; label.alignment = TextAnchor.MiddleCenter; label.text = victory ? "战斗胜利" : "战斗失败"; label.color = victory ? new Color(.48f, .92f, 1f, 0f) : new Color(.94f, .36f, .32f, 0f);
            CanvasGroup group = card.AddComponent<CanvasGroup>(); group.alpha = 0f; rect.localScale = Vector3.one * .84f;
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, .16f)).Join(rect.DOScale(1f, .2f).SetEase(Ease.OutBack));
            sequence.AppendInterval(.7f).Append(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .22f)).OnComplete(() => Destroy(card));
        }

        public void ResetBattleFeedback()
        {
            lastOutcome = null;
            healthCache.Clear();
            durabilityCache.Clear(); statusCache.Clear();
        }

        public void NotifyDestructible(GridPosition position, bool destroyed)
        {
            EnsureCanvas();
            PulseCell(position, destroyed ? new Color(1f, .35f, .08f, .85f) : new Color(1f, .75f, .2f, .65f), destroyed ? .24f : .14f);
            ShowFloatingText(position, destroyed ? "摧毁" : "受损", destroyed ? new Color(1f, .42f, .18f) : new Color(1f, .82f, .25f));
        }

        public void NotifyAttack(GridPosition source, GridPosition target, int damage, bool defeated)
        {
            EnsureCanvas();
            PulseCell(source, new Color(.35f, .85f, 1f, .72f), .12f);
            PulseCell(target, defeated ? new Color(1f, .18f, .12f, .85f) : new Color(1f, .35f, .28f, .72f), .18f);
            if (damage > 0) ShowFloatingText(target, "-" + damage, new Color(1f, .45f, .35f));
            if (defeated) ShowBreakText(target);
        }

        private void PulseCell(GridPosition position, Color color, float duration)
        {
            GameObject pulse = new GameObject("战斗反馈脉冲"); pulse.transform.SetParent(canvas.transform, false);
            RectTransform rect = pulse.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(-690 + position.X * 78, -80 + position.Y * 78); rect.sizeDelta = new Vector2(64, 64);
            Image image = pulse.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            CanvasGroup group = pulse.AddComponent<CanvasGroup>(); group.alpha = .85f; rect.localScale = Vector3.one * .7f;
            DOTween.Sequence().SetUpdate(true).Join(rect.DOScale(1.18f, duration).SetEase(Ease.OutQuad)).Join(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, duration)).OnComplete(() => Destroy(pulse));
        }

        private void ShowBreakText(GridPosition position)
        {
            GameObject textObject = new GameObject("目标击破反馈"); textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = new Vector2(-690 + position.X * 78, -80 + position.Y * 78 + 24); rect.sizeDelta = new Vector2(180, 30);
            Text text = textObject.AddComponent<Text>(); text.font = Resources.Load<Font>("Fonts/SimHei"); text.fontSize = 18; text.alignment = TextAnchor.MiddleCenter; text.text = "目标击破"; text.color = new Color(1f, .8f, .3f);
            CanvasGroup group = textObject.AddComponent<CanvasGroup>();
            DOTween.Sequence().SetUpdate(true).Join(rect.DOScale(1.12f, .16f)).Join(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .5f)).OnComplete(() => Destroy(textObject));
        }

        private void ShowFloatingText(GridPosition position, string message, Color color)
        {
            EnsureCanvas();
            GameObject textObject = new GameObject("伤害反馈"); textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            // The board occupies the left 75% of the 1920 reference canvas.
            rect.anchoredPosition = new Vector2(-690 + position.X * 78, -80 + position.Y * 78); rect.sizeDelta = new Vector2(96, 36);
            Text text = textObject.AddComponent<Text>(); text.font = Resources.Load<Font>("Fonts/SimHei"); text.fontSize = 24; text.alignment = TextAnchor.MiddleCenter; text.text = message; text.color = color;
            CanvasGroup group = textObject.AddComponent<CanvasGroup>();
            float targetY = rect.anchoredPosition.y + 44f;
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(DOTween.To(() => rect.anchoredPosition.y, value => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, value), targetY, .42f).SetEase(Ease.OutCubic)).Join(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .42f));
            sequence.OnComplete(() => Destroy(textObject));
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            GameObject root = new GameObject("运行时战斗反馈"); DontDestroyOnLoad(root);
            canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        }
    }
}
