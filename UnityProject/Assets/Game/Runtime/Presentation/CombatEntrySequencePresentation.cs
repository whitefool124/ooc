using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// Owns the combat-entry cinematic and the standing mission brief.
    ///
    /// Entry order: the fight fades up from black already at play scale - there is no zoom-out
    /// or push-in - the mission goal docks into the battlefield's top-left corner, and the
    /// camera then tours every enemy in turn behind a named portrait card before returning to
    /// the hero and floating a short "mission start" notice.
    ///
    /// The sequence is presentation-only: it drives <see cref="BattlefieldViewport"/> through
    /// <see cref="BattlefieldViewport.ApplyCinematicPose"/> and never touches combat state.
    /// </summary>
    public sealed class CombatEntrySequencePresentation : MonoBehaviour
    {
        // The fight opens straight at play scale - no zoom-out, no push-in. The only opening
        // beat is a short look at the field before the camera starts touring the enemies.
        private const float FieldLookSeconds = .9f;
        private const float TourApproachSeconds = .34f;
        private const float TourHoldSeconds = 1.05f;
        private const float TourExitSeconds = .2f;
        private const float ReturnToHeroSeconds = .6f;
        // Fade from black: hold at full black first so the cut is actually visible, then lift.
        private const float VeilHoldSeconds = .32f;
        private const float VeilFadeSeconds = .9f;
        // A single frame can be arbitrarily long (combat construction shares it); clamping keeps
        // that hitch from swallowing the whole hold-and-fade in one step.
        private const float MaxFrameStepSeconds = .05f;
        private const float RevealZoomScale = 1.15f;
        private const float RevealFrameSize = 232f;
        private const float OverlaySortingOffset = -10f;
        private const float StartNoticeHoldSeconds = 1.3f;
        // The goal box sits just under the combat header and inside the board's left edge.
        private const float ObjectiveTopInset = -92f;
        private const float ObjectiveChipWidth = 520f;
        private const float ObjectiveChipHeight = 132f;
        private const float ObjectiveChipLeft = 48f;
        private const float ObjectiveTabWidth = 44f;
        // The docked goal stays open this long after the fight starts, then tucks away.
        private const float ObjectiveCollapseDelaySeconds = 3f;
        // Grace after the pointer leaves, so a diagonal move does not flap the panel.
        private const float ObjectiveHoverGraceSeconds = .35f;

        // Battlefield viewport geometry in the 1920x1080 reference canvas (top-left origin).
        private const float BoardViewportX = 16f;
        private const float BoardViewportY = 80f;
        private const float BoardViewportWidth = 1408f;
        private const float BoardViewportHeight = 768f;

        private ICombatEntrySequenceHost host;
        private Canvas canvas;
        private Canvas chipCanvas;
        private GameObject root;
        private CanvasGroup rootGroup;

        private GameObject objectiveChip;
        private RectTransform objectiveChipRect;
        private CanvasGroup objectiveChipGroup;
        private Text objectiveChipBody;
        private GameObject objectiveTab;
        private RectTransform objectiveTabRect;
        private CanvasGroup objectiveTabGroup;

        private GameObject revealCard;
        private CanvasGroup revealCardGroup;
        private RawImage revealPortrait;
        private Text revealName;
        private Text revealDetail;
        private GameObject revealFrame;
        private CanvasGroup revealFrameGroup;

        private GameObject startNotice;
        private CanvasGroup startNoticeGroup;

        private GameObject entryVeil;
        private CanvasGroup entryVeilGroup;
        private VeilStage veilStage;
        private float veilElapsed;
        private Action veilContinuation;

        private enum VeilStage { None, Hold, Fade }

        private bool blocking;
        private bool noticeVisible;
        private bool chipDocked;
        private bool chipCollapsed;
        private float chipCollapseAt;
        private float chipHoverHoldUntil;
        private bool uiBuilt;

        public bool IsBlockingInput => blocking;

        public void Initialize(ICombatEntrySequenceHost source)
        {
            host = source;
        }

        /// <summary>
        /// Runs the entry cinematic for the combat the host is currently presenting.  Returns
        /// false when there is nothing to play, in which case the caller keeps full control.
        /// </summary>
        public bool Play()
        {
            if (host == null || host.CurrentState == null || host.BattlefieldViewport == null) return false;
            UnitState hero = host.CurrentState.GetUnit("hero");
            if (hero == null) return false;

            EnsureUi();
            DOTween.Kill(this);
            blocking = true;
            noticeVisible = false;

            UiMotionProfile motion = Motion();
            float defaultCellSize = BattlefieldPresentationAdapter.DefaultCellSize(
                host.CurrentState.Map.Width, host.CurrentState.Map.Height);
            float revealZoom = ClampZoom(defaultCellSize * RevealZoomScale);
            float centerX = (host.CurrentState.Map.Width - 1) * .5f;
            float centerY = (host.CurrentState.Map.Height - 1) * .5f;
            IReadOnlyList<UnitState> targets = TourTargets(host.CurrentState);

            root.SetActive(true);
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;
            HideObjectiveChip();
            HideRevealCard(motion);
            HideStartNotice();

            if (motion.IsImmediate)
            {
                // Reduced-motion path: no veil, no tour. The player still gets the goal and the
                // start notice, so the flow never becomes skippable by accident.
                SetVeilAlpha(0f);
                ApplyPose(defaultCellSize, hero.Position.X, hero.Position.Y);
                DockObjective(motion);
                BeginStartNotice(motion);
                return true;
            }

            // The board opens at play scale, centered on the field, behind an opaque veil so the
            // fight never cuts in mid-frame.  There is deliberately no zoom-out or push-in.
            SetVeilAlpha(1f);
            ApplyPose(defaultCellSize, centerX, centerY);
            // The veil is driven by hand rather than by DOTween: it has to hold at full black
            // for a beat, and the frame where the fight is built is long enough that a tween
            // would spend most of the fade before anything was ever drawn.
            BeginVeilIntro(() => StartCinematic(hero, motion, targets, centerX, centerY, defaultCellSize, revealZoom));
            return true;
        }

        /// <summary>
        /// Plays the card and camera choreography that follows the fade.  It starts only once
        /// the veil has cleared, so no leg of it can be eaten by the opening hitch.
        /// </summary>
        private void StartCinematic(UnitState hero, UiMotionProfile motion, IReadOnlyList<UnitState> targets,
            float centerX, float centerY, float defaultCellSize, float revealZoom)
        {
            if (host == null || host.BattlefieldViewport == null) return;
            Sequence sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
            // The goal is the only thing shown over the opening look at the field.
            sequence.AppendCallback(() => DockObjective(motion));
            sequence.AppendInterval(FieldLookSeconds);
            // Every leg starts from the waypoint the camera is actually resting on; deriving the
            // start of each tween from the previous waypoint keeps the tour continuous instead of
            // snapping back to the hero between reveals.
            float poseX = centerX, poseY = centerY, poseZoom = defaultCellSize;

            foreach (UnitState enemy in targets)
            {
                sequence.AppendCallback(() => ShowRevealCard(enemy, motion));
                sequence.Append(PoseTween(poseX, poseY, poseZoom, enemy.Position.X, enemy.Position.Y, revealZoom,
                    TourApproachSeconds, FormalUiMotionTokens.StandardEase));
                poseX = enemy.Position.X; poseY = enemy.Position.Y; poseZoom = revealZoom;
                sequence.AppendInterval(TourHoldSeconds);
                sequence.AppendCallback(() => HideRevealCard(motion));
                sequence.AppendInterval(TourExitSeconds);
            }

            sequence.Append(PoseTween(poseX, poseY, poseZoom, hero.Position.X, hero.Position.Y, defaultCellSize,
                ReturnToHeroSeconds, FormalUiMotionTokens.StandardEase));
            sequence.AppendCallback(() =>
            {
                // Land on the exact interactive pose so handing control back never snaps.
                host.BattlefieldViewport.Focus(hero.Position);
                DockObjective(motion);
            });
            sequence.AppendInterval(motion.StandardDuration);
            sequence.AppendCallback(() => BeginStartNotice(motion));
        }

        /// <summary>Abandons the cinematic and goes straight to the start notice.</summary>
        public void SkipToStart()
        {
            if (!blocking || noticeVisible) return;
            UnitState hero = host == null || host.CurrentState == null ? null : host.CurrentState.GetUnit("hero");
            if (hero == null || host.BattlefieldViewport == null) { Finish(); return; }
            DOTween.Kill(this);
            UiMotionProfile motion = Motion();
            float defaultCellSize = BattlefieldPresentationAdapter.DefaultCellSize(
                host.CurrentState.Map.Width, host.CurrentState.Map.Height);
            CancelVeilIntro();
            SetVeilAlpha(0f);
            ApplyPose(defaultCellSize, hero.Position.X, hero.Position.Y);
            host.BattlefieldViewport.Focus(hero.Position);
            HideRevealCard(motion);
            DockObjective(motion);
            BeginStartNotice(motion);
        }

        /// <summary>Ends the entry sequence and returns control to the battle.</summary>
        public void Finish()
        {
            if (!blocking && !noticeVisible) return;
            DOTween.Kill(this);
            CancelVeilIntro();
            blocking = false;
            noticeVisible = false;
            if (startNoticeGroup != null) startNoticeGroup.DOKill();
            if (root != null) root.SetActive(false);
            // The docked goal lingers open for a beat, then tucks itself away.
            chipCollapseAt = Time.unscaledTime + ObjectiveCollapseDelaySeconds;
            host?.CompleteCombatEntrySequence();
        }

        private void Update()
        {
            if (host == null) return;
            AdvanceVeilIntro();
            UpdateObjectiveChip();
            if (!blocking) return;
            if (!host.IsDeveloperCombatActive) { Finish(); return; }

            Keyboard keyboard = Keyboard.current;
            bool confirm = keyboard != null &&
                (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame ||
                 keyboard.numpadEnterKey.wasPressedThisFrame);
            bool skip = confirm || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
            if (!skip) return;
            // During the notice the battle is already handed over; any key just ends the beat.
            if (noticeVisible) Finish();
            else SkipToStart();
        }

        private UiMotionProfile Motion()
            => UiMotionProfile.FromIntensity(host == null ? 1f : host.UiPreferences.AnimationIntensity);

        private static float ClampZoom(float value) => Mathf.Max(BattlefieldPresentationAdapter.MinimumCellSize,
            Mathf.Min(BattlefieldPresentationAdapter.MaximumCellSize, value));

        /// <summary>Enemies are toured from the far edge inward, matching how the board reads.</summary>
        private static IReadOnlyList<UnitState> TourTargets(CombatState state) => state.Units.Values
            .Where(unit => !unit.IsHero && unit.IsAlive)
            .OrderByDescending(unit => unit.Position.Y).ThenBy(unit => unit.Position.X).ToArray();

        private Tween PoseTween(float fromX, float fromY, float fromZoom, float toX, float toY, float toZoom,
            float duration, Ease ease)
        {
            return DOTween.To(progress =>
                {
                    if (host == null || host.BattlefieldViewport == null) return;
                    host.BattlefieldViewport.ApplyCinematicPose(
                        Mathf.Lerp(fromZoom, toZoom, progress),
                        Mathf.Lerp(fromX, toX, progress),
                        Mathf.Lerp(fromY, toY, progress));
                }, 0f, 1f, duration)
                .SetEase(ease).SetUpdate(true).SetTarget(this);
        }

        private void ApplyPose(float zoom, float gridX, float gridY)
        {
            if (host == null || host.BattlefieldViewport == null) return;
            host.BattlefieldViewport.ApplyCinematicPose(zoom, gridX, gridY);
        }

        private void EnsureUi()
        {
            if (uiBuilt || root != null) return;
            // Two canvases: the cinematic overlay is torn down when the fight starts, while the
            // docked objective chip must survive it and stay readable over the board.
            chipCanvas = FormalUiKit.CanvasRoot("战斗目标角标",
                UiLayoutContract.InteractionSortingOrder + Mathf.RoundToInt(OverlaySortingOffset) - 1);
            canvas = FormalUiKit.CanvasRoot("战斗入场演出",
                UiLayoutContract.InteractionSortingOrder + Mathf.RoundToInt(OverlaySortingOffset));
            root = canvas.gameObject;
            rootGroup = root.GetComponent<CanvasGroup>();
            if (rootGroup == null) rootGroup = root.AddComponent<CanvasGroup>();
            // The overlay swallows battlefield input for the whole cinematic; the canvas root
            // rect covers the screen, so a clear full-screen graphic is the shield.
            Image shield = root.GetComponent<Image>();
            if (shield == null) shield = root.AddComponent<Image>();
            shield.sprite = null; shield.color = Color.clear; shield.raycastTarget = true;

            BuildObjectiveChip(chipCanvas.transform);
            BuildObjectiveTab(chipCanvas.transform);
            BuildRevealCard(root.transform);
            revealFrame = BuildRevealFrame(root.transform);
            BuildStartNotice(root.transform);
            // Added last so it draws over every other entry element: the black is what hides
            // the cut into the battle, and it must cover the goal card and notice too.
            BuildEntryVeil(root.transform);
            uiBuilt = true;
            root.SetActive(false);
            chipCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Full-screen black used to fade the battle in.  It carries no raycast target of its
        /// own; the overlay's shield already swallows input for the whole cinematic.
        /// </summary>
        private void BuildEntryVeil(Transform parent)
        {
            entryVeil = FormalUiKit.Create("入场黑幕", parent);
            RectTransform rect = entryVeil.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = entryVeil.AddComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.black;
            image.raycastTarget = false;
            entryVeilGroup = entryVeil.AddComponent<CanvasGroup>();
            entryVeilGroup.alpha = 1f;
        }

        private void SetVeilAlpha(float alpha)
        {
            if (entryVeilGroup == null) return;
            entryVeilGroup.DOKill();
            entryVeilGroup.alpha = Mathf.Clamp01(alpha);
        }

        private void BeginVeilIntro(Action onCleared)
        {
            veilStage = VeilStage.Hold;
            veilElapsed = 0f;
            veilContinuation = onCleared;
            SetVeilAlpha(1f);
        }

        private void CancelVeilIntro()
        {
            veilStage = VeilStage.None;
            veilElapsed = 0f;
            veilContinuation = null;
        }

        /// <summary>
        /// Holds full black, then lifts it with the same ease the rest of the UI uses.
        /// Driven here rather than by a tween so the opening hitch cannot skip it.
        /// </summary>
        private void AdvanceVeilIntro()
        {
            if (veilStage == VeilStage.None || entryVeilGroup == null) return;
            veilElapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFrameStepSeconds);
            if (veilStage == VeilStage.Hold)
            {
                entryVeilGroup.alpha = 1f;
                if (veilElapsed < VeilHoldSeconds) return;
                veilElapsed = 0f;
                veilStage = VeilStage.Fade;
                return;
            }
            float progress = Mathf.Clamp01(veilElapsed / VeilFadeSeconds);
            entryVeilGroup.alpha = 1f - (1f - Mathf.Pow(1f - progress, 3f));
            if (progress < 1f) return;
            entryVeilGroup.alpha = 0f;
            veilStage = VeilStage.None;
            Action continuation = veilContinuation;
            veilContinuation = null;
            if (continuation != null) continuation();
        }

        /// <summary>
        /// Builds a battle-style card using the same treatment as the HUD modules: a solid
        /// raised-surface fill with a hairline rule frame and no skin overlay.  The entry UI is
        /// part of the battle screen, so it must not use the dark ink that full-screen cinematic
        /// veils use.
        /// </summary>
        private static GameObject BattleCard(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            GameObject card = FormalUiKit.AnchoredPanel(name, parent, anchor, pivot, position, size,
                FormalUiTheme.SurfaceRaised);
            Image surface = card.GetComponent<Image>();
            if (surface != null)
            {
                surface.sprite = null;
                surface.type = Image.Type.Simple;
                surface.color = FormalUiTheme.SurfaceRaised;
                Image skin = FormalUiKit.SkinOverlay(surface);
                if (skin != null) skin.gameObject.SetActive(false);
            }
            FormalUiKit.ThinFrame(card.transform, size, FormalUiTheme.Rule, "卡片框");
            return card;
        }

        private void BuildObjectiveChip(Transform parent)
        {
            objectiveChip = BattleCard("关卡目标角标", parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(ObjectiveChipLeft, ObjectiveTopInset), new Vector2(ObjectiveChipWidth, ObjectiveChipHeight));
            objectiveChipRect = objectiveChip.GetComponent<RectTransform>();
            objectiveChipGroup = objectiveChip.AddComponent<CanvasGroup>();
            // The chip is purely informational: it must never swallow board clicks underneath.
            // Pointer detection for the slide-out is done by rectangle test instead of raycast.
            Image chipSurface = objectiveChip.GetComponent<Image>();
            if (chipSurface != null) chipSurface.raycastTarget = false;
            FormalUiKit.Line(objectiveChip.transform, new Vector2(18f, -18f), new Vector2(4f, 40f), FormalUiTheme.Cyan, "目标色条");
            FormalUiKit.Label("标题", "本关目标", objectiveChip.transform, new Vector2(36f, -14f), new Vector2(260f, 40f),
                FormalUiTheme.BodyFontSize, FormalUiTheme.Cyan, TextAnchor.MiddleLeft);
            objectiveChipBody = FormalUiKit.Label("内容", string.Empty, objectiveChip.transform,
                new Vector2(36f, -58f), new Vector2(ObjectiveChipWidth - 60f, ObjectiveChipHeight - 74f),
                FormalUiTheme.BodyFontSize, FormalUiTheme.Text, TextAnchor.UpperLeft);
            objectiveChipBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            objectiveChipBody.verticalOverflow = VerticalWrapMode.Truncate;
        }

        /// <summary>The peek of the tucked-away goal: the edge the pointer has to reach.</summary>
        private void BuildObjectiveTab(Transform parent)
        {
            objectiveTab = BattleCard("关卡目标页签", parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, ObjectiveTopInset), new Vector2(ObjectiveTabWidth, ObjectiveChipHeight));
            objectiveTabRect = objectiveTab.GetComponent<RectTransform>();
            objectiveTabGroup = objectiveTab.AddComponent<CanvasGroup>();
            DisableRaycast(objectiveTab);
            // Explicit break so the tab reads vertically whatever the font metrics do.
            Text caption = FormalUiKit.Label("页签文字", "本关\n目标", objectiveTab.transform,
                new Vector2(0f, -22f), new Vector2(ObjectiveTabWidth, 96f),
                FormalUiTheme.BodyFontSize, FormalUiTheme.Cyan, TextAnchor.UpperCenter);
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            caption.verticalOverflow = VerticalWrapMode.Overflow;
            objectiveTabGroup.alpha = 0f;
            objectiveTab.SetActive(false);
        }

        private static void DisableRaycast(GameObject target)
        {
            Image image = target == null ? null : target.GetComponent<Image>();
            if (image != null) image.raycastTarget = false;
        }

        /// <summary>
        /// Grows the objective panel to the wrapped height of its goal text.  Objectives vary a
        /// lot in length, and a fixed-height box would either clip the goal or leave dead space.
        /// <see cref="Text.preferredHeight"/> reports only the tight glyph box, which this pixel
        /// font draws smaller than its real line slot, so the line count drives the same slot
        /// rule the rest of the HUD uses.
        /// </summary>
        private static void FitObjectivePanel(Text body, RectTransform panel, float bodyTop, float bottomPadding, int maxLines)
        {
            if (body == null || panel == null) return;
            float width = Mathf.Max(1f, body.rectTransform.sizeDelta.x);
            int lines = Mathf.Clamp(Mathf.CeilToInt(body.preferredWidth / width), 1, Mathf.Max(1, maxLines));
            float height = FormalUiTheme.MinimumTextSlotHeight(body.fontSize) + (lines - 1) * 32f;
            body.rectTransform.sizeDelta = new Vector2(width, height);
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, bodyTop + height + bottomPadding);
        }

        private void BuildRevealCard(Transform parent)
        {
            revealCard = BattleCard("敌方情报卡", parent, new Vector2(0f, 1f), new Vector2(.5f, .5f),
                new Vector2(BoardViewportX + BoardViewportWidth * .5f, -(BoardViewportY + BoardViewportHeight - 92f)),
                new Vector2(620f, 156f));
            revealCardGroup = revealCard.AddComponent<CanvasGroup>();

            GameObject portraitObject = FormalUiKit.Create("敌方立绘", revealCard.transform);
            RectTransform portraitRect = portraitObject.AddComponent<RectTransform>();
            portraitRect.anchorMin = portraitRect.anchorMax = portraitRect.pivot = new Vector2(0f, 1f);
            portraitRect.anchoredPosition = new Vector2(14f, -14f);
            portraitRect.sizeDelta = new Vector2(128f, 128f);
            revealPortrait = portraitObject.AddComponent<RawImage>();
            revealPortrait.raycastTarget = false;

            revealName = FormalUiKit.Label("敌方名称", string.Empty, revealCard.transform, new Vector2(160f, -22f),
                new Vector2(440f, 40f), FormalUiTheme.HeadingFontSize, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            revealDetail = FormalUiKit.Label("敌方状态", string.Empty, revealCard.transform, new Vector2(160f, -70f),
                new Vector2(440f, 72f), FormalUiTheme.BodyFontSize, FormalUiTheme.Muted, TextAnchor.UpperLeft);
            revealDetail.horizontalOverflow = HorizontalWrapMode.Wrap;
            revealDetail.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private GameObject BuildRevealFrame(Transform parent)
        {
            // The camera centers the toured cell in the viewport, so a fixed frame at the
            // viewport center always lands on the enemy being introduced.
            GameObject frame = FormalUiKit.Create("敌方锁定框", parent);
            RectTransform rect = frame.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(BoardViewportX + BoardViewportWidth * .5f,
                -(BoardViewportY + BoardViewportHeight * .5f));
            rect.sizeDelta = new Vector2(RevealFrameSize, RevealFrameSize);
            CanvasGroup group = frame.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            revealFrameGroup = group;
            // A focus reticle, so it uses the battle accent rather than a status colour.
            FormalUiKit.ThinFrame(frame.transform, new Vector2(RevealFrameSize, RevealFrameSize),
                FormalUiTheme.Cyan, "锁定框");
            frame.SetActive(false);
            return frame;
        }

        /// <summary>
        /// The battle-start notification.  It replaces a confirm dialog: the fight is already
        /// live behind it, so it only announces the handover and then gets out of the way.
        /// </summary>
        private void BuildStartNotice(Transform parent)
        {
            startNotice = BattleCard("任务开始提示", parent, new Vector2(0f, 1f), new Vector2(.5f, .5f),
                new Vector2(BoardViewportX + BoardViewportWidth * .5f, -(BoardViewportY + BoardViewportHeight * .5f)),
                new Vector2(520f, 148f));
            startNoticeGroup = startNotice.AddComponent<CanvasGroup>();
            DisableRaycast(startNotice);
            FormalUiKit.Line(startNotice.transform, new Vector2(40f, -22f), new Vector2(440f, 4f), FormalUiTheme.Cyan, "上线");
            FormalUiKit.Label("标题", "任务开始", startNotice.transform, new Vector2(0f, -46f), new Vector2(520f, 72f),
                FormalUiTheme.TitleFontSize, FormalUiTheme.Cyan, TextAnchor.MiddleCenter);
            FormalUiKit.Line(startNotice.transform, new Vector2(40f, -128f), new Vector2(440f, 4f), FormalUiTheme.Cyan, "下线");
        }

        private void DockObjective(UiMotionProfile motion)
        {
            objectiveChipBody.text = host?.CombatObjectiveSummary ?? string.Empty;
            FitObjectivePanel(objectiveChipBody, objectiveChipRect, 58f, 14f, maxLines: 4);
            chipDocked = true;
            chipCollapsed = false;
            // Not before Finish(); the countdown to tucking the goal away starts with the fight.
            chipCollapseAt = float.PositiveInfinity;
            chipHoverHoldUntil = 0f;
            objectiveChipRect.anchoredPosition = new Vector2(ObjectiveChipLeft, ObjectiveTopInset);
            if (objectiveTabGroup != null) objectiveTabGroup.alpha = 0f;
            if (objectiveTab != null) objectiveTab.SetActive(false);
            if (chipCanvas != null) chipCanvas.gameObject.SetActive(true);
            objectiveChip.SetActive(true);
            objectiveChipGroup.DOKill();
            if (motion.IsImmediate)
            {
                objectiveChipGroup.alpha = 1f;
                return;
            }
            objectiveChipGroup.alpha = 0f;
            DOTween.To(() => objectiveChipGroup.alpha, value => objectiveChipGroup.alpha = value, 1f, motion.StandardDuration)
                .SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true).SetTarget(objectiveChipGroup);
        }

        private void HideObjectiveChip()
        {
            if (objectiveChip == null) return;
            chipDocked = false;
            chipCollapsed = false;
            chipCollapseAt = float.PositiveInfinity;
            objectiveChipGroup.DOKill();
            objectiveChipGroup.alpha = 0f;
            objectiveChip.SetActive(false);
            if (objectiveTabGroup != null) objectiveTabGroup.DOKill();
            if (objectiveTab != null) objectiveTab.SetActive(false);
            if (chipCanvas != null) chipCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Drives the docked goal's tuck-away: it hides itself a few seconds into the fight and
        /// slides back out whenever the pointer reaches either the panel or its edge tab.
        /// Pointer state is sampled by rectangle test so the chip keeps swallowing no board clicks.
        /// </summary>
        private void UpdateObjectiveChip()
        {
            if (objectiveChip == null) return;
            bool visible = chipDocked && host.IsDeveloperCombatActive;
            if (objectiveChip.activeSelf != visible) objectiveChip.SetActive(visible);
            // The tab doubles as the hover region while the panel is tucked away, so it stays
            // active for as long as the goal does; its alpha is what makes it visible or not.
            if (objectiveTab != null && objectiveTab.activeSelf != visible) objectiveTab.SetActive(visible);
            if (!visible) { chipCollapsed = false; return; }

            if (PointerOverObjective())
            {
                chipHoverHoldUntil = Time.unscaledTime + ObjectiveHoverGraceSeconds;
                SetObjectiveCollapsed(false);
                return;
            }
            if (Time.unscaledTime < chipCollapseAt) return;
            if (Time.unscaledTime < chipHoverHoldUntil) return;
            SetObjectiveCollapsed(true);
        }

        private bool PointerOverObjective()
        {
            if (chipCanvas == null) return false;
            Mouse mouse = Mouse.current;
            if (mouse == null) return false;
            Vector2 screen = mouse.position.ReadValue();
            Camera camera = chipCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : chipCanvas.worldCamera;
            if (objectiveTabRect != null && objectiveTab != null && objectiveTab.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(objectiveTabRect, screen, camera)) return true;
            if (!chipCollapsed && objectiveChipRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(objectiveChipRect, screen, camera)) return true;
            return false;
        }

        private void SetObjectiveCollapsed(bool collapsed)
        {
            if (chipCollapsed == collapsed) return;
            chipCollapsed = collapsed;
            if (objectiveChipRect == null) return;
            UiMotionProfile motion = Motion();
            float targetX = collapsed ? -ObjectiveChipWidth : ObjectiveChipLeft;
            float chipY = objectiveChipRect.anchoredPosition.y;
            objectiveChipRect.DOKill();
            if (objectiveTabGroup != null) objectiveTabGroup.DOKill();
            if (objectiveTab != null) objectiveTab.SetActive(true);
            if (motion.IsImmediate)
            {
                objectiveChipRect.anchoredPosition = new Vector2(targetX, chipY);
                if (objectiveTabGroup != null) objectiveTabGroup.alpha = collapsed ? 1f : 0f;
                return;
            }
            DOTween.To(() => objectiveChipRect.anchoredPosition.x, value =>
                    objectiveChipRect.anchoredPosition = new Vector2(value, chipY), targetX, motion.StandardDuration)
                .SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true).SetTarget(objectiveChipRect);
            if (objectiveTabGroup == null) return;
            DOTween.To(() => objectiveTabGroup.alpha, value => objectiveTabGroup.alpha = value,
                    collapsed ? 1f : 0f, motion.QuickDuration)
                .SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true).SetTarget(objectiveTabGroup);
        }

        private void ShowRevealCard(UnitState enemy, UiMotionProfile motion)
        {
            if (enemy == null || revealCard == null) return;
            Texture2D portrait = host?.UnitPortrait(enemy);
            revealPortrait.texture = portrait;
            if (portrait != null) portrait.filterMode = FilterMode.Point;
            revealPortrait.uvRect = new Rect(0f, 0f, 1f, 1f);
            revealName.text = string.IsNullOrEmpty(enemy.DisplayName) ? enemy.Id : enemy.DisplayName;
            revealDetail.text = "生命 " + enemy.Health + " / " + enemy.MaxHealth +
                "　护盾 " + enemy.Shield + "\n" + EnemyResolutionSemantics.Forecast(enemy);
            revealCard.SetActive(true);
            revealFrame.SetActive(true);
            revealCardGroup.DOKill();
            revealFrameGroup.DOKill();
            if (motion.IsImmediate)
            {
                revealCardGroup.alpha = 1f;
                revealFrameGroup.alpha = 1f;
                return;
            }
            revealCardGroup.alpha = 0f;
            DOTween.To(() => revealCardGroup.alpha, value => revealCardGroup.alpha = value, 1f, motion.QuickDuration)
                .SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true).SetTarget(revealCardGroup);
            revealFrameGroup.alpha = 0f;
            DOTween.To(() => revealFrameGroup.alpha, value => revealFrameGroup.alpha = value, 1f, motion.QuickDuration)
                .SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true).SetTarget(revealFrameGroup);
        }

        private void HideRevealCard(UiMotionProfile motion)
        {
            if (revealCard == null) return;
            revealCardGroup.DOKill();
            revealFrameGroup.DOKill();
            if (motion.IsImmediate)
            {
                revealCardGroup.alpha = 0f;
                revealFrameGroup.alpha = 0f;
                revealCard.SetActive(false);
                revealFrame.SetActive(false);
                return;
            }
            DOTween.To(() => revealCardGroup.alpha, value => revealCardGroup.alpha = value, 0f, motion.QuickDuration)
                .SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true).SetTarget(revealCardGroup)
                .OnComplete(() => { if (revealCard != null) revealCard.SetActive(false); });
            DOTween.To(() => revealFrameGroup.alpha, value => revealFrameGroup.alpha = value, 0f, motion.QuickDuration)
                .SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true).SetTarget(revealFrameGroup)
                .OnComplete(() => { if (revealFrame != null) revealFrame.SetActive(false); });
        }

        /// <summary>
        /// Floats the "mission start" notice and hands control back once it has been read.  The
        /// tail is owned here rather than by the cinematic sequence so that skipping straight to
        /// the notice still ends the same way.
        /// </summary>
        private void BeginStartNotice(UiMotionProfile motion)
        {
            noticeVisible = true;
            float total = NoticeSeconds(motion);
            ShowStartNotice(motion);
            DOTween.Sequence().SetTarget(this).SetUpdate(true)
                .AppendInterval(total)
                .AppendCallback(Finish);
        }

        private static float NoticeSeconds(UiMotionProfile motion)
            => motion.IsImmediate ? .6f : motion.StandardDuration * 2f + StartNoticeHoldSeconds;

        private void ShowStartNotice(UiMotionProfile motion)
        {
            if (startNotice == null) return;
            startNotice.SetActive(true);
            startNoticeGroup.DOKill();
            if (motion.IsImmediate) { startNoticeGroup.alpha = 1f; return; }
            startNoticeGroup.alpha = 0f;
            DOTween.Sequence().SetTarget(startNoticeGroup).SetUpdate(true)
                .Append(DOTween.To(() => startNoticeGroup.alpha, value => startNoticeGroup.alpha = value, 1f,
                    motion.StandardDuration).SetEase(FormalUiMotionTokens.StandardEase))
                .AppendInterval(StartNoticeHoldSeconds)
                .Append(DOTween.To(() => startNoticeGroup.alpha, value => startNoticeGroup.alpha = value, 0f,
                    motion.StandardDuration).SetEase(FormalUiMotionTokens.FeedbackEase));
        }

        private void HideStartNotice()
        {
            if (startNotice == null) return;
            startNoticeGroup.DOKill();
            startNoticeGroup.alpha = 0f;
            startNotice.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            // CanvasRoot returns a scene-authored anchor when one exists; that anchor has a
            // parent and must survive. Only a canvas we created ourselves is childless.
            if (canvas != null && canvas.transform.parent == null) Destroy(canvas.gameObject);
            if (chipCanvas != null && chipCanvas.transform.parent == null) Destroy(chipCanvas.gameObject);
        }
    }
}
