using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed partial class FormalRogueliteUi
    {
        private AcademyTutorialPresenter Teaching => (bootstrap as Component)?.GetComponent<AcademyTutorialPresenter>();
        public string TeachingContext
        {
            get
            {
                if (overlay == UiOverlay.Settings) return "settings";
                if (overlay == UiOverlay.Archive) return "archive";
                if (overlay == UiOverlay.Encyclopedia) return "encyclopedia";
                if (overlay == UiOverlay.Loadout) return loadoutSection == LoadoutSection.Spells ? "spells" : "equipment";
                var run = bootstrap?.CurrentMapRun;
                if (run == null) return "map";
                if (run.OpeningGiftPending) return "gift";
                if (run.AwaitingReward) return "reward";
                if (currentScreen == UiScreen.Briefing) return "briefing";
                if (overlay != UiOverlay.NodeRoom) return "map";
                var node = run.MapNode(string.IsNullOrEmpty(selectedNodeId) ? run.CurrentNodeId : selectedNodeId);
                if (node == null) return "map";
                if (node.Type == RogueliteMapNodeType.Workshop) return "workshop";
                if (node.Type == RogueliteMapNodeType.Medical) return "medical";
                if (node.Type == RogueliteMapNodeType.Shop) return "shop";
                return node.IsCombat ? "briefing" : "event";
            }
        }
    }
}
