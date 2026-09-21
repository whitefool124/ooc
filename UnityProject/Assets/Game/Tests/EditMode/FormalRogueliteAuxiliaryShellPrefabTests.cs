using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FormalRogueliteAuxiliaryShellPrefabTests
    {
        [TestCase(FormalRogueliteAuxiliaryShellView.SettingsResourcePath, "设置卡", 760f)]
        [TestCase(FormalRogueliteAuxiliaryShellView.ArchiveResourcePath, "档案卡", 700f)]
        public void Prefab_ContainsStableCardBodyAndFooter(string resourcePath, string cardName, float height)
        {
            FormalRogueliteAuxiliaryShellView prefab = Resources.Load<FormalRogueliteAuxiliaryShellView>(resourcePath);

            Assert.That(prefab, Is.Not.Null);
            FormalRogueliteAuxiliaryShellView instance = Object.Instantiate(prefab);
            try
            {
                Assert.DoesNotThrow(instance.ValidateReferences);
                Assert.That(instance.Card.name, Is.EqualTo(cardName));
                Assert.That(instance.Card.sizeDelta, Is.EqualTo(new Vector2(1040f, height)));
                Assert.That(instance.Body.name, Is.EqualTo("页面内容区"));
                Assert.That(instance.Footer.name, Is.EqualTo("页面操作区"));
                Assert.That(instance.Body.GetSiblingIndex(), Is.LessThan(instance.Footer.GetSiblingIndex()));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
