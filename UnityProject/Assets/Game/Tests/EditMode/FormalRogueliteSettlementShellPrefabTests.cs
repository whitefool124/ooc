using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FormalRogueliteSettlementShellPrefabTests
    {
        [Test]
        public void Prefab_ContainsStableSettlementCardAndRegions()
        {
            FormalRogueliteSettlementShellView prefab = Resources.Load<FormalRogueliteSettlementShellView>(
                FormalRogueliteSettlementShellView.ResourcePath);

            Assert.That(prefab, Is.Not.Null);
            FormalRogueliteSettlementShellView instance = Object.Instantiate(prefab);
            try
            {
                Assert.DoesNotThrow(instance.ValidateReferences);
                Assert.That(instance.Card.name, Is.EqualTo("结算卡"));
                Assert.That(instance.Header.name, Is.EqualTo("结算标题区"));
                Assert.That(instance.Rewards.name, Is.EqualTo("奖励选择区"));
                Assert.That(instance.Footer.name, Is.EqualTo("结算操作区"));
                Assert.That(instance.Header.GetSiblingIndex(), Is.LessThan(instance.Rewards.GetSiblingIndex()));
                Assert.That(instance.Rewards.GetSiblingIndex(), Is.LessThan(instance.Footer.GetSiblingIndex()));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
