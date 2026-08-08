using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Runtime-only presentation layer: it adds readable feedback without changing the authored scene HUD.
    public sealed class CombatVisualFeedback : MonoBehaviour
    {
        private readonly Dictionary<string, int> healthCache = new Dictionary<string, int>();
        private readonly Dictionary<string, int> shieldCache = new Dictionary<string, int>();
        private readonly Dictionary<string, GridPosition> positionCache = new Dictionary<string, GridPosition>();
        private readonly Dictionary<GridPosition, int> durabilityCache = new Dictionary<GridPosition, int>();
        private readonly Dictionary<string, Dictionary<StatusType, int>> statusCache = new Dictionary<string, Dictionary<StatusType, int>>();
        private readonly Dictionary<string, float> hitUntil = new Dictionary<string, float>();
        private readonly Dictionary<string, UnitMotion> unitMotions = new Dictionary<string, UnitMotion>();
        private readonly Dictionary<string, Sprite> semanticIcons = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite[]> vfxFrames = new Dictionary<string, Sprite[]>();
        private CombatPrototypeBootstrap bootstrap;
        private Canvas canvas;
        private string lastOutcome;
        private string activeUnitId;

        private enum UnitMotionKind { Move, Attack, Cast, Hit, ShieldHit, Recover, Ready }

        private readonly struct UnitMotion
        {
            public UnitMotionKind Kind { get; }
            public float StartedAt { get; }
            public float Duration { get; }
            public Vector2 Direction { get; }
            public Vector2 OriginOffset { get; }

            public UnitMotion(UnitMotionKind kind, float duration, Vector2 direction, Vector2 originOffset)
            {
                Kind = kind;
                StartedAt = Time.unscaledTime;
                Duration = duration;
                Direction = direction;
                OriginOffset = originOffset;
            }
        }

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
                if (shieldCache.TryGetValue(unit.Id, out int previousShield))
                {
                    if (unit.Shield < previousShield) Publish(new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb, unit.Position, previousShield - unit.Shield));
                    else if (unit.Shield > previousShield) Publish(new CombatFeedbackEvent(CombatFeedbackKind.ShieldRestore, unit.Position, unit.Shield - previousShield));
                }
                shieldCache[unit.Id] = unit.Shield;
                if (healthCache.TryGetValue(unit.Id, out int previousHealth))
                {
                    if (unit.Health < previousHealth) Publish(new CombatFeedbackEvent(CombatFeedbackKind.Damage, unit.Position, previousHealth - unit.Health));
                    else if (unit.Health > previousHealth) Publish(new CombatFeedbackEvent(CombatFeedbackKind.Healing, unit.Position, unit.Health - previousHealth));
                    if (previousHealth > 0 && !unit.IsAlive) Publish(new CombatFeedbackEvent(CombatFeedbackKind.UnitDefeated, unit.Position));
                }
                healthCache[unit.Id] = unit.Health;
                if (positionCache.TryGetValue(unit.Id, out GridPosition previousPosition) && previousPosition != unit.Position)
                    Publish(new CombatFeedbackEvent(CombatFeedbackKind.Movement, previousPosition, unit.Position));
                positionCache[unit.Id] = unit.Position;
                statusCache.TryGetValue(unit.Id, out Dictionary<StatusType, int> previousStatuses);
                foreach (KeyValuePair<StatusType, int> status in unit.Statuses)
                    if (previousStatuses == null || !previousStatuses.TryGetValue(status.Key, out int previousDuration) || status.Value > previousDuration)
                        NotifyStatusApplied(unit.Position, status.Key, status.Value);
                statusCache[unit.Id] = unit.Statuses.ToDictionary(entry => entry.Key, entry => entry.Value);
            }
            if (activeUnitId != bootstrap.CurrentState.ActiveUnitId)
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
            Text label = card.AddComponent<Text>(); label.font = FormalUiKit.Font; label.fontSize = 36; label.alignment = TextAnchor.MiddleCenter; label.text = victory ? "战斗胜利" : "战斗失败"; label.color = victory ? new Color(.48f, .92f, 1f, 0f) : new Color(.94f, .36f, .32f, 0f);
            CanvasGroup group = card.AddComponent<CanvasGroup>(); group.alpha = 0f; rect.localScale = Vector3.one * .84f;
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, .16f)).Join(rect.DOScale(1f, .2f).SetEase(Ease.OutBack));
            sequence.AppendInterval(.7f).Append(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .22f)).OnComplete(() => Destroy(card));
        }

        public void ResetBattleFeedback()
        {
            lastOutcome = null;
            healthCache.Clear();
            shieldCache.Clear(); positionCache.Clear(); durabilityCache.Clear(); statusCache.Clear(); hitUntil.Clear(); unitMotions.Clear(); activeUnitId = null;
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
            return new Vector2(Mathf.Round(offset.x * intensity), Mathf.Round(offset.y * intensity));
        }

        public Color UnitPresentationTint(UnitState unit)
        {
            if (unit == null || !unitMotions.TryGetValue(unit.Id, out UnitMotion motion)) return Color.white;
            float progress = Mathf.Clamp01((Time.unscaledTime - motion.StartedAt) / Mathf.Max(.01f, motion.Duration));
            float flash = Mathf.Sin(progress * Mathf.PI) * (bootstrap?.UiPreferences.AnimationIntensity ?? 1f);
            if (motion.Kind == UnitMotionKind.Hit) return Color.Lerp(Color.white, new Color(1f, .36f, .28f), flash * .72f);
            if (motion.Kind == UnitMotionKind.ShieldHit) return Color.Lerp(Color.white, new Color(.35f, .92f, 1f), flash * .68f);
            if (motion.Kind == UnitMotionKind.Recover) return Color.Lerp(Color.white, new Color(.48f, 1f, .66f), flash * .52f);
            if (motion.Kind == UnitMotionKind.Cast) return Color.Lerp(Color.white, new Color(.55f, .86f, 1f), flash * .38f);
            if (motion.Kind == UnitMotionKind.Ready) return Color.Lerp(Color.white, new Color(1f, .82f, .38f), flash * .28f);
            return Color.white;
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

        public void NotifyMovement(GridPosition source, GridPosition target)
        {
            if (source != target) Publish(new CombatFeedbackEvent(CombatFeedbackKind.Movement, source, target));
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
            EnsureCanvas();
            CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(feedback.Kind);
            Color color = SemanticColor(semantic);
            if (bootstrap?.UiPreferences.HighContrast == true) color = Color.Lerp(color, Color.white, .18f);
            float intensity = bootstrap?.UiPreferences.AnimationIntensity ?? 1f;
            float duration = (feedback.Kind == CombatFeedbackKind.UnitDefeated || feedback.Kind == CombatFeedbackKind.DestructibleDestroyed ? .24f : .16f) * Mathf.Lerp(.35f, 1f, intensity);
            PlayFormalVfx(feedback.Target, VfxForFeedback(feedback.Kind));
            if (bootstrap?.UiPreferences.FloatingText != false) ShowFloatingText(feedback.Target, feedback.FloatingText, color, semantic.Key);

            UnitState targetUnit = bootstrap.CurrentState?.Units.Values.FirstOrDefault(unit => unit.Position == feedback.Target);
            UnitState sourceUnit = bootstrap.CurrentState?.Units.Values.FirstOrDefault(unit => unit.Position == feedback.Source);
            if (feedback.Kind == CombatFeedbackKind.Movement && targetUnit != null)
            {
                Vector2 origin = new Vector2((feedback.Source.X - feedback.Target.X) * 78f, (feedback.Source.Y - feedback.Target.Y) * 78f);
                // The resolved unit is already on the destination cell. Keep the feedback local so a
                // multi-cell move does not sweep a sprite across most of the battlefield.
                origin = Vector2.ClampMagnitude(origin, 28f);
                PlayUnitMotion(targetUnit, UnitMotionKind.Move, .18f, Vector2.zero, origin);
            }
            else if (feedback.Kind == CombatFeedbackKind.Damage || feedback.Kind == CombatFeedbackKind.ShieldAbsorb)
            {
                Vector2 direction = GridDirection(feedback.Source, feedback.Target);
                if (sourceUnit != null && sourceUnit != targetUnit) PlayUnitMotion(sourceUnit, UnitMotionKind.Attack, .26f, direction, Vector2.zero);
                if (targetUnit != null) PlayUnitMotion(targetUnit, feedback.Kind == CombatFeedbackKind.ShieldAbsorb ? UnitMotionKind.ShieldHit : UnitMotionKind.Hit, .24f, direction, Vector2.zero);
                if (intensity > .01f) PulseCell(feedback.Target, color, duration * 1.5f);
            }
            else if (targetUnit != null && (feedback.Kind == CombatFeedbackKind.Healing || feedback.Kind == CombatFeedbackKind.ShieldRestore || feedback.Kind == CombatFeedbackKind.ManaRestore || feedback.Kind == CombatFeedbackKind.StatusCleared))
            {
                PlayUnitMotion(targetUnit, UnitMotionKind.Recover, .30f, Vector2.zero, Vector2.zero);
                if (intensity > .01f) PulseCell(feedback.Target, color, duration * 1.7f);
            }
            if (targetUnit != null)
            {
                if (feedback.Kind == CombatFeedbackKind.Damage || feedback.Kind == CombatFeedbackKind.Healing || feedback.Kind == CombatFeedbackKind.UnitDefeated)
                    healthCache[targetUnit.Id] = targetUnit.Health;
                if (feedback.Kind == CombatFeedbackKind.ShieldAbsorb || feedback.Kind == CombatFeedbackKind.ShieldRestore)
                    shieldCache[targetUnit.Id] = targetUnit.Shield;
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
                PlayUnitMotion(sourceUnit, motion, usesContactMotion ? .30f : .34f, GridDirection(source, target), Vector2.zero);
            }
            if (skill.Id == "enemy_stone_snare") { PlayFormalVfx(target, "bound"); return; }
            if (skill.Id == "enemy_revealing_lantern") { PlayFormalVfx(target, "armor_break"); return; }
            if (skill.Id == "enemy_windlass_bolt") { PlayFormalVfx(target, "heavy_hit"); return; }
            if (!skill.Effects.Any(effect => effect.Type == SkillEffectType.Damage && effect.DamageType == DamageType.Fire)) return;
            string effect = skill.Id == "cinder_sweep" ? "fire_spray" :
                skill.Id == "searing_mark" ? "fire_detonate" :
                skill.Delivery == SkillDeliveryMethod.Area ? "fire_cross_blast" : "fire_projectile";
            PlayFormalVfx(target, effect);
            if (skill.Status == StatusType.Burning && skill.Delivery == SkillDeliveryMethod.Area)
                PlayFormalVfx(target, "fire_burning_ground");
        }

        public void NotifyFireSpell(FireSpellDefinition spell, GridPosition source, IReadOnlyList<GridPosition> targetCells)
        {
            if (spell == null || targetCells == null || targetCells.Count == 0) return;
            UnitState sourceUnit = bootstrap.CurrentState?.Units.Values.FirstOrDefault(unit => unit.Position == source);
            GridPosition primary = targetCells[0];
            if (sourceUnit != null) PlayUnitMotion(sourceUnit, UnitMotionKind.Cast, .34f, GridDirection(source, primary), Vector2.zero);
            foreach (string module in spell.PresentationModules)
            {
                IEnumerable<GridPosition> positions = spell.Shape == FireSelectionShape.Single ? new[] { primary } : targetCells;
                foreach (GridPosition position in positions.Distinct()) PlayFormalVfx(position, module);
            }
        }

        private void PlayUnitMotion(UnitState unit, UnitMotionKind kind, float duration, Vector2 direction, Vector2 originOffset)
        {
            if (unit == null || (bootstrap?.UiPreferences.AnimationIntensity ?? 1f) <= .01f) return;
            unitMotions[unit.Id] = new UnitMotion(kind, duration, direction, originOffset);
        }

        private static Vector2 GridDirection(GridPosition source, GridPosition target)
        {
            Vector2 direction = new Vector2(Mathf.Sign(target.X - source.X), Mathf.Sign(target.Y - source.Y));
            return direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        private void PlayFormalVfx(GridPosition position, string effect)
        {
            EnsureCanvas();
            Sprite[] frames = FormalVfxFrames(effect);
            GameObject root = new GameObject("正式VFX_" + effect); root.transform.SetParent(canvas.transform, false);
            RectTransform rect = root.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = FeedbackPosition(position); rect.sizeDelta = new Vector2(72, 72);
            Image image = root.AddComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
            StartCoroutine(AnimateVfx(root, image, frames));
        }

        private static IEnumerator AnimateVfx(GameObject root, Image image, IReadOnlyList<Sprite> frames)
        {
            for (int index = 0; index < frames.Count; index++)
            {
                if (root == null) yield break;
                image.sprite = frames[index];
                yield return new WaitForSecondsRealtime(.07f);
            }
            if (root != null) Destroy(root);
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
            GameObject pulse = new GameObject("战斗反馈脉冲"); pulse.transform.SetParent(canvas.transform, false);
            RectTransform rect = pulse.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = FeedbackPosition(position); rect.sizeDelta = new Vector2(66, 66);
            AddBorder(rect, new Vector2(0, 30), new Vector2(66, 4), color);
            AddBorder(rect, new Vector2(0, -30), new Vector2(66, 4), color);
            AddBorder(rect, new Vector2(-30, 0), new Vector2(4, 58), color);
            AddBorder(rect, new Vector2(30, 0), new Vector2(4, 58), color);
            CanvasGroup group = pulse.AddComponent<CanvasGroup>(); group.alpha = .85f; rect.localScale = Vector3.one * .7f;
            DOTween.Sequence().SetUpdate(true).Join(rect.DOScale(1.18f, duration).SetEase(Ease.OutQuad)).Join(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, duration)).OnComplete(() => Destroy(pulse));
        }

        private void ShowFloatingText(GridPosition position, string message, Color color, string iconKey)
        {
            EnsureCanvas();
            GameObject textObject = new GameObject("伤害反馈"); textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            // The board occupies the left 75% of the 1920 reference canvas.
            rect.anchoredPosition = FloatingFeedbackPosition(position); rect.sizeDelta = new Vector2(176, 28);
            Sprite icon = SemanticIcon(iconKey);
            if (icon != null)
            {
                GameObject iconObject = new GameObject("反馈图标_" + iconKey); iconObject.transform.SetParent(textObject.transform, false);
                RectTransform iconRect = iconObject.AddComponent<RectTransform>(); iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, .5f); iconRect.pivot = new Vector2(0, .5f); iconRect.anchoredPosition = new Vector2(4, 0); iconRect.sizeDelta = new Vector2(22, 22);
                Image image = iconObject.AddComponent<Image>(); image.sprite = icon; image.color = color; image.preserveAspect = true; image.raycastTarget = false;
            }
            GameObject labelObject = new GameObject("反馈文字"); labelObject.transform.SetParent(textObject.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = new Vector2(28, 0); labelRect.offsetMax = Vector2.zero;
            Text text = labelObject.AddComponent<Text>(); text.font = FormalUiKit.Font; text.fontSize = 18; text.alignment = TextAnchor.MiddleLeft; text.text = message; text.color = color; text.raycastTarget = false;
            CanvasGroup group = textObject.AddComponent<CanvasGroup>();
            float targetY = rect.anchoredPosition.y + 28f;
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(DOTween.To(() => rect.anchoredPosition.y, value => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, value), targetY, .42f).SetEase(Ease.OutCubic)).Join(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .42f));
            sequence.OnComplete(() => Destroy(textObject));
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

        private static Vector2 FeedbackPosition(GridPosition position)
        {
            // Mirrors the centered 12x9 board inside the 1440px tactical field.
            return new Vector2(-669 + position.X * 78, 389 - position.Y * 78);
        }

        private static Vector2 FloatingFeedbackPosition(GridPosition position)
        {
            Vector2 center = FeedbackPosition(position) + new Vector2(0, 34);
            // 176x28 label plus an 8px safety margin; keep all feedback inside the left 75% board.
            center.x = Mathf.Clamp(center.x, -864f, 392f);
            center.y = Mathf.Clamp(center.y, -518f, 518f);
            return center;
        }

        private static Color SemanticColor(CombatFeedbackSemantic semantic)
        {
            return ColorUtility.TryParseHtmlString(semantic.ColorHex, out Color color) ? color : Color.white;
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
