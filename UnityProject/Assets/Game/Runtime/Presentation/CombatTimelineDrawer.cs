using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // An overlay only. Its visibility never changes battlefield geometry or combat state.
    public sealed class CombatTimelineDrawer : MonoBehaviour
    {
        public const float OpenDelay = .20f;
        public const float CloseDelay = .35f;
        private RectTransform panel;
        private Button tab, pin;
        private Func<bool> blocked;
        private Action closed;
        private float hoveringSince = -1f, outsideSince = -1f;
        public bool IsPinned { get; private set; }
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;

        public void Configure(RectTransform overlay, Button handle, Button pinButton, Func<bool> isBlocked, Action onClose)
        {
            panel = overlay; tab = handle; pin = pinButton; blocked = isBlocked; closed = onClose;
            tab.onClick.AddListener(TogglePin);
            pin.onClick.AddListener(TogglePin);
            ResetDrawer();
        }

        public void OpenPinned()
        {
            IsPinned = true;
            SetOpen(true);
        }

        public void TogglePin()
        {
            IsPinned = !IsPinned;
            // Mouse clicks must not leave keyboard focus holding an unpinned drawer open.
            if (Mouse.current != null && (Contains((RectTransform)tab.transform, Mouse.current.position.ReadValue()) ||
                Contains((RectTransform)pin.transform, Mouse.current.position.ReadValue())))
                EventSystem.current?.SetSelectedGameObject(null);
            if (IsPinned) SetOpen(true);
            UpdateLabels();
        }

        public void ResetDrawer()
        {
            IsPinned = false; hoveringSince = outsideSince = -1f;
            SetOpen(false);
            UpdateLabels();
        }

        private void Update()
        {
            if (panel == null || tab == null) return;
            if (blocked?.Invoke() == true) { ResetDrawer(); return; }
            bool over = Mouse.current != null && (Contains((RectTransform)tab.transform, Mouse.current.position.ReadValue()) ||
                IsOpen && Contains(panel, Mouse.current.position.ReadValue()));
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            bool focused = selected != null && (selected == tab.gameObject || IsOpen && selected.transform.IsChildOf(panel));
            float now = Time.unscaledTime;
            if (IsPinned || focused) { SetOpen(true); outsideSince = -1f; }
            else if (over)
            {
                outsideSince = -1f;
                if (hoveringSince < 0f) hoveringSince = now;
                if (now - hoveringSince >= OpenDelay) SetOpen(true);
            }
            else
            {
                hoveringSince = -1f;
                if (outsideSince < 0f) outsideSince = now;
                if (now - outsideSince >= CloseDelay) SetOpen(false);
            }
        }

        private bool Contains(RectTransform rect, Vector2 pointer)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            return rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect,
                pointer, canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);
        }

        private void SetOpen(bool value)
        {
            if (panel == null || panel.gameObject.activeSelf == value) return;
            panel.gameObject.SetActive(value);
            if (!value) closed?.Invoke();
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (tab != null) tab.GetComponentInChildren<Text>().text = "行\n动\n值\n" + (IsOpen ? "›" : "‹");
            if (pin != null) pin.GetComponentInChildren<Text>().text = IsPinned ? "取消固定" : "固定";
        }

        private void OnDisable() => ResetDrawer();
    }
}
