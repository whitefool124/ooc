using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEditor;

namespace OCC.Combat.Tests
{
    public sealed class OccAudioTests
    {
        [Test] public void VideoAlwaysYieldsToItsOwnAudio()
            => Assert.AreEqual("", OccAudioDirector.SelectMusic(true, true, true, true, true, true, true));
        [Test] public void ArchiveAndOutcomeTakePriorityOverCombat()
        {
            Assert.AreEqual("archive_piano", OccAudioDirector.SelectMusic(false, true, true, true, false, false, true));
            Assert.AreEqual("archive_piano", OccAudioDirector.SelectMusic(false, false, true, true, true, false, true));
            Assert.AreEqual("boss_exam", OccAudioDirector.SelectMusic(false, false, true, true, false, false, true));
            Assert.AreEqual("jrpg_practicum", OccAudioDirector.SelectMusic(false, false, true, false, false, false, true));
            Assert.AreEqual("magic_town", OccAudioDirector.SelectMusic(false, false, false, false, false, true, false));
            Assert.AreEqual("academy_town", OccAudioDirector.SelectMusic(false, false, false, false, false, false, true));
        }
        [Test] public void AllMusicExistsAndStreams()
        {
            foreach (var name in new[] { "archive_piano", "academy_town", "magic_town", "jrpg_practicum", "boss_exam" })
            {
                var clip = Resources.Load<AudioClip>("Audio/OCC/Music/" + name);
                Assert.IsNotNull(clip, name);
                Assert.Greater(clip.length, 10, name);
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
                Assert.IsNotNull(importer, name);
                Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType, name);
            }
        }
        [Test] public void ShortCuesHaveAudibleSignalWithoutClipping()
        {
            foreach (var name in new[] { "archive_select", "archive_confirm", "archive_reject", "aether_ready", "archive_reward", "practicum_complete", "practicum_retry" })
            {
                var clip = Resources.Load<AudioClip>("Audio/OCC/SFX/" + name);
                Assert.IsNotNull(clip, name);
                clip.LoadAudioData();
                var samples = new float[clip.samples * clip.channels];
                Assert.IsTrue(clip.GetData(samples, 0), name);
                float peak = 0;
                foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
                Assert.That(peak, Is.InRange(.03f, .99f), name);
            }
        }
        [Test] public void SelectedFreeEffectsDecodeAndHaveMatchedLevels()
        {
            var clips = Resources.LoadAll<AudioClip>("Audio/OCC/SFX/FreeProcessed");
            Assert.AreEqual(28, clips.Length);
            foreach (var clip in clips)
            {
                clip.LoadAudioData();
                var samples = new float[clip.samples * clip.channels];
                Assert.IsTrue(clip.GetData(samples, 0), clip.name);
                float peak = 0;
                foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
                Assert.That(peak, Is.InRange(.15f, .90f), clip.name);
            }
        }
    }
}
