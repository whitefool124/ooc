using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FormalBattlefieldShellPrefabTests
    {
        [Test]
        public void Prefab_ContainsStableBattlefieldHierarchyAndSerializedReferences()
        {
            FormalBattlefieldShellView prefab = Resources.Load<FormalBattlefieldShellView>(
                FormalBattlefieldShellView.ResourcePath);

            Assert.That(prefab, Is.Not.Null);
            FormalBattlefieldShellView instance = Object.Instantiate(prefab);
            try
            {
                Assert.DoesNotThrow(instance.ValidateReferences);
                Assert.That(instance.Viewport.name, Is.EqualTo("战场裁切视口"));
                Assert.That(instance.Board.parent, Is.EqualTo(instance.Viewport));
                Assert.That(instance.SurroundLayer.parent, Is.EqualTo(instance.Board));
                Assert.That(instance.InputSurface.transform, Is.EqualTo(instance.Viewport));
                Assert.That(instance.HomeButton.transform.parent, Is.EqualTo(instance.Viewport));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
