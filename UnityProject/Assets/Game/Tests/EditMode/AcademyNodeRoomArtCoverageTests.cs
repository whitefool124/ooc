using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class AcademyNodeRoomArtCoverageTests
    {
        [Test]
        public void EveryAcademyNodeTypeHasAnIndependentFormalIdentityIcon()
        {
            RogueliteMapNodeType[] types = RogueliteMapCatalog.Nodes.Select(value => value.Type).Distinct().ToArray();
            Assert.That(types, Is.EquivalentTo(Enum.GetValues(typeof(RogueliteMapNodeType))));
            foreach (RogueliteMapNodeType type in types)
            {
                string runtimeId = type.ToString().ToLowerInvariant();
                Assert.That(FormalArtRegistry.NodeTypes.Any(value => value.RuntimeId == runtimeId), Is.True, runtimeId);
                Assert.That(Resources.Load<Sprite>(FormalArtRegistry.NodeTypePath(runtimeId)), Is.Not.Null, runtimeId);
            }
        }

        [Test]
        public void NavigationArtCoversEnterConfirmBackAndCloseActions()
        {
            foreach (string id in new[] { "continue", "confirm", "back", "close" })
                Assert.That(Resources.Load<Sprite>(FormalArtRegistry.NavigationPath(id)), Is.Not.Null, id);
        }

        [Test]
        public void CombinedCombatDossierHasFormalArchiveDecorationAndDirectEntryContract()
        {
            foreach (string id in new[] { "teaching_record", "sealed_dossier" })
                Assert.That(Resources.Load<Sprite>(FormalUiEffectsConfig.ChapterDividerPath(id)), Is.Not.Null, id);
            foreach (string id in new[] { "teaching_chalk_clip", "sealed_red_clip", "reward_brass_tag" })
                Assert.That(Resources.Load<Sprite>(FormalUiEffectsConfig.ChapterMarkerPath(id)), Is.Not.Null, id);

            MethodInfo entry = typeof(IRogueliteUiHost).GetMethod("StartMapNodeCombat");
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(entry.GetParameters().Select(value => value.ParameterType), Is.EqualTo(new[] { typeof(string) }));
        }

        [Test]
        public void NodeMaterialMappingUsesEveryApprovedChapterFamily()
        {
            string[] dividers = RogueliteMapCatalog.Nodes.Select(FormalUiAssetPlacement.ChapterDivider).Distinct().ToArray();
            string[] markers = RogueliteMapCatalog.Nodes.Select(FormalUiAssetPlacement.ChapterMarker).Distinct().ToArray();
            Assert.That(dividers, Is.SupersetOf(new[] { "teaching_record", "workshop_record", "infirmary_record", "field_survey", "sealed_dossier" }));
            Assert.That(markers, Is.SupersetOf(new[] { "teaching_chalk_clip", "workshop_caliper_clip", "infirmary_bandage_clip", "field_leaf_clip", "sealed_red_clip" }));
            Assert.That(FormalUiEffectsConfig.ChapterMarkerPath("reward_brass_tag"), Is.Not.Empty);
        }
    }
}
