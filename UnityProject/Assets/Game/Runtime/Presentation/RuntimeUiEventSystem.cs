using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace OCC.Combat.Presentation
{
    public static class RuntimeUiEventSystem
    {
        public static EventSystem Ensure()
        {
            EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            EventSystem eventSystem = systems.FirstOrDefault(item => item.isActiveAndEnabled) ?? systems.FirstOrDefault();
            if (eventSystem == null)
            {
                GameObject events = new GameObject("EventSystem");
                if (Application.isPlaying) Object.DontDestroyOnLoad(events);
                eventSystem = events.AddComponent<EventSystem>();
            }

            eventSystem.gameObject.SetActive(true);
            eventSystem.enabled = true;

            foreach (EventSystem duplicate in systems.Where(item => item != eventSystem))
            {
                duplicate.enabled = false;
                foreach (BaseInputModule module in duplicate.GetComponents<BaseInputModule>()) module.enabled = false;
            }

            BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
            InputSystemUIInputModule inputModule = modules.OfType<InputSystemUIInputModule>().FirstOrDefault();
            if (inputModule == null && modules.Length == 0)
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            if (inputModule != null)
            {
                inputModule.enabled = true;
                if (inputModule.actionsAsset == null) inputModule.AssignDefaultActions();
            }

            return eventSystem;
        }

        public static bool CancelPressedThisFrame()
        {
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        }

        public static bool AnyInputPressedThisFrame()
        {
            return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
        }

        public static bool AnyInputIsHeld()
        {
            return (Keyboard.current != null && Keyboard.current.anyKey.isPressed) ||
                (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.startButton.isPressed));
        }

        public static void Select(GameObject target)
        {
            if (target == null || !target.activeInHierarchy) return;
            EventSystem eventSystem = Ensure();
            if (eventSystem.currentSelectedGameObject == target) return;
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(target);
        }

        public static void ClearSelection()
        {
            EventSystem eventSystem = Ensure();
            if (eventSystem.currentSelectedGameObject != null) eventSystem.SetSelectedGameObject(null);
        }
    }
}
