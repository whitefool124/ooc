using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FormalRogueliteServiceShellPrefabTests
    {
        [Test]
        public void Prefab_ContainsStableServiceRegionsInRenderOrder()
        {
            FormalRogueliteServiceShellView prefab = Resources.Load<FormalRogueliteServiceShellView>(
                FormalRogueliteServiceShellView.ResourcePath);

            Assert.That(prefab, Is.Not.Null);
            FormalRogueliteServiceShellView instance = Object.Instantiate(prefab);
            try
            {
                Assert.DoesNotThrow(instance.ValidateReferences);
                Assert.That(instance.Body.name, Is.EqualTo("服务内容区"));
                Assert.That(instance.Footer.name, Is.EqualTo("底部操作区"));
                Assert.That(instance.TopBar.name, Is.EqualTo("服务顶栏"));
                Assert.That(instance.Body.GetSiblingIndex(), Is.LessThan(instance.Footer.GetSiblingIndex()));
                Assert.That(instance.Footer.GetSiblingIndex(), Is.LessThan(instance.TopBar.GetSiblingIndex()));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
