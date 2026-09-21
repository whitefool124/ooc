using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FormalRogueliteLoadoutShellPrefabTests
    {
        [Test]
        public void Prefab_ContainsStableLoadoutCardAndRegions()
        {
            FormalRogueliteLoadoutShellView prefab = Resources.Load<FormalRogueliteLoadoutShellView>(
                FormalRogueliteLoadoutShellView.ResourcePath);

            Assert.That(prefab, Is.Not.Null);
            FormalRogueliteLoadoutShellView instance = Object.Instantiate(prefab);
            try
            {
                Assert.DoesNotThrow(instance.ValidateReferences);
                Assert.That(instance.Card.name, Is.EqualTo("整备总览"));
                Assert.That(instance.Navigation.name, Is.EqualTo("整备导航区"));
                Assert.That(instance.Body.name, Is.EqualTo("整备内容区"));
                Assert.That(instance.Footer.name, Is.EqualTo("整备操作区"));
                Assert.That(instance.Navigation.GetSiblingIndex(), Is.LessThan(instance.Body.GetSiblingIndex()));
                Assert.That(instance.Body.GetSiblingIndex(), Is.LessThan(instance.Footer.GetSiblingIndex()));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
