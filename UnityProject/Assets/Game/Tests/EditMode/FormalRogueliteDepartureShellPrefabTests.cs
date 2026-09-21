using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FormalRogueliteDepartureShellPrefabTests
    {
        [Test]
        public void Prefab_ContainsStableDepartureRegionsInRenderOrder()
        {
            FormalRogueliteDepartureShellView prefab = Resources.Load<FormalRogueliteDepartureShellView>(
                FormalRogueliteDepartureShellView.ResourcePath);

            Assert.That(prefab, Is.Not.Null);
            FormalRogueliteDepartureShellView instance = Object.Instantiate(prefab);
            try
            {
                Assert.DoesNotThrow(instance.ValidateReferences);
                Assert.That(instance.Body.name, Is.EqualTo("任务公开信息区"));
                Assert.That(instance.Footer.name, Is.EqualTo("出发操作区"));
                Assert.That(instance.Dossier.name, Is.EqualTo("Pixso任务档案"));
                Assert.That(instance.TopBar.name, Is.EqualTo("Pixso出发准备顶栏"));
                Assert.That(instance.Body.GetSiblingIndex(), Is.LessThan(instance.Footer.GetSiblingIndex()));
                Assert.That(instance.Footer.GetSiblingIndex(), Is.LessThan(instance.Dossier.GetSiblingIndex()));
                Assert.That(instance.Dossier.GetSiblingIndex(), Is.LessThan(instance.TopBar.GetSiblingIndex()));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
