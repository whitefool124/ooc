using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class CombatPresentationComposition
    {
        public CombatVisualFeedback Feedback { get; private set; }
        public FormalUiInteractionLayer Interaction { get; private set; }
        public RogueliteSettlementPresentation Settlement { get; private set; }
        public FormalCombatHud CombatHud { get; private set; }
        public FormalRogueliteUi RogueliteUi { get; private set; }
        public FormalStartupPresentation Startup { get; private set; }
        public FormalBattlefieldView Battlefield { get; private set; }
        public DeveloperConsolePanel DeveloperConsole { get; private set; }
        public TarkovInventoryPanel Inventory { get; private set; }

        private CombatPresentationComposition()
        {
        }

        public static CombatPresentationComposition Attach(GameObject owner, ICombatPresentationCompositionHost host)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (host == null) throw new ArgumentNullException(nameof(host));
            var result = new CombatPresentationComposition
            {
                Feedback = Attach(owner, (CombatVisualFeedback value) => value.Initialize(host)),
                Interaction = Attach(owner, (FormalUiInteractionLayer value) => value.Initialize(host)),
                Settlement = Attach(owner, (RogueliteSettlementPresentation value) => value.Initialize(host)),
                CombatHud = Attach(owner, (FormalCombatHud value) => value.Initialize(host)),
                RogueliteUi = Attach(owner, (FormalRogueliteUi value) => value.Initialize(host)),
                Startup = Attach(owner, (FormalStartupPresentation value) => value.Initialize(host)),
                Battlefield = Attach(owner, (FormalBattlefieldView value) => value.Initialize(host))
            };
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DeveloperBuildGate.IsEnabled)
                result.DeveloperConsole = Attach(owner, (DeveloperConsolePanel value) => value.Initialize(host));
#endif
            result.Inventory = Attach(owner, (TarkovInventoryPanel value) => value.Initialize(host));
            return result;
        }

        private static T Attach<T>(GameObject owner, Action<T> initialize) where T : Component
        {
            T component = owner.GetComponent<T>();
            if (component == null) component = owner.AddComponent<T>();
            initialize(component);
            return component;
        }
    }
}
