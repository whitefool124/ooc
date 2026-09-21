using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class FormalBattlefieldShellView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/FormalBattlefieldShell";

        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform board;
        [SerializeField] private RectTransform surroundLayer;
        [SerializeField] private BattlefieldViewportInputSurface inputSurface;
        [SerializeField] private Button homeButton;

        public RectTransform Viewport => viewport;
        public RectTransform Board => board;
        public RectTransform SurroundLayer => surroundLayer;
        public BattlefieldViewportInputSurface InputSurface => inputSurface;
        public Button HomeButton => homeButton;

        public static FormalBattlefieldShellView Create(Transform parent)
        {
            FormalBattlefieldShellView prefab = Resources.Load<FormalBattlefieldShellView>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException("Missing formal battlefield shell prefab at Resources/" + ResourcePath + ".prefab");
            FormalBattlefieldShellView view = Instantiate(prefab, parent, false);
            view.name = "战场稳定骨架";
            view.ValidateReferences();
            return view;
        }

        public void Configure(RectTransform viewportReference, RectTransform boardReference,
            RectTransform surroundReference, BattlefieldViewportInputSurface inputReference,
            Button homeReference)
        {
            viewport = viewportReference;
            board = boardReference;
            surroundLayer = surroundReference;
            inputSurface = inputReference;
            homeButton = homeReference;
        }

        public void ValidateReferences()
        {
            if (viewport == null || board == null || surroundLayer == null || inputSurface == null || homeButton == null)
                throw new InvalidOperationException("Formal battlefield shell prefab has incomplete serialized references.");
        }
    }
}
