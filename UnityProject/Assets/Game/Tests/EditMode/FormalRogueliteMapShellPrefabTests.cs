using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class FormalRogueliteMapShellPrefabTests
    {
        [Test]
        public void Prefab_ContainsStableMapRegionsAndSerializedReferences()
        {
            FormalRogueliteMapShellView prefab = Resources.Load<FormalRogueliteMapShellView>(
                FormalRogueliteMapShellView.ResourcePath);

            Assert.That(prefab, Is.Not.Null);
            FormalRogueliteMapShellView instance = Object.Instantiate(prefab);
            try
            {
                Assert.DoesNotThrow(instance.ValidateReferences);
                Assert.That(instance.Header.name, Is.EqualTo("页眉"));
                Assert.That(instance.Status.name, Is.EqualTo("行动状态栏"));
                Assert.That(instance.MapViewport.name, Is.EqualTo("节点地图视口"));
                Assert.That(instance.MapCanvas.name, Is.EqualTo("学院分区地图画布"));
                Assert.That(instance.MapCanvas.parent, Is.EqualTo(instance.MapViewport));
                Assert.That(instance.MapViewport.GetComponent<RectMask2D>(), Is.Not.Null);
                Assert.That(instance.ViewportController.transform, Is.EqualTo(instance.MapViewport));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
