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
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
