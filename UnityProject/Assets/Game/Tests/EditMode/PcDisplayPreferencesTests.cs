using System;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class PcDisplayPreferencesTests
    {
        private string prefix;
        [SetUp] public void SetUp() => prefix = "OCC.DisplayTest." + Guid.NewGuid() + ".";
        [TearDown] public void TearDown()
        {
            foreach (string key in new[] { "resolution.v1", "fullscreen.v1", "displayMode.v2", "vSync.v1", "frameRate.v1" })
                PlayerPrefs.DeleteKey(prefix + key);
        }

        [Test] public void FreshInstallUsesWindowed1080pAt60Fps()
        {
            var settings = PcDisplayPreferences.Load(10, prefix);
            Assert.That(settings.ResolutionIndex, Is.EqualTo(2));
            Assert.That(settings.Mode, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(settings.VSync, Is.False);
            Assert.That(PcDisplayPreferences.FrameRates[settings.FrameRateIndex], Is.EqualTo(60));
        }

        [Test] public void LegacyFullscreenPreferenceRetainsBorderlessBehavior()
        {
            PlayerPrefs.SetInt(prefix + "fullscreen.v1", 1);
            Assert.That(PcDisplayPreferences.Load(10, prefix).Mode, Is.EqualTo(FullScreenMode.FullScreenWindow));
        }

        [TestCase(0, FullScreenMode.Windowed)]
        [TestCase(1, FullScreenMode.FullScreenWindow)]
        [TestCase(2, FullScreenMode.ExclusiveFullScreen)]
        public void EachDisplayModeSurvivesRestart(int mode, FullScreenMode expected)
        {
            new PcDisplayPreferences { ResolutionIndex = 4, ModeIndex = mode, VSync = true, FrameRateIndex = 4 }.Save(prefix);
            var loaded = PcDisplayPreferences.Load(10, prefix);
            Assert.That(loaded.Mode, Is.EqualTo(expected));
            Assert.That(loaded.ResolutionIndex, Is.EqualTo(4));
            Assert.That(loaded.VSync, Is.True);
            Assert.That(loaded.FrameRateIndex, Is.EqualTo(4));
        }

        [Test] public void InvalidSavedValuesCannotIndexOutsideOptions()
        {
            PlayerPrefs.SetInt(prefix + "resolution.v1", 500);
            PlayerPrefs.SetInt(prefix + "displayMode.v2", -99);
            PlayerPrefs.SetInt(prefix + "frameRate.v1", 500);
            var loaded = PcDisplayPreferences.Load(10, prefix);
            Assert.That(loaded.ResolutionIndex, Is.EqualTo(9));
            Assert.That(loaded.Mode, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(loaded.FrameRateLabel, Is.EqualTo("不限"));
        }

        [Test] public void RollbackSnapshotDoesNotChangeWhenPendingValuesChange()
        {
            var original = new PcDisplayPreferences();
            var pending = original.Copy();
            pending.ModeIndex = 2;
            pending.ResolutionIndex = 4;
            Assert.That(original.Mode, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(original.ResolutionIndex, Is.EqualTo(2));
        }

        [TestCase(false, 144)]
        [TestCase(true, -1)]
        public void VSyncTakesPriorityOverFrameCap(bool sync, int expected)
        {
            int oldSync = QualitySettings.vSyncCount, oldRate = Application.targetFrameRate;
            try
            {
                new PcDisplayPreferences { VSync = sync, FrameRateIndex = 3 }.Apply(new Vector2Int(1920, 1080));
                Assert.That(QualitySettings.vSyncCount, Is.EqualTo(sync ? 1 : 0));
                Assert.That(Application.targetFrameRate, Is.EqualTo(expected));
            }
            finally { QualitySettings.vSyncCount = oldSync; Application.targetFrameRate = oldRate; }
        }
    }
}
