using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class EpicToonVfxShowcaseTests
    {
        [Test]
        public void PlayAt_BuildsADeepCompositeParticleEffect()
        {
            var cameraObject = new GameObject("camera", typeof(Camera));
            var host = new GameObject("showcase");
            GameObject effect = null;
            try
            {
                EpicToonVfxShowcase showcase = host.AddComponent<EpicToonVfxShowcase>();
                showcase.Configure(cameraObject.GetComponent<Camera>());
                effect = showcase.PlayAt(Vector3.zero);
                ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
                Assert.That(systems.Length, Is.GreaterThanOrEqualTo(14));
                Assert.That(showcase.LastSystemCount, Is.EqualTo(systems.Length));
                Assert.That(systems.All(system => system.GetComponent<ParticleSystemRenderer>().sharedMaterial != null), Is.True);
                Assert.That(systems.Select(system => system.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture)
                    .Count(texture => texture != null), Is.EqualTo(systems.Length));
            }
            finally
            {
                if (effect != null) Object.DestroyImmediate(effect);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void PlayLightningAt_BuildsAStagedElectricStrike()
        {
            var cameraObject = new GameObject("camera", typeof(Camera));
            var host = new GameObject("showcase");
            GameObject effect = null;
            try
            {
                EpicToonVfxShowcase showcase = host.AddComponent<EpicToonVfxShowcase>();
                showcase.Configure(cameraObject.GetComponent<Camera>());
                effect = showcase.PlayLightningAt(Vector3.zero);
                ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
                Assert.That(effect.name, Is.EqualTo("Epic Toon 雷霆裁决"));
                Assert.That(systems.Length, Is.GreaterThanOrEqualTo(14));
                Assert.That(showcase.LastLightningSystemCount, Is.EqualTo(systems.Length));
                Assert.That(systems.Count(system => system.emission.burstCount > 0), Is.EqualTo(systems.Length));
                Assert.That(systems.Any(system => system.name == "天穹主雷柱"), Is.True);
                Assert.That(systems.Any(system => system.name == "分叉雷脉"), Is.True);
                Assert.That(systems.All(system => system.GetComponent<ParticleSystemRenderer>().sharedMaterial != null), Is.True);
            }
            finally
            {
                if (effect != null) Object.DestroyImmediate(effect);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
