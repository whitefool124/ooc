using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FormalCombatHudShellPrefabTests
    {
        [Test]
        public void Prefab_ContainsStableHudRegionsAndSerializedReferences()
        {
            FormalCombatHudShellView prefab = Resources.Load<FormalCombatHudShellView>(
                FormalCombatHudShellView.ResourcePath);

            Assert.That(prefab, Is.Not.Null);
            FormalCombatHudShellView instance = Object.Instantiate(prefab);
            try
            {
                Assert.DoesNotThrow(instance.ValidateReferences);
                Assert.That(instance.Header.name, Is.EqualTo("战斗抬头"));
                Assert.That(instance.RightConsole.name, Is.EqualTo("战斗信息"));
                Assert.That(instance.ActionPointBadge.name, Is.EqualTo("行动点徽章"));
                Assert.That(instance.Commands.name, Is.EqualTo("战术指令"));
                Assert.That(instance.HeroStatus.sizeDelta, Is.EqualTo(new Vector2(480, 160)));
                Assert.That(instance.ActionOrder.sizeDelta, Is.EqualTo(new Vector2(912, 96)));
                Assert.That(instance.TimelineTab.sizeDelta, Is.EqualTo(new Vector2(44, 136)));
                Assert.That(instance.RightConsole.sizeDelta, Is.EqualTo(new Vector2(416, 436)));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
