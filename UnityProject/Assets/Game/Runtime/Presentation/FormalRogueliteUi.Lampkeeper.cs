using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed partial class FormalRogueliteUi
    {
        private bool openingGiftMapPreview;
        private LampkeeperGiftState presentedOpeningGift;
        private void SyncOpeningGiftPresentation()
        {
            var gift = bootstrap.CurrentMapRun?.OpeningGift;
            if (ReferenceEquals(gift, presentedOpeningGift)) return;
            presentedOpeningGift = gift; openingGiftMapPreview = false;
            if (gift != null && !gift.Resolved) { overlay = UiOverlay.None; Invalidate(); }
        }
        private void DrawOpeningGift()
        {
            var run = bootstrap.CurrentMapRun;
            var gift = run.OpeningGift;
            var template = Resources.Load<LampkeeperGiftView>(LampkeeperGiftView.ResourcePath);
            if (template == null) throw new InvalidOperationException("Missing lampkeeper gift view prefab.");
            var view = Instantiate(template, content.transform, false);
            bool spellChoice = gift.SelectedId == "LK-05";
            string[] ids = spellChoice ? gift.SpellCandidateIds : gift.CandidateIds;
            var spellCatalog = spellChoice ? OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01() : null;
            view.Bind(spellChoice, ids,
                id => spellChoice ? FireSpellCatalog.Get(id).DisplayName : LampkeeperGiftCatalog.Get(id).Name,
                id =>
                {
                    if (!spellChoice) return LampkeeperGiftCatalog.Get(id).Description;
                    return SpellTooltipBody(spellCatalog.Spells.Single(s => s.DefinitionId == id));
                },
                id => { if (spellChoice) bootstrap.ChooseOpeningGiftSpell(id); else bootstrap.ChooseOpeningGift(id); Invalidate(); },
                () => { openingGiftMapPreview = true; SetOverlay(UiOverlay.None); Invalidate(); },
                () => SetOverlay(UiOverlay.Loadout), () => bootstrap.RequestReturnToLanding());
            foreach (var button in view.GetComponentsInChildren<Button>()) focusTargets[button.name] = button.gameObject;
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                BindContentHover(view.ChoiceButtons[i].gameObject, spellChoice ? "个人术式" : "守灯人遗赠",
                    spellChoice ? FireSpellCatalog.Get(id).DisplayName : LampkeeperGiftCatalog.Get(id).Name,
                    spellChoice ? SpellTooltipBody(spellCatalog.Spells.Single(s => s.DefinitionId == id)) : LampkeeperGiftCatalog.Get(id).Description, cyan);
            }
            pendingFocusKey = "按钮_守灯人选择_0";
        }
        private static string OpeningGiftSummary(RogueliteMapRun run)
        {
            var gift = run?.OpeningGift;
            if (gift == null || string.IsNullOrEmpty(gift.SelectedId)) return string.Empty;
            return "守灯人遗赠 · " + LampkeeperGiftCatalog.Get(gift.SelectedId).Name + "　" +
                LampkeeperGiftCatalog.Get(gift.SelectedId).Description +
                (string.IsNullOrEmpty(gift.SelectedSpellId) ? string.Empty : "\n已掌握：" + FireSpellCatalog.Get(gift.SelectedSpellId).DisplayName);
        }
    }
}
