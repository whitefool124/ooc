using System;
using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class AetherBrandingTests
    {
        [TestCase("OCC.FirstExperiencePrototype.v2.slot.0_h279715824", "OCC.FirstExperiencePrototype.v2.slot.0")]
        [TestCase("occ.first_experience.map_run.slot.2_h123", "occ.first_experience.map_run.slot.2")]
        [TestCase("occ.roguelite.ui_preferences_h12", "occ.roguelite.ui_preferences")]
        [TestCase("occ.verification.fixture.slot.0_h1", null)]
        [TestCase("Screenmanager Resolution Width_h1", null)]
        [TestCase("OCC.QuickTestScenario_h1", null)]
        public void MigrationWhitelistsSavesAndSkipsVerificationAndNativeWindowSettings(string source, string expected)
            => Assert.That(AetherLegacyPreferences.LogicalKey(source), Is.EqualTo(expected));

        [Test]
        public void MigrationPreservesChineseSavePayloadAndStripsOnlyTerminalNull()
            => Assert.That(AetherLegacyPreferences.DecodeString(System.Text.Encoding.UTF8.GetBytes("{\"人物\":\"维克多\"}\0")),
                Is.EqualTo("{\"人物\":\"维克多\"}"));

        [TestCase("OCC.FirstExperiencePrototype.volume.v1", true)]
        [TestCase("OCC.Visual.DeskLampBrightness.v1", true)]
        [TestCase("OCC.FirstExperiencePrototype.resolution.v1", false)]
        public void FloatBitsAreNotMistakenForIntegerPreferences(string key, bool expected)
            => Assert.That(AetherLegacyPreferences.IsFloat(key), Is.EqualTo(expected));

        [Test]
        public void UnityWindowsDoubleSizedVolumeRetainsItsValue()
        {
            Assert.That(AetherLegacyPreferences.DecodeFloat(BitConverter.GetBytes(0.800000011920929)), Is.EqualTo(.8f));
            Assert.That(AetherLegacyPreferences.DecodeFloat(BitConverter.GetBytes(.4f)), Is.EqualTo(.4f));
            Assert.Throws<ArgumentException>(() => AetherLegacyPreferences.DecodeFloat(new byte[2]));
        }
    }
}
