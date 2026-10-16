using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>Draws the approved square range language in one mesh per logical battlefield cell.</summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class CombatBattlefieldRangeGraphic : MaskableGraphic
    {
        public const byte Top = 1, Right = 2, Bottom = 4, Left = 8;

        private BattlefieldCellMarker move, attack, skill;
        private bool enemyRange, enemyEffect, playerEffect, enemyRoute, enemyDestination;
        private byte enemyEdges, playerEdges;
        private bool focused, animations = true;
        private float appearedAt, intentAge = CombatIntentChangeTracker.Duration, resultAt = -10f;
        private bool accepted;
        private int lastAnimationFrame = -1;
        private const float EnterDuration = .16f, ExitDuration = .12f;
        private MarkerState departing;
        private float departingOpacity;
        private float meshOpacity = 1f;
        private bool transitionPending;
        private bool transientWasVisible;

        private struct MarkerState
        {
            public BattlefieldCellMarker Move, Attack, Skill;
            public bool EnemyRange, EnemyEffect, PlayerEffect, EnemyRoute, EnemyDestination, Focused;
            public byte EnemyEdges, PlayerEdges;
        }

        private MarkerState CurrentMarkers => new MarkerState
        {
            Move = move, Attack = attack, Skill = skill, EnemyRange = enemyRange,
            EnemyEffect = enemyEffect, PlayerEffect = playerEffect, EnemyRoute = enemyRoute,
            EnemyDestination = enemyDestination, EnemyEdges = enemyEdges, PlayerEdges = playerEdges,
            Focused = focused
        };

        public void Refresh(BattlefieldCellMarker moveMarker, BattlefieldCellMarker attackMarker,
            BattlefieldCellMarker skillMarker, bool possibleEnemyAttack, bool actualEnemyEffect,
            byte actualEnemyEdges, bool actualPlayerEffect, byte actualPlayerEdges,
            bool route, bool destination, bool focus = false, float animationIntensity = 1f,
            float changedIntentAge = CombatIntentChangeTracker.Duration)
        {
            raycastTarget = false;
            if (animations != (animationIntensity > .01f)) SetVerticesDirty();
            animations = animationIntensity > .01f;
            intentAge = changedIntentAge;
            if (move == moveMarker && attack == attackMarker && skill == skillMarker &&
                enemyRange == possibleEnemyAttack && enemyEffect == actualEnemyEffect &&
                enemyEdges == actualEnemyEdges && playerEffect == actualPlayerEffect &&
                playerEdges == actualPlayerEdges && enemyRoute == route && enemyDestination == destination && focused == focus) return;
            // Keep the old geometry until its exit completes. Rapid changes retain
            // only one outgoing snapshot, so cancelled previews cannot accumulate.
            departing = CurrentMarkers;
            departingOpacity = animations ? Mathf.Clamp01((Time.unscaledTime - appearedAt) / EnterDuration) : 0f;
            appearedAt = Time.unscaledTime;
            transitionPending = animations;
            focused = focus;
            move = moveMarker; attack = attackMarker; skill = skillMarker;
            enemyRange = possibleEnemyAttack; enemyEffect = actualEnemyEffect;
            enemyEdges = actualEnemyEdges; playerEffect = actualPlayerEffect;
            playerEdges = actualPlayerEdges; enemyRoute = route; enemyDestination = destination;
            SetVerticesDirty();
        }

        public void ShowResult(bool success) { accepted = success; resultAt = Time.unscaledTime; SetVerticesDirty(); }

        private void Update()
        {
            bool marked = focused || move != BattlefieldCellMarker.None || attack != BattlefieldCellMarker.None ||
                skill != BattlefieldCellMarker.None || enemyRange || enemyEffect || enemyRoute || enemyDestination;
            int frame = Mathf.FloorToInt(Time.unscaledTime * 15f);
            if (frame == lastAnimationFrame) return;
            lastAnimationFrame = frame;
            float age = Time.unscaledTime - appearedAt;

            Vector3 cellPosition = transform.parent != null ? transform.parent.localPosition : transform.localPosition;
            float phase = cellPosition.x * .011f + cellPosition.y * .017f;
            float pulse = animations && marked ? .955f + .045f * Mathf.Sin(Time.unscaledTime * 3.5f + phase) : 1f;
            canvasRenderer.SetAlpha(pulse);
            bool transientVisible = intentAge < CombatIntentChangeTracker.Duration || Time.unscaledTime - resultAt < .40f;
            if (transitionPending || focused || transientVisible || transientWasVisible) SetVerticesDirty();
            transientWasVisible = transientVisible;
            if (age >= EnterDuration || !animations) transitionPending = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable(); resultAt = -10f; canvasRenderer.SetAlpha(1f);
            departing = default; departingOpacity = 0f;
            transitionPending = false;
            transientWasVisible = false;
            move = attack = skill = BattlefieldCellMarker.None;
            enemyRange = enemyEffect = playerEffect = enemyRoute = enemyDestination = focused = false;
            enemyEdges = playerEdges = 0; lastAnimationFrame = -1;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float age = Time.unscaledTime - appearedAt;
            if (animations && age < ExitDuration)
                DrawMarkers(vh, departing, departingOpacity * (1f - Mathf.Clamp01(age / ExitDuration)));
            DrawMarkers(vh, CurrentMarkers, animations ? Mathf.Clamp01(age / EnterDuration) : 1f);
            meshOpacity = 1f;
            if (intentAge < CombatIntentChangeTracker.Duration)
            {
                byte alpha = animations ? (Mathf.FloorToInt(intentAge / .12f) % 2 == 0 ? (byte)255 : (byte)100) : (byte)200;
                Border(vh, 1, 1, new Color32(255, 196, 94, alpha));
            }
            float resultAge = Time.unscaledTime - resultAt;
            if (resultAge < .40f)
            {
                byte alpha = (byte)(255f * (1f - Mathf.Clamp01(resultAge / .40f)));
                Corners(vh, 1, accepted ? new Color32(198, 255, 226, alpha) : new Color32(255, 105, 86, alpha));
            }
        }

        private void DrawMarkers(VertexHelper vh, MarkerState state, float opacity)
        {
            if (opacity <= 0f) return;
            meshOpacity = opacity;
            var move = state.Move; var attack = state.Attack; var skill = state.Skill;
            bool enemyRange = state.EnemyRange, enemyEffect = state.EnemyEffect, playerEffect = state.PlayerEffect;
            bool enemyRoute = state.EnemyRoute, enemyDestination = state.EnemyDestination, focused = state.Focused;
            byte enemyEdges = state.EnemyEdges, playerEdges = state.PlayerEdges;
            if (enemyEffect) Box(vh, 0, 0, 32, 32, new Color32(192, 68, 59, 133));
            bool moving = move != BattlefieldCellMarker.None;
            bool selecting = attack != BattlefieldCellMarker.None || skill != BattlefieldCellMarker.None;
            if (enemyRange)
            {
                if (!moving) Box(vh, 2, 2, 28, 28, new Color32(233, 184, 90, 36));
                Border(vh, moving ? 0 : 2, 1,
                    new Color32(moving ? (byte)189 : (byte)164, moving ? (byte)118 : (byte)106,
                        moving ? (byte)43 : (byte)36, 235));
            }
            if (moving)
            {
                int inset = enemyRange ? 6 : 3;
                Box(vh, inset, inset, 32 - inset * 2, 32 - inset * 2,
                    new Color32(80, 198, 209, enemyRange ? (byte)84 : (byte)46));
                Border(vh, inset, 1, new Color32(enemyRange ? (byte)39 : (byte)52,
                    enemyRange ? (byte)142 : (byte)142, enemyRange ? (byte)154 : (byte)151, 240));
                if (move == BattlefieldCellMarker.MovementRisk)
                    Box(vh, 13, 13, 6, 6, new Color32(231, 83, 73, 220));
            }
            if (selecting && !playerEffect)
            {
                bool target = attack == BattlefieldCellMarker.AttackTarget || skill == BattlefieldCellMarker.SkillRisk;
                Box(vh, 3, 3, 26, 26, new Color32(32, 91, 66, target ? (byte)80 : (byte)61));
                Border(vh, 3, 1, new Color32(target ? (byte)67 : (byte)46,
                    target ? (byte)142 : (byte)118, target ? (byte)103 : (byte)84, 235));
            }
            if (playerEffect) Box(vh, 0, 0, 32, 32, new Color32(40, 108, 78, 170));
            if (skill == BattlefieldCellMarker.SkillRisk)
                Box(vh, 13, 13, 6, 6, new Color32(231, 83, 73, 220));
            if (enemyEdges != 0) Edges(vh, 0, 1, enemyEdges, new Color32(231, 84, 73, 255));
            if (playerEdges != 0) Edges(vh, 0, 1, playerEdges, new Color32(65, 137, 96, 255));
            if (enemyRoute)
            {
                Box(vh, 12, 12, 8, 8, new Color32(91, 50, 23, 255));
                Box(vh, 13, 13, 6, 6, new Color32(237, 161, 75, 255));
            }
            if (enemyDestination)
            {
                Box(vh, 2, 2, 28, 28, new Color32(240, 166, 75, 54));
                Color32 corner = new Color32(164, 87, 30, 255);
                Box(vh, 2, 29, 10, 1, corner); Box(vh, 2, 20, 1, 10, corner);
                Box(vh, 20, 29, 10, 1, corner); Box(vh, 29, 20, 1, 10, corner);
                Box(vh, 2, 2, 10, 1, corner); Box(vh, 2, 2, 1, 10, corner);
                Box(vh, 20, 2, 10, 1, corner); Box(vh, 29, 2, 1, 10, corner);
                Box(vh, 11, 11, 10, 10, new Color32(113, 59, 26, 255));
                Box(vh, 12, 12, 8, 8, new Color32(255, 193, 107, 255));
            }
            if (focused)
            {
                int inset = animations && Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0 ? 2 : 3;
                Color32 tint = new Color32(234, 255, 255, 255);
                Corners(vh, inset, tint);
            }
        }

        private void Corners(VertexHelper vh, int inset, Color32 tint)
        {
            int far = 31 - inset;
            Box(vh, inset, inset, 7, 1, tint); Box(vh, inset, inset, 1, 7, tint);
            Box(vh, far - 6, inset, 7, 1, tint); Box(vh, far, inset, 1, 7, tint);
            Box(vh, inset, far, 7, 1, tint); Box(vh, inset, far - 6, 1, 7, tint);
            Box(vh, far - 6, far, 7, 1, tint); Box(vh, far, far - 6, 1, 7, tint);
        }

        private void Border(VertexHelper vh, int inset, int thickness, Color32 tint) =>
            Edges(vh, inset, thickness, Top | Right | Bottom | Left, tint);

        private void Edges(VertexHelper vh, int inset, int thickness, byte sides, Color32 tint)
        {
            int size = 32 - inset * 2;
            if ((sides & Top) != 0) Box(vh, inset, 32 - inset - thickness, size, thickness, tint);
            if ((sides & Right) != 0) Box(vh, 32 - inset - thickness, inset, thickness, size, tint);
            if ((sides & Bottom) != 0) Box(vh, inset, inset, size, thickness, tint);
            if ((sides & Left) != 0) Box(vh, inset, inset, thickness, size, tint);
        }

        private void Box(VertexHelper vh, float x, float y, float width, float height, Color32 tint)
        {
            tint.a = (byte)Mathf.RoundToInt(tint.a * meshOpacity * .70f);
            Rect rect = rectTransform.rect;
            float sx = rect.width / 32f, sy = rect.height / 32f;
            float left = rect.xMin + x * sx, bottom = rect.yMin + y * sy;
            float right = left + width * sx, top = bottom + height * sy;
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), tint, Vector2.zero);
            vh.AddVert(new Vector3(left, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, bottom), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
