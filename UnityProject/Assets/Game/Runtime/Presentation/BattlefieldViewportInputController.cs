using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class BattlefieldViewportInputController
    {
        private bool primaryPanning;
        private bool sideButtonPanning;
        private bool spaceHeld;

        public bool IsPrimaryPanning => primaryPanning;
        public bool IsSideButtonPanning => sideButtonPanning;
        public bool IsSpaceHeld => spaceHeld;

        public void HandleGuiEvent(Event input, BattlefieldViewport viewport, GridPosition focusTarget)
        {
            if (input == null || viewport == null) return;
            if (input.type == EventType.KeyDown && input.keyCode == KeyCode.Space) spaceHeld = true;
            if (input.type == EventType.KeyUp && input.keyCode == KeyCode.Space) spaceHeld = false;
            if (input.type == EventType.KeyDown && input.keyCode == KeyCode.Home)
            {
                viewport.Focus(focusTarget);
                input.Use();
                return;
            }
            if (input.type == EventType.MouseUp && primaryPanning)
            {
                primaryPanning = false;
                input.Use();
                return;
            }

            BattlefieldRect bounds = viewport.ViewportRect;
            if (!bounds.Contains(input.mousePosition.x, input.mousePosition.y)) return;
            if (IsSidePanButton(input.button) &&
                (input.type == EventType.MouseDown || input.type == EventType.MouseDrag || input.type == EventType.MouseUp))
            {
                input.Use();
                return;
            }
            if (input.type == EventType.ScrollWheel)
            {
                if (viewport.ZoomAt(input.mousePosition.x, input.mousePosition.y, input.delta.y < 0f ? 1 : -1)) input.Use();
                return;
            }

            bool panButton = input.button == 2 || (spaceHeld && input.button == 0);
            if (input.type == EventType.MouseDown && panButton)
            {
                primaryPanning = true;
                input.Use();
            }
            else if (input.type == EventType.MouseDrag && primaryPanning)
            {
                viewport.Pan(input.delta.x, input.delta.y);
                input.Use();
            }
        }

        public void UpdateSideButtonPan(BattlefieldViewport viewport, bool battlefieldVisible, bool sideHeld,
            bool sidePressedThisFrame, Vector2 pointer, Vector2 delta, float referenceScale)
        {
            if (viewport == null || !battlefieldVisible || !sideHeld)
            {
                sideButtonPanning = false;
                return;
            }

            if (!sideButtonPanning && sidePressedThisFrame)
            {
                BattlefieldRect bounds = viewport.ViewportRect;
                sideButtonPanning = bounds.Contains(pointer.x, pointer.y);
            }
            if (!sideButtonPanning || referenceScale <= 0f || delta.sqrMagnitude <= 0f) return;
            viewport.Pan(delta.x / referenceScale, -delta.y / referenceScale);
        }

        public void Reset()
        {
            primaryPanning = false;
            sideButtonPanning = false;
            spaceHeld = false;
        }

        public static bool IsPanButton(int button, bool isSpaceHeld) =>
            button == 2 || IsSidePanButton(button) || (isSpaceHeld && button == 0);

        public static bool IsSidePanButton(int button) => button == 3 || button == 4;

        public static Vector2 ScreenToReferenceUi(Vector2 screenPosition, float screenWidth, float screenHeight,
            float referenceWidth, float referenceHeight)
        {
            float scale = Mathf.Min(screenWidth / referenceWidth, screenHeight / referenceHeight);
            Vector2 offset = new Vector2((screenWidth - referenceWidth * scale) * .5f,
                (screenHeight - referenceHeight * scale) * .5f);
            Vector2 topLeft = new Vector2(screenPosition.x, screenHeight - screenPosition.y);
            return scale > 0f ? (topLeft - offset) / scale : topLeft;
        }
    }
}
