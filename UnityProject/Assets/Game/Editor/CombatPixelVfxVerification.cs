using System.IO;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.EditorTools
{
    public static class CombatPixelVfxVerification
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-02_战斗特效覆盖"));
        private static bool previousBackground, backgroundChanged;
        [MenuItem("OCC/VFX/Arm Battle Capture")]
        public static void ArmCapture()
        {
            if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
            if (!backgroundChanged) { previousBackground = Application.runInBackground; backgroundChanged = true; }
            Application.runInBackground = true;
            EditorApplication.playModeStateChanged -= RestoreBackground;
            EditorApplication.playModeStateChanged += RestoreBackground;
            EditorApplication.update -= PauseOnEffect; EditorApplication.update += PauseOnEffect;
        }
        private static void RestoreBackground(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode) return;
            if (backgroundChanged) Application.runInBackground = previousBackground;
            backgroundChanged = false; EditorApplication.update -= PauseOnEffect;
            EditorApplication.playModeStateChanged -= RestoreBackground;
        }
        private static void PauseOnEffect()
        {
            var effects = Object.FindObjectsByType<CombatPixelVfxGraphic>(FindObjectsSortMode.None);
            if (!effects.Any(e => e.isActiveAndEnabled && e.NormalizedAge >= .25f && e.NormalizedAge < .75f)) return;
            File.WriteAllLines(Path.Combine(Folder, "capture-effects.txt"), effects.Select(e => e.name + " age=" + e.NormalizedAge + " size=" + e.rectTransform.sizeDelta));
            EditorApplication.update -= PauseOnEffect; EditorApplication.isPaused = true;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        [MenuItem("OCC/VFX/Inspect Battle UI")]
        public static void Inspect()
        {
            Directory.CreateDirectory(Folder);
            var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled && b.IsInteractable());
            File.WriteAllLines(Path.Combine(Folder, "visible-buttons.txt"), buttons.Select(b => b.name + " | " + string.Join(" / ", b.GetComponentsInChildren<Text>().Select(t => t.text.Replace("\n", " ")))));
            var shader = Resources.Load<Shader>("Shaders/CombatPixelLight");
            File.WriteAllLines(Path.Combine(Folder, "shader-messages.txt"), ShaderUtil.GetShaderMessages(shader).Select(m => m.severity + " " + m.message));
            File.WriteAllLines(Path.Combine(Folder, "visible-text.txt"), Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled).Select(t => t.text));
            var host = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (host?.CurrentState != null)
            {
                File.WriteAllLines(Path.Combine(Folder, "units.txt"), host.CurrentState.Units.Values.Select(u => u.Id + " " + u.Position + " HP=" + u.Health + " shield=" + u.Shield));
                File.WriteAllText(Path.Combine(Folder, "input-state.txt"), "action=" + host.SelectedAction + " modal=" + host.IsInteractionModalOpen + " playing=" + host.IsCombatActionPlaying);
            }
        }
        [MenuItem("OCC/VFX/Click Battle UI")]
        public static void Click()
        {
            if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
            string label = File.ReadAllText(Path.Combine(Folder, "click.txt")).Trim();
            if (label.StartsWith("cell:"))
            {
                string cellName = label.Substring(5).Replace(":double", "");
                var target = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(m => m.isActiveAndEnabled && m.GetType().Name == "BattlefieldCellPointer" && m.name == cellName);
                var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, clickCount = 1,
                    position = RectTransformUtility.WorldToScreenPoint(null, target.transform.position) };
                ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerClickHandler);
                if (label.EndsWith(":double")) ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerClickHandler);
                return;
            }
            Button[] matches = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled && b.IsInteractable() &&
                (b.name == label || b.GetComponentsInChildren<Text>().Any(t => t.text == label))).ToArray();
            if (matches.Length != 1) throw new System.InvalidOperationException("Expected exactly one enabled button: " + label + " found " + matches.Length);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(matches[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Debug.Log("VFX verification UI click: " + label);
        }
    }
}
