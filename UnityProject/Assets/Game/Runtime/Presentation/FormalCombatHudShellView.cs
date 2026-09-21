using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class FormalCombatHudShellView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/FormalCombatHudShell";

        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform rightConsole;
        [SerializeField] private RectTransform actionPointBadge;
        [SerializeField] private RectTransform commands;

        public RectTransform Header => header;
        public RectTransform RightConsole => rightConsole;
        public RectTransform ActionPointBadge => actionPointBadge;
        public RectTransform Commands => commands;

        public static FormalCombatHudShellView Create(Transform parent)
        {
            FormalCombatHudShellView prefab = Resources.Load<FormalCombatHudShellView>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException("Missing formal combat HUD shell prefab at Resources/" + ResourcePath + ".prefab");
            FormalCombatHudShellView view = Instantiate(prefab, parent, false);
            view.name = "战斗HUD稳定骨架";
            view.ValidateReferences();
            return view;
        }

        public void Configure(RectTransform headerReference, RectTransform consoleReference,
            RectTransform actionPointReference, RectTransform commandsReference)
        {
            header = headerReference;
            rightConsole = consoleReference;
            actionPointBadge = actionPointReference;
            commands = commandsReference;
        }

        public void ValidateReferences()
        {
            if (header == null || rightConsole == null || actionPointBadge == null || commands == null)
                throw new InvalidOperationException("Formal combat HUD shell prefab has incomplete serialized references.");
        }
    }
}
