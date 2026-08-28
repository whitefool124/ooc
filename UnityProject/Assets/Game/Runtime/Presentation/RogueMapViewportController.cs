using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class RogueMapViewportController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public const float ReferenceDragThreshold = 10f;
        public const float KeyboardPanSpeed = 720f;
        private static readonly float[] zoomLevels = { 1f, 2f };

        private RectTransform viewport;
        private RectTransform mapContent;
        private Canvas canvas;
        private int zoomIndex;
        private Vector2 dragOrigin;
        private bool dragging;

        public bool IsDragging => dragging;
        public float Zoom => zoomLevels[zoomIndex];
        public int ZoomIndex => zoomIndex;
        public RectTransform MapContent => mapContent;

        public void Initialize(RectTransform viewportRect, RectTransform contentRect, Canvas ownerCanvas, int initialZoomIndex = 0)
        {
            viewport = viewportRect != null ? viewportRect : throw new ArgumentNullException(nameof(viewportRect));
            mapContent = contentRect != null ? contentRect : throw new ArgumentNullException(nameof(contentRect));
            canvas = ownerCanvas != null ? ownerCanvas : throw new ArgumentNullException(nameof(ownerCanvas));
            zoomIndex = Mathf.Clamp(initialZoomIndex, 0, zoomLevels.Length - 1);
            mapContent.localScale = Vector3.one * Zoom;
            ClampCurrentPosition();
        }

        public void SetView(Vector2 pan, int requestedZoomIndex)
        {
            if (!Ready) return;
            zoomIndex = Mathf.Clamp(requestedZoomIndex, 0, zoomLevels.Length - 1);
            mapContent.localScale = Vector3.one * Zoom;
            mapContent.anchoredPosition = ClampPan(pan, viewport.rect.size, mapContent.rect.size, Zoom);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Ready || eventData.button != PointerEventData.InputButton.Left && eventData.button != PointerEventData.InputButton.Middle) return;
            dragOrigin = eventData.position;
            dragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!Ready || eventData.button != PointerEventData.InputButton.Left && eventData.button != PointerEventData.InputButton.Middle) return;
            float scale = canvas.scaleFactor <= 0 ? 1f : canvas.scaleFactor;
            if (!dragging && Vector2.Distance(dragOrigin, eventData.position) / scale < ReferenceDragThreshold) return;
            dragging = true;
            eventData.eligibleForClick = false;
            mapContent.anchoredPosition = ClampPan(mapContent.anchoredPosition + eventData.delta / scale,
                viewport.rect.size, mapContent.rect.size, Zoom);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragging) eventData.eligibleForClick = false;
            dragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!Ready || Mathf.Approximately(eventData.scrollDelta.y, 0)) return;
            int next = Mathf.Clamp(zoomIndex + (eventData.scrollDelta.y > 0 ? 1 : -1), 0, zoomLevels.Length - 1);
            if (next == zoomIndex) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out Vector2 pointer);
            float previous = Zoom;
            zoomIndex = next;
            mapContent.localScale = Vector3.one * Zoom;
            mapContent.anchoredPosition = ZoomAroundPoint(mapContent.anchoredPosition, pointer, previous, Zoom);
            ClampCurrentPosition();
            eventData.eligibleForClick = false;
        }

        public void ZoomIn() => SetZoomIndex(zoomIndex + 1);

        public void ZoomOut() => SetZoomIndex(zoomIndex - 1);

        private void SetZoomIndex(int requestedZoomIndex)
        {
            if (!Ready) return;
            int next = Mathf.Clamp(requestedZoomIndex, 0, zoomLevels.Length - 1);
            if (next == zoomIndex) return;
            float previous = Zoom;
            zoomIndex = next;
            mapContent.localScale = Vector3.one * Zoom;
            mapContent.anchoredPosition = ZoomAroundPoint(mapContent.anchoredPosition, Vector2.zero, previous, Zoom);
            ClampCurrentPosition();
        }

        private void Update()
        {
            if (!Ready || dragging) return;
            Vector2 direction = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction.x -= 1;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction.x += 1;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction.y -= 1;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction.y += 1;
            }
            if (Gamepad.current != null) direction += Gamepad.current.leftStick.ReadValue();
            if (direction.sqrMagnitude <= .01f) return;
            mapContent.anchoredPosition = ClampPan(mapContent.anchoredPosition - Vector2.ClampMagnitude(direction, 1f) * KeyboardPanSpeed * Time.unscaledDeltaTime,
                viewport.rect.size, mapContent.rect.size, Zoom);
        }

        public void CenterOnSourcePosition(Vector2 sourcePosition)
        {
            if (!Ready) return;
            Vector2 logical = AcademyMap3DLayout.ProjectMapToCanvas(sourcePosition);
            mapContent.anchoredPosition = ClampPan(-logical * Zoom, viewport.rect.size, mapContent.rect.size, Zoom);
        }

        public void ResetToOverview()
        {
            if (!Ready) return;
            zoomIndex = 0;
            mapContent.localScale = Vector3.one;
            mapContent.anchoredPosition = Vector2.zero;
            ClampCurrentPosition();
        }

        public static Vector2 ClampPan(Vector2 position, Vector2 viewportSize, Vector2 contentSize, float zoom)
        {
            float x = Mathf.Max(0, (contentSize.x * zoom - viewportSize.x) * .5f);
            float y = Mathf.Max(0, (contentSize.y * zoom - viewportSize.y) * .5f);
            return new Vector2(Mathf.Clamp(position.x, -x, x), Mathf.Clamp(position.y, -y, y));
        }

        public static Vector2 ZoomAroundPoint(Vector2 contentPosition, Vector2 pointerLocal, float previousZoom, float nextZoom)
        {
            if (previousZoom <= 0) throw new ArgumentOutOfRangeException(nameof(previousZoom));
            return pointerLocal + (contentPosition - pointerLocal) * (nextZoom / previousZoom);
        }

        private bool Ready => viewport != null && mapContent != null && canvas != null;

        private void ClampCurrentPosition()
        {
            if (!Ready) return;
            mapContent.anchoredPosition = ClampPan(mapContent.anchoredPosition, viewport.rect.size, mapContent.rect.size, Zoom);
        }
    }
}
