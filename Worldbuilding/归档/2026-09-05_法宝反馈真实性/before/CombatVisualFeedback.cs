using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public static class CombatFeedbackPresentationPolicy
    {
        public static bool AnimationsEnabled(float intensity) => intensity > .01f;
    }

    public static class UnitEndpointAnimationPolicy
    {
        public const int EndpointFrameCount = 2;

        public static int FrameIndex(float normalizedProgress) =>
            Mathf.Clamp01(normalizedProgress) < .5f ? 0 : 1;

        public static Vector2 LocalJitter(float normalizedProgress, Vector2 direction, int maximumPixels)
        {
            float progress = Mathf.Clamp01(normalizedProgress);
            int amplitude = Mathf.Clamp(maximumPixels, 0, 2);
            if (amplitude == 0 || progress < .30f || progress > .78f) return Vector2.zero;
            Vector2 axis = direction.sqrMagnitude > .001f
                ? new Vector2(-direction.y, direction.x).normalized
                : Vector2.right;
            int phase = Mathf.FloorToInt((progress - .30f) / .12f);
            int sign = phase % 2 == 0 ? 1 : -1;
            return new Vector2(Mathf.Round(axis.x * amplitude * sign), Mathf.Round(axis.y * amplitude * sign));
        }
    }

    public static class CombatStatusFeedback
    {
        public static bool HasRemoval(IReadOnlyDictionary<StatusType, int> previous, IReadOnlyDictionary<StatusType, int> current) =>
            previous != null && previous.Keys.Any(status => current == null || !current.ContainsKey(status));
    }

    public readonly struct CombatDamagePopupPresentation
    {
        public int Amount { get; }
        public bool IncludesHealthDamage { get; }
        public string Text => "-" + Amount;

        public CombatDamagePopupPresentation(int amount, bool includesHealthDamage)
        {
            Amount = System.Math.Max(0, amount);
            IncludesHealthDamage = includesHealthDamage;
        }

        public CombatDamagePopupPresentation Merge(CombatDamagePopupPresentation next) =>
            new CombatDamagePopupPresentation(Amount + next.Amount, IncludesHealthDamage || next.IncludesHealthDamage);

        public static CombatDamagePopupPresentation From(CombatFeedbackEvent feedback)
        {
            bool healthDamage = feedback.Kind == CombatFeedbackKind.Damage;
            if (!healthDamage && feedback.Kind != CombatFeedbackKind.ShieldAbsorb)
                throw new System.ArgumentException("Only resolved health or shield damage can create a damage popup.", nameof(feedback));
            return new CombatDamagePopupPresentation(feedback.Amount, healthDamage);
        }
    }

    // Runtime-only presentation layer: it adds readable feedback without changing the authored scene HUD.
    public sealed class CombatVisualFeedback : MonoBehaviour, IResolvedCombatFeedbackSink
    {
        private readonly Dictionary<string, int> healthCache = new Dictionary<string, int>();
        private readonly Dictionary<string, int> shieldCache = new Dictionary<string, int>();
        private readonly Dictionary<string, int> manaCache = new Dictionary<string, int>();
        private readonly Dictionary<string, GridPosition> positionCache = new Dictionary<string, GridPosition>();
        private readonly Dictionary<GridPosition, int> durabilityCache = new Dictionary<GridPosition, int>();
        private readonly Dictionary<string, Dictionary<StatusType, int>> statusCache = new Dictionary<string, Dictionary<StatusType, int>>();
        private readonly Dictionary<string, float> hitUntil = new Dictionary<string, float>();
        private readonly Dictionary<string, UnitMotion> unitMotions = new Dictionary<string, UnitMotion>();
        private readonly CombatMovementPlayback movementPlayback = new CombatMovementPlayback();
        private readonly CombatActionPlayback actionPlayback = new CombatActionPlayback();
        private CombatActionCapture actionCapture;
        public bool IsActionPlaying { get { AdvanceActionPlayback(); return actionPlayback.IsPlaying(Time.unscaledTime); } }
        public int ActionPresentationVersion => actionPlayback.Version;
        private float FeedbackTime => actionPlayback.DispatchTime ?? Time.unscaledTime;

        public CombatActionCapture BeginResolvedAction(string actorId, FireBattleState fire = null, bool attack = false, GridPosition? target = null)
        {
            if (bootstrap?.CurrentState == null) return null;
            AdvanceActionPlayback();
            if (actionCapture != null || actionPlayback.IsPlaying(Time.unscaledTime))
                throw new System.InvalidOperationException("An action presentation is already in progress.");
            var capture = actionPlayback.Capture(bootstrap.CurrentState, fire, actorId,
                AnimationsEnabled ? movementPlayback.Remaining(actorId, Time.unscaledTime) : 0f);
            actionCapture = capture;
            capture.TriggerDelay = attack ? .16f : 0f;
            if (attack && target.HasValue)
            {
                UnitState actor = bootstrap.CurrentState.GetUnit(actorId);
                capture.AddDelivery(() => PlayUnitMotion(actor, UnitMotionKind.Attack, .30f,
                    GridDirection(capture.Source, target.Value), Vector2.zero));
            }
            capture.AbortAction = () => { if (actionCapture == capture) actionCapture = null; };
            capture.CompleteAction = () =>
            {
                actionCapture = null;
                actionPlayback.Start(capture, bootstrap.CurrentState, Time.unscaledTime, Publish);
                CaptureResolvedFeedbackState();
                AdvanceActionPlayback();
            };
            return capture;
        }

        private void AdvanceActionPlayback() => actionPlayback.Advance(Time.unscaledTime, AnimationsEnabled);
        public UnitState PresentedUnit(UnitState unit) => actionPlayback.Unit(unit, Time.unscaledTime);
        public TileState PresentedTile(GridPosition position, TileState tile) => actionPlayback.Tile(position, tile, Time.unscaledTime);
        public bool PresentedFireground(GridPosition position, bool exists) => actionPlayback.Fireground(position, exists, Time.unscaledTime);

        private void CaptureResolvedFeedbackState()
        {
            foreach (UnitState unit in bootstrap.CurrentState.Units.Values)
            {
                healthCache[unit.Id] = unit.Health; shieldCache[unit.Id] = unit.Shield;
                manaCache[unit.Id] = unit.Mana; positionCache[unit.Id] = unit.Position;
                statusCache[unit.Id] = unit.Statuses.ToDictionary(entry => entry.Key, entry => entry.Value);
            }
            for (int y = 0; y < bootstrap.CurrentState.Map.Height; y++) for (int x = 0; x < bootstrap.CurrentState.Map.Width; x++)
            { var p = new GridPosition(x, y); durabilityCache[p] = bootstrap.CurrentState.Map.GetTile(p).Durability; }
        }
        private readonly Dictionary<string, Sprite> semanticIcons = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite[]> vfxFrames = new Dictionary<string, Sprite[]>();
        private readonly CombatVfxPlayback vfxPlayback = new CombatVfxPlayback();
        private readonly Dictionary<CombatVfxSlot, Image> activeVfx = new Dictionary<CombatVfxSlot, Image>();
        private readonly HashSet<CombatVfxSlot> visibleVfx = new HashSet<CombatVfxSlot>();
        private readonly List<CombatVfxSlot> expiredVfx = new List<CombatVfxSlot>();
        private RectTransform vfxRoot;
        private readonly Dictionary<GridPosition, DamagePopupState> activeDamagePopups = new Dictionary<GridPosition, DamagePopupState>();
        private ICombatFeedbackHost bootstrap;
        private Canvas canvas;
        private RectTransform battlefieldClip;
        private string lastOutcome;
        private string activeUnitId;
        private string focusedEnemyId;
        private GameObject enemyActionBanner;
        private int damagePopupSerial;
        private static readonly Vector2 FloatingTextSize = new Vector2(240f, 48f);
        private static readonly Vector2 DamageTextSize = new Vector2(168f, 104f);
        private const float FloatingTextRise = 28f;
        private const float DamageTextRise = 52f;

        private sealed class DamagePopupState
        {
            public GameObject Root;
            public RectTransform Rect;
            public Text Label;
            public CanvasGroup Group;
            public CombatDamagePopupPresentation Presentation;
            public float LastUpdate;
        }

        private enum UnitMotionKind { Move, Attack, Cast, Hit, ShieldHit, Recover, Ready }

        private readonly struct UnitMotion
        {
            public UnitMotionKind Kind { get; }
            public float StartedAt { get; }
            public float Duration { get; }
            public Vector2 Direction { get; }
            public Vector2 OriginOffset { get; }

            public UnitMotion(UnitMotionKind kind, float duration, Vector2 direction, Vector2 originOffset, float startedAt)
            {
                Kind = kind;
                StartedAt = startedAt;
                Duration = duration;
                Direction = direction;
                OriginOffset = originOffset;
            }
        }

        public void Initialize(ICombatFeedbackHost source)
        {
            bootstrap = source;
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(160, 32);
        }

        private void Update()
        {
            if (bootstrap == null || !bootstrap.IsDeveloperCombatActive || bootstrap.CurrentState == null)
            { ClearVfx(); movementPlayback.Clear(); actionPlayback.Clear(); actionCapture?.Dispose(); return; }
            AdvanceActionPlayback();
            if (!AnimationsEnabled) movementPlayback.Clear();
            RefreshVfx(Time.unscaledTime);
            foreach (UnitState unit in bootstrap.CurrentState.Units.Values)
            {
                if (!PresentedUnit(unit).IsAlive) movementPlayback.Cancel(unit.Id);
                if (shieldCache.TryGetValue(unit.Id, out int previousShield))
                {
                    if (unit.Shield < previousShield) Publish(new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb, unit.Position, previousShield - unit.Shield));
                    else if (unit.Shield > previousShield) Publish(new CombatFeedbackEvent(CombatFeedbackKind.ShieldRestore, unit.Position, unit.Shield - previousShield));
                }
                shieldCache[unit.Id] = unit.Shield;
                if (manaCache.TryGetValue(unit.Id, out int previousMana) && unit.Mana > previousMana)
                    Publish(new CombatFeedbackEvent(CombatFeedbackKind.ManaRestore, unit.Position, unit.Mana - previousMana));
                manaCache[unit.Id] = unit.Mana;
                if (healthCache.TryGetValue(unit.Id, out int previousHealth))
                {
                    if (unit.Health < previousHealth) Publish(new CombatFeedbackEvent(CombatFeedbackKind.Damage, unit.Position, previousHealth - unit.Health));
                    else if (unit.Health > previousHealth) Publish(new CombatFeedbackEvent(CombatFeedbackKind.Healing, unit.Position, unit.Health - previousHealth));
                    if (previousHealth > 0 && !unit.IsAlive) Publish(new CombatFeedbackEvent(CombatFeedbackKind.UnitDefeated, unit.Position));
                }
                healthCache[unit.Id] = unit.Health;
                if (positionCache.TryGetValue(unit.Id, out GridPosition previousPosition) && previousPosition != unit.Position)
                    NotifyMovement(unit.Id, previousPosition, unit.Position, null);
                positionCache[unit.Id] = unit.Position;
                statusCache.TryGetValue(unit.Id, out Dictionary<StatusType, int> previousStatuses);
                foreach (KeyValuePair<StatusType, int> status in unit.Statuses)
                    if (previousStatuses == null || !previousStatuses.TryGetValue(status.Key, out int previousDuration) || status.Value > previousDuration)
                        NotifyStatusApplied(unit.Position, status.Key, status.Value);
                if (CombatStatusFeedback.HasRemoval(previousStatuses, unit.Statuses))
                    Publish(new CombatFeedbackEvent(CombatFeedbackKind.StatusCleared, unit.Position));
                statusCache[unit.Id] = unit.Statuses.ToDictionary(entry => entry.Key, entry => entry.Value);
            }
            if (!actionPlayback.IsPlaying(Time.unscaledTime) && activeUnitId != bootstrap.CurrentState.ActiveUnitId)
            {
                activeUnitId = bootstrap.CurrentState.ActiveUnitId;
                UnitState active = bootstrap.CurrentState.GetUnit(activeUnitId);
                if (active != null && active.IsAlive)
                {
                    PlayUnitMotion(active, UnitMotionKind.Ready, .34f, Vector2.zero, Vector2.zero);
                    if ((bootstrap?.UiPreferences.AnimationIntensity ?? 1f) > .01f)
                        PulseCell(active.Position, active.IsHero ? new Color(.30f, .78f, .88f) : new Color(.94f, .45f, .32f), .28f);
                }
            }
            for (int y = 0; y < bootstrap.CurrentState.Map.Height; y++) for (int x = 0; x < bootstrap.CurrentState.Map.Width; x++)
            {
                GridPosition position = new GridPosition(x, y); TileState tile = bootstrap.CurrentState.Map.GetTile(position);
                if (!durabilityCache.TryGetValue(position, out int oldDurability)) durabilityCache[position] = tile.Durability;
                else if (tile.Durability < oldDurability) { NotifyDestructible(position, tile); durabilityCache[position] = tile.Durability; }
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
            Text label = card.AddComponent<Text>(); label.font = FormalUiKit.Font; label.fontSize = FormalUiTheme.TitleFontSize; label.fontStyle = FontStyle.Normal; label.alignment = TextAnchor.MiddleCenter; label.text = victory ? "战斗胜利" : "战斗失败"; label.color = victory ? FormalUiTheme.Cyan : FormalUiTheme.Danger; label.resizeTextForBestFit = false; label.raycastTarget = false;
            CanvasGroup group = card.AddComponent<CanvasGroup>(); group.alpha = 0f; rect.localScale = Vector3.one;
            if (!AnimationsEnabled)
            {
                group.alpha = 1f;
                rect.localScale = Vector3.one;
                DOVirtual.DelayedCall(.9f, () => { if (card != null) DestroyFeedbackObject(card); }).SetUpdate(true).SetTarget(card);
                return;
            }
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(card);
            sequence.Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, .16f));
            sequence.AppendInterval(.7f).Append(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .22f)).OnComplete(() => DestroyFeedbackObject(card));
        }

        public void ResetBattleFeedback()
        {
            lastOutcome = null;
            healthCache.Clear(); shieldCache.Clear(); manaCache.Clear(); positionCache.Clear();
            durabilityCache.Clear(); statusCache.Clear(); hitUntil.Clear(); unitMotions.Clear(); activeUnitId = null;
            ClearVfx(); movementPlayback.Clear(); CancelEnemyAction();
            actionCapture?.Dispose(); actionCapture = null; actionPlayback.Clear();
            if (canvas != null)
            {
                foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                { DOTween.Kill(child); DOTween.Kill(child.gameObject); }
                foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
                    if (child != battlefieldClip) DestroyFeedbackObject(child.gameObject);
                if (battlefieldClip != null)
                    foreach (Transform child in battlefieldClip.Cast<Transform>().ToArray()) DestroyFeedbackObject(child.gameObject);
            }
            vfxRoot = null; activeDamagePopups.Clear(); damagePopupSerial = 0;
        }

        private void OnDisable() => ResetBattleFeedback();

        private void OnDestroy()
        {
            ResetBattleFeedback();
            if (canvas != null && canvas.gameObject != gameObject) DestroyFeedbackObject(canvas.gameObject);
        }

        public void BeginEnemyAction(UnitState enemy, EnemyIntentPresentation intent, float visibleSeconds)
        {
            if (enemy == null) return;
            CancelEnemyAction();
            focusedEnemyId = enemy.Id;
            PlayUnitMotion(enemy, UnitMotionKind.Ready, .58f, Vector2.zero, Vector2.zero);
            if ((bootstrap?.UiPreferences.AnimationIntensity ?? 1f) > .01f)
                PulseCell(enemy.Position, new Color(.98f, .42f, .28f), .62f);

            EnsureCanvas();
            enemyActionBanner = FormalUiKit.AnchoredPanel("敌方行动提示", canvas.transform,
                new Vector2(.375f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -42f),
                new Vector2(560f, 78f), new Color(.12f, .025f, .022f, .98f));
            Image panel = enemyActionBanner.GetComponent<Image>();
            if (panel != null) panel.raycastTarget = false;
            FormalUiKit.Label("敌方行动标题", "敌方行动 · " + enemy.DisplayName, enemyActionBanner.transform,
                new Vector2(20f, -10f), new Vector2(520f, 28f), 22, FormalUiTheme.Danger, TextAnchor.MiddleLeft);
            FormalUiKit.Label("敌方行动内容", intent?.CompactText ?? "正在行动", enemyActionBanner.transform,
                new Vector2(20f, -40f), new Vector2(520f, 24f), 17, FormalUiTheme.Text, TextAnchor.MiddleLeft);

            RectTransform rect = enemyActionBanner.GetComponent<RectTransform>();
            CanvasGroup group = enemyActionBanner.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            rect.localScale = new Vector3(.94f, .94f, 1f);
            if (!AnimationsEnabled)
            {
                group.alpha = 1f;
                rect.localScale = Vector3.one;
                DOVirtual.DelayedCall(Mathf.Max(.4f, visibleSeconds), () =>
                {
                    if (enemyActionBanner != null) DestroyFeedbackObject(enemyActionBanner);
                    enemyActionBanner = null;
                }).SetUpdate(true).SetTarget(enemyActionBanner);
                return;
            }
            float stay = Mathf.Max(.4f, visibleSeconds - .32f);
            DOTween.Sequence().SetUpdate(true).SetTarget(enemyActionBanner)
                .Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, .16f))
                .Join(rect.DOScale(1f, .20f).SetEase(Ease.OutBack))
                .AppendInterval(stay)
                .Append(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .16f))
                .OnComplete(() =>
                {
                    if (enemyActionBanner != null) DestroyFeedbackObject(enemyActionBanner);
                    enemyActionBanner = null;
                });
        }

        public void CompleteEnemyAction(string enemyId)
        {
            if (string.Equals(focusedEnemyId, enemyId, System.StringComparison.Ordinal)) focusedEnemyId = null;
            if (enemyActionBanner == null) return;
            GameObject banner = enemyActionBanner;
            enemyActionBanner = null;
            DOTween.Kill(banner);
            CanvasGroup group = banner.GetComponent<CanvasGroup>();
            if (group == null) { DestroyFeedbackObject(banner); return; }
            DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .14f).SetUpdate(true).SetTarget(banner)
                .OnComplete(() => DestroyFeedbackObject(banner));
        }

        public void CancelEnemyAction()
        {
            focusedEnemyId = null;
            if (enemyActionBanner == null) return;
            DOTween.Kill(enemyActionBanner);
            DestroyFeedbackObject(enemyActionBanner);
            enemyActionBanner = null;
        }

        public int UnitShakeOffset(UnitState unit)
        {
            if (bootstrap?.UiPreferences.ScreenShake == false) return 0;
            if (unit == null || !hitUntil.TryGetValue(unit.Id, out float until) || until <= Time.unscaledTime) return 0;
            return Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime * 42f) * 2f * (bootstrap?.UiPreferences.AnimationIntensity ?? 1f));
        }

        public Vector2 UnitPresentationOffset(UnitState unit)
        {
            if (unit == null || !unitMotions.TryGetValue(unit.Id, out UnitMotion motion)) return Vector2.zero;
            float intensity = bootstrap?.UiPreferences.AnimationIntensity ?? 1f;
            if (intensity <= .01f) { unitMotions.Remove(unit.Id); return Vector2.zero; }
            float progress = Mathf.Clamp01((Time.unscaledTime - motion.StartedAt) / Mathf.Max(.01f, motion.Duration));
            if (progress >= 1f) { unitMotions.Remove(unit.Id); return Vector2.zero; }
            Vector2 offset;
            switch (motion.Kind)
            {
                case UnitMotionKind.Move:
                    float eased = 1f - Mathf.Pow(1f - progress, 3f);
                    offset = Vector2.Lerp(motion.OriginOffset, Vector2.zero, eased);
                    offset.y -= Mathf.Sin(progress * Mathf.PI) * 2f;
                    break;
                case UnitMotionKind.Attack:
                    float thrust = progress < .24f ? -progress / .24f * 2f : progress < .52f ? Mathf.Lerp(-2f, 9f, (progress - .24f) / .28f) : Mathf.Lerp(9f, 0f, (progress - .52f) / .48f);
                    offset = motion.Direction * thrust;
                    break;
                case UnitMotionKind.Cast:
                    offset = new Vector2(Mathf.Sin(progress * Mathf.PI * 4f) * 2f, -Mathf.Sin(progress * Mathf.PI) * 7f);
                    break;
                case UnitMotionKind.Hit:
                case UnitMotionKind.ShieldHit:
                    offset = motion.Direction * (1f - progress) * 6f + new Vector2(Mathf.Sin(progress * Mathf.PI * 8f) * 3f, 0f);
                    break;
                case UnitMotionKind.Recover:
                case UnitMotionKind.Ready:
                    offset = new Vector2(0f, -Mathf.Sin(progress * Mathf.PI) * (motion.Kind == UnitMotionKind.Ready ? 5f : 3f));
                    break;
                default: offset = Vector2.zero; break;
            }
            if (!unit.IsHero && (motion.Kind == UnitMotionKind.Attack || motion.Kind == UnitMotionKind.Cast ||
                motion.Kind == UnitMotionKind.Hit || motion.Kind == UnitMotionKind.ShieldHit))
            {
                int jitterPixels = motion.Kind == UnitMotionKind.Hit || motion.Kind == UnitMotionKind.ShieldHit ? 2 : 1;
                offset += UnitEndpointAnimationPolicy.LocalJitter(progress, motion.Direction, jitterPixels);
            }
            return new Vector2(Mathf.Round(offset.x * intensity), Mathf.Round(offset.y * intensity));
        }

        public Color UnitPresentationTint(UnitState unit)
        {
            if (unit == null) return Color.white;
            bool focused = string.Equals(focusedEnemyId, unit.Id, System.StringComparison.Ordinal);
            if (!unitMotions.TryGetValue(unit.Id, out UnitMotion motion))
                return focused ? Color.Lerp(Color.white, new Color(1f, .32f, .20f), AnimationsEnabled ? .18f + Mathf.PingPong(Time.unscaledTime * .9f, .14f) : .22f) : Color.white;
            float progress = Mathf.Clamp01((Time.unscaledTime - motion.StartedAt) / Mathf.Max(.01f, motion.Duration));
            float flash = Mathf.Sin(progress * Mathf.PI) * (bootstrap?.UiPreferences.AnimationIntensity ?? 1f);
            Color tint = Color.white;
            if (motion.Kind == UnitMotionKind.Hit) tint = Color.Lerp(Color.white, new Color(1f, .36f, .28f), flash * .72f);
            else if (motion.Kind == UnitMotionKind.ShieldHit) tint = Color.Lerp(Color.white, new Color(.35f, .92f, 1f), flash * .68f);
            else if (motion.Kind == UnitMotionKind.Recover) tint = Color.Lerp(Color.white, new Color(.48f, 1f, .66f), flash * .52f);
            else if (motion.Kind == UnitMotionKind.Cast) tint = Color.Lerp(Color.white, new Color(.55f, .86f, 1f), flash * .38f);
            else if (motion.Kind == UnitMotionKind.Ready) tint = Color.Lerp(Color.white, new Color(1f, .52f, .28f), flash * .46f);
            return focused ? Color.Lerp(tint, new Color(1f, .34f, .22f), .16f) : tint;
        }

        public void NotifyDestructible(GridPosition position, TileState tile)
        {
            bool destroyed = tile.IsDestroyed;
            durabilityCache[position] = tile.Durability;
            Publish(new CombatFeedbackEvent(destroyed ? CombatFeedbackKind.DestructibleDestroyed : CombatFeedbackKind.DestructibleDamaged, position));
        }

        public void NotifyStatusApplied(GridPosition position, StatusType status, int duration)
        {
            Publish(new CombatFeedbackEvent(CombatFeedbackCatalog.ForStatus(status), position, duration: duration));
        }

        public void NotifyAttack(GridPosition source, GridPosition target, int damage, bool defeated)
        {
            if (damage > 0) Publish(new CombatFeedbackEvent(CombatFeedbackKind.Damage, source, target, damage));
            if (defeated) Publish(new CombatFeedbackEvent(CombatFeedbackKind.UnitDefeated, source, target));
        }

        public void NotifyRecovery(GridPosition position, int health, int shield)
        {
            if (health > 0) Publish(new CombatFeedbackEvent(CombatFeedbackKind.Healing, position, health));
            if (shield > 0) Publish(new CombatFeedbackEvent(CombatFeedbackKind.ShieldRestore, position, shield));
        }

        public CombatMovementPose UnitTravelPose(UnitState unit)
        {
            if (unit == null) return default;
            if (!AnimationsEnabled || !unit.IsAlive) movementPlayback.Cancel(unit.Id);
            return movementPlayback.Sample(unit.Id, unit.Position, unit.Facing, Time.unscaledTime);
        }

        public void NotifyMovement(string unitId, GridPosition source, GridPosition target, IReadOnlyList<GridPosition> path)
        {
            UnitState unit = bootstrap?.CurrentState?.GetUnit(unitId);
            if (unit == null) return;
            // Entry reactions can displace a unit after the Move result. Do not animate a stale endpoint.
            if (AnimationsEnabled && unit.IsAlive && unit.Position == target)
                movementPlayback.Play(unit.Id, source, target, unit.Facing, path, Time.unscaledTime);
            else movementPlayback.Cancel(unit.Id);
            positionCache[unit.Id] = unit.Position;
        }

        public void NotifyArtifact(ArtifactDefinition artifact, GridPosition source, IReadOnlyList<GridPosition> targetCells, ArtifactExecution execution)
        {
            if (artifact == null || execution == null) return;
            UnitState sourceUnit = bootstrap.CurrentState?.Units.Values.FirstOrDefault(unit => unit.Id == "hero");
            if (sourceUnit != null) PlayUnitMotion(sourceUnit, UnitMotionKind.Cast, .34f, GridDirection(source, targetCells.FirstOrDefault()), Vector2.zero);
            foreach (ArtifactStep step in execution.Steps)
            {
                CombatFeedbackKind? kind = null;
                switch (step.Kind)
                {
                    case ArtifactEffectKind.Damage:
                    case ArtifactEffectKind.LoseHealth:
                    case ArtifactEffectKind.BacklashIfTargetSurvives: kind = CombatFeedbackKind.Damage; break;
                    case ArtifactEffectKind.RestoreHealth: kind = CombatFeedbackKind.Healing; break;
                    case ArtifactEffectKind.RestoreShield:
                    case ArtifactEffectKind.TransferShield: kind = CombatFeedbackKind.ShieldRestore; break;
                    case ArtifactEffectKind.RestoreMana: kind = CombatFeedbackKind.ManaRestore; break;
                    case ArtifactEffectKind.ApplyStatus:
                        ArtifactEffectDefinition statusEffect = artifact.Effects.FirstOrDefault(effect => effect.Kind == ArtifactEffectKind.ApplyStatus);
                        kind = CombatFeedbackCatalog.ForStatus(statusEffect.Status); break;
                    case ArtifactEffectKind.ClearNegativeStatuses:
                    case ArtifactEffectKind.ClearFireground: kind = CombatFeedbackKind.StatusCleared; break;
                    case ArtifactEffectKind.MoveSource:
                    case ArtifactEffectKind.ForceMoveTarget: kind = CombatFeedbackKind.Movement; break;
                    case ArtifactEffectKind.DamageObject: kind = CombatFeedbackKind.DestructibleDamaged; break;
                    case ArtifactEffectKind.DestroyLightCover: kind = CombatFeedbackKind.DestructibleDestroyed; break;
                    case ArtifactEffectKind.CreateLightCover:
                    case ArtifactEffectKind.CreateFireground:
                    case ArtifactEffectKind.DeployDecoy:
                    case ArtifactEffectKind.ArmReaction:
                    case ArtifactEffectKind.ArmAnchor: kind = CombatFeedbackKind.StatusCleared; break;
                    case ArtifactEffectKind.DelayInitiative: kind = CombatFeedbackKind.Slow; break;
                }
                if (kind.HasValue) Publish(new CombatFeedbackEvent(kind.Value, source, step.Cell, step.Applied));
            }
        }

        // Stable presentation entry point for later skills/effects. It consumes read-only result data only.
        public void Publish(CombatFeedbackEvent feedback)
        {
            if (actionCapture != null) { actionCapture.AddFeedback(feedback); return; }
            EnsureCanvas();
            CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(feedback.Kind);
            Color color = SemanticColor(semantic);
            if (bootstrap?.UiPreferences.HighContrast == true) color = Color.Lerp(color, Color.white, .18f);
            float intensity = bootstrap?.UiPreferences.AnimationIntensity ?? 1f;
            float duration = (feedback.Kind == CombatFeedbackKind.UnitDefeated || feedback.Kind == CombatFeedbackKind.DestructibleDestroyed ? .24f : .16f) * Mathf.Lerp(.35f, 1f, intensity);
            PlayFormalVfx(feedback.Target, VfxForFeedback(feedback.Kind));
            if (bootstrap?.UiPreferences.FloatingText != false)
            {
                if (feedback.Kind == CombatFeedbackKind.Damage || feedback.Kind == CombatFeedbackKind.ShieldAbsorb)
                    ShowDamagePopup(feedback);
                else
                    ShowFloatingText(feedback.Target, feedback.FloatingText, color, semantic.Key);
            }

            UnitState targetUnit = bootstrap.CurrentState?.Units.Values.FirstOrDefault(unit => unit.Position == feedback.Target);
            UnitState sourceUnit = bootstrap.CurrentState?.Units.Values.FirstOrDefault(unit => unit.Position == feedback.Source);
            if (feedback.Kind == CombatFeedbackKind.Movement && targetUnit != null)
            {
                // Legacy position-only events cannot prove a route through obstacles.
                NotifyMovement(targetUnit.Id, feedback.Source, feedback.Target, null);
            }
            else if (feedback.Kind == CombatFeedbackKind.Damage || feedback.Kind == CombatFeedbackKind.ShieldAbsorb)
            {
                Vector2 direction = GridDirection(feedback.Source, feedback.Target);
                if (sourceUnit != null && sourceUnit != targetUnit && !actionPlayback.IsPlaying(Time.unscaledTime))
                    PlayUnitMotion(sourceUnit, UnitMotionKind.Attack, sourceUnit.IsHero ? .30f : .52f, direction, Vector2.zero);
                if (targetUnit != null) PlayUnitMotion(targetUnit, feedback.Kind == CombatFeedbackKind.ShieldAbsorb ? UnitMotionKind.ShieldHit : UnitMotionKind.Hit, sourceUnit?.IsHero == false ? .42f : .30f, direction, Vector2.zero);
                if (intensity > .01f) PulseCell(feedback.Target, color, duration * 1.5f);
            }
            else if (targetUnit != null && (feedback.Kind == CombatFeedbackKind.Healing || feedback.Kind == CombatFeedbackKind.ShieldRestore || feedback.Kind == CombatFeedbackKind.ManaRestore || feedback.Kind == CombatFeedbackKind.StatusCleared))
            {
                PlayUnitMotion(targetUnit, UnitMotionKind.Recover, targetUnit.IsHero ? .30f : .44f, Vector2.zero, Vector2.zero);
                if (intensity > .01f) PulseCell(feedback.Target, color, duration * 1.7f);
            }
            if (targetUnit != null)
            {
                if (feedback.Kind == CombatFeedbackKind.Damage || feedback.Kind == CombatFeedbackKind.Healing || feedback.Kind == CombatFeedbackKind.UnitDefeated)
                    healthCache[targetUnit.Id] = targetUnit.Health;
                if (feedback.Kind == CombatFeedbackKind.ShieldAbsorb || feedback.Kind == CombatFeedbackKind.ShieldRestore)
                    shieldCache[targetUnit.Id] = targetUnit.Shield;
                if (feedback.Kind == CombatFeedbackKind.ManaRestore)
                    manaCache[targetUnit.Id] = targetUnit.Mana;
                if (feedback.Kind == CombatFeedbackKind.Movement)
                    positionCache[targetUnit.Id] = targetUnit.Position;
                if (feedback.Kind == CombatFeedbackKind.Burning || feedback.Kind == CombatFeedbackKind.Bound || feedback.Kind == CombatFeedbackKind.Slow || feedback.Kind == CombatFeedbackKind.ArmorBreak || feedback.Kind == CombatFeedbackKind.StatusCleared)
                    statusCache[targetUnit.Id] = targetUnit.Statuses.ToDictionary(entry => entry.Key, entry => entry.Value);
                if (feedback.Kind == CombatFeedbackKind.Damage || feedback.Kind == CombatFeedbackKind.ShieldAbsorb)
                    hitUntil[targetUnit.Id] = Time.unscaledTime + .18f;
            }
        }

        public void NotifySkillDelivery(SkillDefinition skill, GridPosition source, GridPosition target)
        {
            if (skill == null) return;
            UnitState sourceUnit = bootstrap.CurrentState?.Units.Values.FirstOrDefault(unit => unit.Position == source);
            if (sourceUnit != null)
            {
                bool usesContactMotion = skill.Id == "enemy_tether_pounce" || skill.Id == "enemy_sundering_sigil" ||
                    skill.Id == "enemy_shield_ram" || skill.Id == "enemy_hooking_strike" || skill.Id == "enemy_vanguard_crush";
                UnitMotionKind motion = usesContactMotion ? UnitMotionKind.Attack : UnitMotionKind.Cast;
                float duration = sourceUnit.IsHero ? (usesContactMotion ? .30f : .34f) : (usesContactMotion ? .54f : .58f);
                if (usesContactMotion && actionCapture != null) actionCapture.SetImpact(target, duration * .52f);
                PlayUnitMotion(sourceUnit, motion, duration, GridDirection(source, target), Vector2.zero);
            }
            if (skill.Id == "enemy_stone_snare") { PlayDeliveryVfx(source, target, "bound"); return; }
            if (skill.Id == "enemy_revealing_lantern") { PlayDeliveryVfx(source, target, "armor_break"); return; }
            if (skill.Id == "enemy_windlass_bolt") { PlayDeliveryVfx(source, target, "heavy_hit"); return; }
            if (!skill.Effects.Any(effect => effect.Type == SkillEffectType.Damage && effect.DamageType == DamageType.Fire)) return;
            string effect = skill.Id == "cinder_sweep" ? "fire_spray" :
                skill.Id == "searing_mark" ? "fire_detonate" :
                skill.Delivery == SkillDeliveryMethod.Area ? "fire_cross_blast" : "fire_projectile";
            PlayDeliveryVfx(source, target, effect);
            // A Burning status is not a ground fire. Status feedback comes from the resolved unit state.
        }

        private void PlayDeliveryVfx(GridPosition source, GridPosition target, string effect)
        {
            if (actionCapture != null)
            {
                actionCapture.SetImpact(target, FireVfxSequence.StageDuration);
                actionCapture.Duration = Mathf.Max(actionCapture.Duration, FireVfxSequence.StageDuration * 2f);
                actionCapture.AddDelivery(() => PlayDeliveryVfx(source, target, effect)); return;
            }
            if (!AnimationsEnabled) return;
            vfxPlayback.ReplaceAbility(new[] { new CombatVfxCue(target, effect, effect == "fire_projectile" ? 0f : FireVfxSequence.StageDuration,
                FireVfxSequence.StageDuration, source, effect == "fire_projectile") }, FeedbackTime);
            RefreshVfx(Time.unscaledTime);
        }

        public void NotifyFireSpell(FireSpellExecution execution)
        {
            if (execution?.Preview?.Spell == null || !AnimationsEnabled) return;
            if (actionCapture != null)
            {
                actionCapture.AddDelivery(() => NotifyFireSpell(execution), FireVfxSequence.From(execution),
                    execution.IsTriggered ? actionCapture.TriggerDelay : 0f); return;
            }
            UnitState sourceUnit = bootstrap?.CurrentState?.GetUnit(execution.SourceUnitId);
            GridPosition primary = execution.Preview.Cells.FirstOrDefault();
            if (!execution.IsTriggered && sourceUnit != null)
                PlayUnitMotion(sourceUnit, UnitMotionKind.Cast, .34f,
                    GridDirection(execution.SourcePosition, primary), Vector2.zero);
            vfxPlayback.ReplaceAbility(FireVfxSequence.From(execution), FeedbackTime);
            RefreshVfx(Time.unscaledTime);
        }

        public static IReadOnlyList<string> FireVfxModules(FireSpellDefinition spell)
        {
            if (spell == null) return new string[0];
            var modules = new HashSet<string>();
            if (spell.DeliveryMode == FireDeliveryMode.WeaponAttachment || spell.DeliveryMode == FireDeliveryMode.BodyEnhancement ||
                spell.DeliveryMode == FireDeliveryMode.SelfStance || spell.DeliveryMode == FireDeliveryMode.TargetMarking)
                modules.Add("fire_attachment");
            if (spell.CombatAffinity == FireCombatAffinity.MeleeOnly && spell.DeliveryMode == FireDeliveryMode.ContactConduction)
                modules.Add("fire_melee_arc");
            else if (spell.DeliveryMode == FireDeliveryMode.DetachedProjection || spell.DeliveryMode == FireDeliveryMode.FiregroundManipulation)
                modules.Add("fire_projectile");
            if (spell.Shape == FireSelectionShape.Cone) modules.Add("fire_spray");
            if (spell.Shape == FireSelectionShape.Line || spell.Shape == FireSelectionShape.ContinuousLine || spell.Shape == FireSelectionShape.Path)
                modules.Add("fire_line");
            if (spell.Shape == FireSelectionShape.CenterAndOrthogonal || spell.Shape == FireSelectionShape.Square3)
                modules.Add("fire_cross_blast");
            if (spell.Rules.Any(rule => rule.Kind == FireRuleKind.Damage || rule.Kind == FireRuleKind.WeaponDamage))
                modules.Add("fire_impact");
            if (spell.Rules.Any(rule => rule.Kind == FireRuleKind.CreateFireground))
                modules.Add(spell.Shape == FireSelectionShape.ContinuousLine ? "fire_wall" : "fire_burning_ground");
            if (spell.Rules.Any(rule => rule.Kind == FireRuleKind.ConsumeBurning || rule.Kind == FireRuleKind.ConsumeFireground))
                modules.Add("fire_detonate");
            if (spell.Rules.Any(rule => rule.Kind == FireRuleKind.RestoreShield || rule.Kind == FireRuleKind.RestoreMana || rule.Kind == FireRuleKind.GrantShieldBeforeRanged))
                modules.Add("fire_absorb");
            if (spell.Rules.Any(rule => rule.Kind == FireRuleKind.ApplyBreakStance)) modules.Add("fire_break_stance");
            if (spell.Id == "F-P-M19" || spell.Id == "F-P-U20" || spell.Id == "F-P-R20") modules.Add("fire_overlimit");
            if (modules.Count == 0) modules.Add("fire_attachment");
            return modules.ToArray();
        }

        private void PlayUnitMotion(UnitState unit, UnitMotionKind kind, float duration, Vector2 direction, Vector2 originOffset)
        {
            if (actionCapture != null)
            {
                actionCapture.AddDelivery(() => PlayUnitMotion(unit, kind, duration, direction, originOffset)); return;
            }
            if (unit == null || (bootstrap?.UiPreferences.AnimationIntensity ?? 1f) <= .01f) return;
            unitMotions[unit.Id] = new UnitMotion(kind, duration, direction, originOffset, FeedbackTime);
        }

        public int EnemyAnimationFrame(UnitState unit)
        {
            if (unit == null || unit.IsHero || !unitMotions.TryGetValue(unit.Id, out UnitMotion motion)) return -1;
            float elapsed = Time.unscaledTime - motion.StartedAt;
            if (elapsed < 0f || elapsed >= motion.Duration) return -1;
            return UnitEndpointAnimationPolicy.FrameIndex(elapsed / Mathf.Max(.01f, motion.Duration));
        }

        public static int VfxPriority(string effect)
        {
            switch (effect)
            {
                case "fire_overlimit": return 100;
                case "fire_detonate": case "fire_cross_blast": case "fire_wall": return 90;
                case "fire_impact": case "fire_melee_arc": case "fire_break_stance": case "fire_absorb": return 80;
                case "fire_projectile": case "fire_spray": case "fire_line": return 70;
                case "fire_cast": case "fire_attachment": return 60;
                case "fire_burning_ground": return 30;
                default: return 50;
            }
        }

        private static Vector2 GridDirection(GridPosition source, GridPosition target)
        {
            Vector2 direction = new Vector2(Mathf.Sign(target.X - source.X), -Mathf.Sign(target.Y - source.Y));
            return direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        private void PlayFormalVfx(GridPosition position, string effect)
        {
            if (!AnimationsEnabled) return;
            vfxPlayback.PlayReaction(position, effect, VfxPriority(effect), FeedbackTime);
            RefreshVfx(Time.unscaledTime);
        }

        private void RefreshVfx(float now)
        {
            if (!AnimationsEnabled) { ClearVfx(); unitMotions.Clear(); return; }
            IReadOnlyList<CombatVfxSample> samples = vfxPlayback.Sample(now);
            if (samples.Count > 0)
            {
                EnsureCanvas();
                if (vfxRoot == null)
                {
                    GameObject root = new GameObject("分层战斗特效", typeof(RectTransform));
                    vfxRoot = root.GetComponent<RectTransform>(); vfxRoot.SetParent(FeedbackParent, false);
                    vfxRoot.anchorMin = Vector2.zero; vfxRoot.anchorMax = Vector2.one;
                    vfxRoot.offsetMin = vfxRoot.offsetMax = Vector2.zero;
                    vfxRoot.SetAsFirstSibling();
                }
            }
            visibleVfx.Clear();
            foreach (CombatVfxSample sample in samples)
            {
                visibleVfx.Add(sample.Slot);
                if (!activeVfx.TryGetValue(sample.Slot, out Image view) || view == null)
                {
                    GameObject root = new GameObject("正式VFX", typeof(RectTransform), typeof(Image));
                    root.transform.SetParent(vfxRoot, false); view = root.GetComponent<Image>();
                    view.rectTransform.anchorMin = view.rectTransform.anchorMax = new Vector2(.5f, .5f);
                    view.preserveAspect = true; view.raycastTarget = false; view.color = new Color(1f, 1f, 1f, .88f);
                    activeVfx[sample.Slot] = view;
                }
                view.name = "正式VFX_" + sample.Cue.Effect;
                Sprite[] frames = FormalVfxFrames(sample.Cue.Effect);
                view.sprite = frames[Mathf.Clamp(Mathf.FloorToInt(sample.Progress * frames.Length), 0, frames.Length - 1)];
                Vector2 position = CurrentGridFeedbackPosition(sample.Cue.Position);
                if (sample.Cue.Travels)
                    position = Vector2.Lerp(CurrentGridFeedbackPosition(sample.Cue.Origin), position, sample.Progress);
                view.rectTransform.anchoredPosition = new Vector2(Mathf.Round(position.x / 2f) * 2f, Mathf.Round(position.y / 2f) * 2f);
                float size = CurrentFeedbackCellSize(); view.rectTransform.sizeDelta = new Vector2(size, size);
            }
            expiredVfx.Clear();
            foreach (var entry in activeVfx)
                if (!visibleVfx.Contains(entry.Key)) expiredVfx.Add(entry.Key);
                else if (entry.Key.Layer == CombatVfxLayer.Reaction) entry.Value.transform.SetAsLastSibling();
            foreach (CombatVfxSlot slot in expiredVfx)
            { if (activeVfx[slot] != null) DestroyFeedbackObject(activeVfx[slot].gameObject); activeVfx.Remove(slot); }
        }

        private void ClearVfx()
        {
            vfxPlayback.Clear();
            foreach (Image view in activeVfx.Values) if (view != null) DestroyFeedbackObject(view.gameObject);
            activeVfx.Clear(); visibleVfx.Clear(); expiredVfx.Clear();
            if (battlefieldClip != null)
                foreach (Transform child in battlefieldClip.Cast<Transform>().Where(child => child.name == "战斗反馈脉冲").ToArray())
                { DOTween.Kill(child.gameObject); DestroyFeedbackObject(child.gameObject); }
        }

        private static void DestroyFeedbackObject(GameObject value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }

        private Sprite[] FormalVfxFrames(string effect)
        {
            if (vfxFrames.TryGetValue(effect, out Sprite[] cached)) return cached;
            string root = FormalArtRegistry.VfxPath(effect);
            Sprite[] frames = Enumerable.Range(0, 6).Select(index => Resources.Load<Sprite>(root + "/frame_" + index.ToString("00"))).ToArray();
            if (frames.Any(frame => frame == null)) throw new KeyNotFoundException("Incomplete formal VFX frames: " + effect);
            vfxFrames[effect] = frames;
            return frames;
        }

        private static string VfxForFeedback(CombatFeedbackKind kind)
        {
            switch (kind)
            {
                case CombatFeedbackKind.Damage: return "hit";
                case CombatFeedbackKind.ShieldAbsorb: return "shield_absorb";
                case CombatFeedbackKind.ArmorBreak: return "armor_break";
                case CombatFeedbackKind.Burning: return "burning";
                case CombatFeedbackKind.Bound: return "bound";
                case CombatFeedbackKind.Slow: return "slow";
                case CombatFeedbackKind.Healing: return "health_repair";
                case CombatFeedbackKind.ShieldRestore: return "shield_restore";
                case CombatFeedbackKind.ManaRestore: return "mana_restore";
                case CombatFeedbackKind.StatusCleared: return "cleanse";
                case CombatFeedbackKind.Movement: return "path";
                case CombatFeedbackKind.DestructibleDamaged: return "object_damage";
                case CombatFeedbackKind.DestructibleDestroyed: return "object_break";
                case CombatFeedbackKind.UnitDefeated: return "heavy_hit";
                default: throw new KeyNotFoundException("Missing formal VFX semantic: " + kind);
            }
        }

        private void PulseCell(GridPosition position, Color color, float duration)
        {
            EnsureCanvas();
            GameObject pulse = new GameObject("战斗反馈脉冲"); pulse.transform.SetParent(FeedbackParent, false);
            RectTransform rect = pulse.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = CurrentGridFeedbackPosition(position);
            BuildPulseFrame(rect, CurrentFeedbackCellSize(), color);
            CanvasGroup group = pulse.AddComponent<CanvasGroup>(); group.alpha = .85f;
            DOTween.To(() => group.alpha, value => group.alpha = value, 0f, duration)
                .SetUpdate(true).SetTarget(pulse).OnComplete(() => DestroyFeedbackObject(pulse));
        }

        private static void BuildPulseFrame(RectTransform rect, float cellSize, Color color)
        {
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            rect.localScale = Vector3.one;
            float edge = cellSize * .5f - 1f;
            AddBorder(rect, new Vector2(0, edge), new Vector2(cellSize, 2f), color);
            AddBorder(rect, new Vector2(0, -edge), new Vector2(cellSize, 2f), color);
            AddBorder(rect, new Vector2(-edge, 0), new Vector2(2f, cellSize - 4f), color);
            AddBorder(rect, new Vector2(edge, 0), new Vector2(2f, cellSize - 4f), color);
        }

        private void ShowFloatingText(GridPosition position, string message, Color color, string iconKey)
        {
            EnsureCanvas();
            GameObject textObject = new GameObject("伤害反馈"); textObject.transform.SetParent(FeedbackParent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            // The board occupies the left 75% of the 1920 reference canvas.
            rect.anchoredPosition = FloatingFeedbackPosition(position); rect.sizeDelta = FloatingTextSize;
            Image backing = textObject.AddComponent<Image>();
            backing.color = FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .96f);
            backing.raycastTarget = false;
            Sprite icon = SemanticIcon(iconKey);
            if (icon != null)
            {
                GameObject iconObject = new GameObject("反馈图标_" + iconKey); iconObject.transform.SetParent(textObject.transform, false);
                RectTransform iconRect = iconObject.AddComponent<RectTransform>(); iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, .5f); iconRect.pivot = new Vector2(0, .5f); iconRect.anchoredPosition = new Vector2(4, 0); iconRect.sizeDelta = new Vector2(32, 32);
                Image image = iconObject.AddComponent<Image>(); image.sprite = icon; image.color = color; image.preserveAspect = true; image.raycastTarget = false;
            }
            GameObject labelObject = new GameObject("反馈文字"); labelObject.transform.SetParent(textObject.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = new Vector2(40, 0); labelRect.offsetMax = Vector2.zero;
            Text text = labelObject.AddComponent<Text>(); text.font = FormalUiKit.Font; text.fontSize = FormalUiTheme.BodyFontSize; text.fontStyle = FontStyle.Normal; text.alignment = TextAnchor.MiddleLeft; text.text = message; text.color = FormalUiTheme.ReadableLabelColor(color); text.raycastTarget = false;
            CanvasGroup group = textObject.AddComponent<CanvasGroup>();
            if (!AnimationsEnabled)
            {
                group.alpha = 1f;
                DOVirtual.DelayedCall(.42f, () => { if (textObject != null) DestroyFeedbackObject(textObject); }).SetUpdate(true).SetTarget(textObject);
                return;
            }
            float targetY = rect.anchoredPosition.y + FloatingTextRise;
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(textObject);
            sequence.Join(DOTween.To(() => rect.anchoredPosition.y, value => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Mathf.Round(value / 2f) * 2f), targetY, .42f).SetEase(Ease.OutCubic)).Join(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .42f));
            sequence.OnComplete(() => DestroyFeedbackObject(textObject));
        }

        private void ShowDamagePopup(CombatFeedbackEvent feedback)
        {
            CombatDamagePopupPresentation next = CombatDamagePopupPresentation.From(feedback);
            float now = Time.unscaledTime;
            if (activeDamagePopups.TryGetValue(feedback.Target, out DamagePopupState current) &&
                current?.Root != null && now - current.LastUpdate <= .10f)
            {
                current.Presentation = current.Presentation.Merge(next);
                current.LastUpdate = now;
                ApplyDamagePopupStyle(current);
                return;
            }

            EnsureCanvas();
            int lane = damagePopupSerial++ % 3;
            GameObject root = new GameObject("实际伤害跳字");
            root.transform.SetParent(FeedbackParent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = DamagePopupPosition(feedback.Target, lane);
            rect.sizeDelta = DamageTextSize;
            Text label = root.AddComponent<Text>();
            label.font = FormalUiKit.Font;
            label.fontSize = FormalUiTheme.FeedbackFontSize;
            label.fontStyle = FontStyle.Normal;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            DamagePopupState popup = new DamagePopupState
            {
                Root = root,
                Rect = rect,
                Label = label,
                Group = group,
                Presentation = next,
                LastUpdate = now
            };
            activeDamagePopups[feedback.Target] = popup;
            ApplyDamagePopupStyle(popup);

            if (!AnimationsEnabled)
            {
                group.alpha = 1f;
                DOVirtual.DelayedCall(.48f, () => CompleteDamagePopup(feedback.Target, popup)).SetUpdate(true).SetTarget(root);
                return;
            }

            float targetY = rect.anchoredPosition.y + DamageTextRise;
            DOTween.Sequence().SetUpdate(true).SetTarget(root)
                .Join(DOTween.To(() => rect.anchoredPosition.y,
                    value => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Mathf.Round(value / 2f) * 2f), targetY, .56f).SetEase(Ease.OutCubic))
                .Insert(.28f, DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .28f))
                .OnComplete(() => CompleteDamagePopup(feedback.Target, popup));
        }

        private void ApplyDamagePopupStyle(DamagePopupState popup)
        {
            popup.Label.text = popup.Presentation.Text;
            CombatFeedbackKind colorKind = popup.Presentation.IncludesHealthDamage
                ? CombatFeedbackKind.Damage
                : CombatFeedbackKind.ShieldAbsorb;
            Color color = SemanticColor(CombatFeedbackCatalog.For(colorKind));
            if (bootstrap?.UiPreferences.HighContrast == true) color = Color.Lerp(color, Color.white, .18f);
            popup.Label.color = color;
        }

        private void CompleteDamagePopup(GridPosition position, DamagePopupState popup)
        {
            if (activeDamagePopups.TryGetValue(position, out DamagePopupState current) && ReferenceEquals(current, popup))
                activeDamagePopups.Remove(position);
            if (popup?.Root != null) DestroyFeedbackObject(popup.Root);
        }

        private Sprite SemanticIcon(string iconKey)
        {
            if (semanticIcons.TryGetValue(iconKey, out Sprite sprite)) return sprite;
            sprite = Resources.Load<Sprite>(FormalArtRegistry.FeedbackPath(iconKey));
            if (sprite == null) throw new KeyNotFoundException("Missing formal feedback icon: " + iconKey);
            semanticIcons[iconKey] = sprite;
            return sprite;
        }

        private static void AddBorder(RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            GameObject border = new GameObject("反馈边线"); border.transform.SetParent(parent, false);
            RectTransform rect = border.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = border.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
        }

        public static Vector2 GridFeedbackPosition(GridPosition position)
        {
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();
            BattlefieldRect board = adapter.BoardRect();
            BattlefieldRect cell = adapter.CellRect(board, BattlefieldPresentationAdapter.DefaultHeight, position);
            return new Vector2(cell.X + cell.Width * .5f - 960f, 540f - cell.Y - cell.Height * .5f);
        }

        public static Vector2 CanvasToFeedbackLocal(Vector2 canvasPosition, Vector2 clipCenter) => canvasPosition - clipCenter;

        private Vector2 CurrentGridFeedbackCanvasPosition(GridPosition position) =>
            bootstrap != null ? bootstrap.GridToFeedbackPosition(position) : GridFeedbackPosition(position);

        private float CurrentFeedbackCellSize()
        {
            if (bootstrap == null) return BattlefieldPresentationAdapter.OverviewCellSize;
            float cell = Mathf.Abs(bootstrap.GridToFeedbackPosition(new GridPosition(1, 0)).x -
                bootstrap.GridToFeedbackPosition(new GridPosition(0, 0)).x);
            return Mathf.Max(64f, Mathf.Round(cell / 64f) * 64f);
        }

        private Vector2 CurrentGridFeedbackPosition(GridPosition position) => CanvasToFeedbackLocal(
            CurrentGridFeedbackCanvasPosition(position), battlefieldClip != null ? battlefieldClip.anchoredPosition : Vector2.zero);

        private Vector2 FloatingFeedbackPosition(GridPosition position)
        {
            Vector2 center = CurrentGridFeedbackCanvasPosition(position) + new Vector2(0, 34);
            center = ClampFeedbackCenter(center, FloatingTextSize, CurrentFeedbackViewport,
                AnimationsEnabled ? FloatingTextRise : 0f);
            return CanvasToFeedbackLocal(center, battlefieldClip != null ? battlefieldClip.anchoredPosition : Vector2.zero);
        }

        private Vector2 DamagePopupPosition(GridPosition position, int lane)
        {
            Vector2 center = CurrentGridFeedbackCanvasPosition(position) + new Vector2((lane - 1) * 12f, 20f + lane * 4f);
            center = ClampFeedbackCenter(center, DamageTextSize, CurrentFeedbackViewport,
                AnimationsEnabled ? DamageTextRise : 0f);
            return CanvasToFeedbackLocal(center, battlefieldClip != null ? battlefieldClip.anchoredPosition : Vector2.zero);
        }

        private BattlefieldRect CurrentFeedbackViewport =>
            bootstrap?.CurrentBattlefieldViewport ?? new BattlefieldPresentationAdapter().ViewportRect;

        public static Vector2 ClampFeedbackCenter(Vector2 desired, Vector2 size, BattlefieldRect viewport, float rise)
        {
            const float margin = 8f;
            float left = viewport.X - UiLayoutContract.ReferenceWidth * .5f + size.x * .5f + margin;
            float right = viewport.XMax - UiLayoutContract.ReferenceWidth * .5f - size.x * .5f - margin;
            float bottom = UiLayoutContract.ReferenceHeight * .5f - viewport.YMax + size.y * .5f + margin;
            float top = UiLayoutContract.ReferenceHeight * .5f - viewport.Y - size.y * .5f - margin - Mathf.Max(0f, rise);
            return new Vector2(Mathf.Round(Mathf.Clamp(desired.x, left, right) / 2f) * 2f,
                Mathf.Round(Mathf.Clamp(desired.y, bottom, top) / 2f) * 2f);
        }

        private static Color SemanticColor(CombatFeedbackSemantic semantic)
        {
            return ColorUtility.TryParseHtmlString(semantic.ColorHex, out Color color) ? color : Color.white;
        }

        private bool AnimationsEnabled => CombatFeedbackPresentationPolicy.AnimationsEnabled(bootstrap?.UiPreferences.AnimationIntensity ?? 1f);

        private Transform FeedbackParent => battlefieldClip != null ? battlefieldClip : canvas.transform;

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            GameObject root = new GameObject("运行时战斗反馈"); DontDestroyOnLoad(root);
            canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60; canvas.pixelPerfect = true;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(UiLayoutContract.ReferenceWidth, UiLayoutContract.ReferenceHeight); scaler.matchWidthOrHeight = UiLayoutContract.MatchWidthOrHeight;
            GameObject clip = new GameObject("战术视口反馈裁切"); clip.transform.SetParent(canvas.transform, false);
            battlefieldClip = clip.AddComponent<RectTransform>();
            battlefieldClip.anchorMin = battlefieldClip.anchorMax = new Vector2(.5f, .5f);
            BattlefieldRect view = bootstrap?.CurrentBattlefieldViewport ?? new BattlefieldPresentationAdapter().ViewportRect;
            battlefieldClip.anchoredPosition = new Vector2(view.X + view.Width * .5f - 960f, 540f - view.Y - view.Height * .5f);
            battlefieldClip.sizeDelta = new Vector2(view.Width, view.Height);
            clip.AddComponent<RectMask2D>();
        }
    }
}
