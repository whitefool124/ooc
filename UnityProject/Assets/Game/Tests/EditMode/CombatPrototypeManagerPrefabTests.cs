using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatPrototypeManagerPrefabTests
    {
        [Test]
        public void Prefab_OwnsProductionPresentationControllers()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/CombatPrototypeManager.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<CombatPrototypeBootstrap>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CombatVisualFeedback>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<FormalUiInteractionLayer>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<RogueliteSettlementPresentation>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<FormalCombatHud>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<FormalRogueliteUi>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<FormalStartupPresentation>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CombatFlowTransitionPresentation>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CombatEntrySequencePresentation>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<FormalBattlefieldView>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<DeveloperConsolePanel>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<TarkovInventoryPanel>(), Is.Not.Null);
        }
    }
}
