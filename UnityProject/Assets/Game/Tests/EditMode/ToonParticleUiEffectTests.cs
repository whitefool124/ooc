using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class ToonParticleUiEffectTests
    {
        [TestCase("fire_impact", ToonParticleRecipe.Impact)]
        [TestCase("heavy_hit", ToonParticleRecipe.Impact)]
        [TestCase("fire_burning_ground", ToonParticleRecipe.Ember)]
        [TestCase("shield_restore", ToonParticleRecipe.Ripple)]
        [TestCase("cleanse", ToonParticleRecipe.Ripple)]
        [TestCase("path", ToonParticleRecipe.Spark)]
        public void ExistingCombatSemantics_MapToNewParticleRecipes(string effect, ToonParticleRecipe expected)
        {
            Assert.That(ToonParticleUiEffect.RecipeFor(effect), Is.EqualTo(expected));
        }

        [TestCase("circle")]
        [TestCase("ring")]
        [TestCase("fire_soft_blank")]
        [TestCase("explosion_sharp_soft_3x3")]
        public void EpicToonSourceTextures_AreImportablePrototypeInputs(string textureName)
        {
            Assert.That(Resources.Load<Texture2D>("Art/PrototypeToonParticles/EpicToonFxSource/" + textureName), Is.Not.Null);
        }

        [Test]
        public void Play_UsesARealParticleSystemAndEmitsImmediately()
        {
            var root = new GameObject("particle-test", typeof(RectTransform));
            try
            {
                ToonParticleUiEffect effect = root.AddComponent<ToonParticleUiEffect>();
                effect.PlayIfChanged("heavy_hit", 64f);
                Assert.That(root.GetComponent<CanvasRenderer>(), Is.Not.Null);
                Assert.That(root.GetComponent<ParticleSystem>(), Is.Not.Null);
                Assert.That(root.GetComponent<ParticleSystem>().particleCount, Is.GreaterThan(0));
                Assert.That(effect.ActiveRecipe, Is.EqualTo(ToonParticleRecipe.Impact));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
