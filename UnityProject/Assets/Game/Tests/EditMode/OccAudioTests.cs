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
        [Test] public void FoleyClipsAreShortMonoPcmAndNotClipped()
        {
            var clips=Resources.LoadAll<AudioClip>("Audio/OCC/SFX/Foley");
            Assert.AreEqual(39,clips.Length);
            foreach(var clip in clips)
            {
                Assert.AreEqual(1,clip.channels,clip.name);Assert.Less(clip.length,3,clip.name);
                var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad,importer.defaultSampleSettings.loadType,clip.name);
                Assert.AreEqual(AudioCompressionFormat.PCM,importer.defaultSampleSettings.compressionFormat,clip.name);
                clip.LoadAudioData();var samples=new float[clip.samples];Assert.IsTrue(clip.GetData(samples,0));
                float peak=0;foreach(float v in samples)peak=Mathf.Max(peak,Mathf.Abs(v));
                Assert.That(peak,Is.InRange(.03f,.781f),clip.name);
            }
        }
        [Test] public void InventorySoundsFollowMaterialAndWeight()
        {
            Assert.AreEqual("glass_drop",InventoryClip("ACA-EQ-HD01",null,true,1));
            Assert.AreEqual("cloth_drop",InventoryClip("ACA-EQ-CH01",null,true,2));
            Assert.AreEqual("leather_drop",InventoryClip("ACA-EQ-LG01",null,true));
            Assert.AreEqual("metal_heavy",InventoryClip("ACA-EQ-CH03",null,true,3));
            Assert.AreEqual("metal_light",InventoryClip("ACA-EQ-LG02",null,true,2));
            Assert.AreEqual("paper_pick",InventoryClip(null,"SPEC-TEST",false));
            Assert.AreEqual("latch",InventoryClip("G-01",null,false));
            Assert.IsNull(ServiceClip("shop"));
            Assert.AreEqual("coins",ServiceClip("purchase"));
        }
        private static string InventoryClip(string definition,string material,bool dropped,int weight=2)
            => (string)typeof(InventoryLiftDetail).Assembly.GetType("OCC.Combat.Presentation.OccSoundEffects").GetMethod("InventoryClip",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{definition,material,dropped,weight});
        private static string ServiceClip(string subject)
            => (string)typeof(InventoryLiftDetail).Assembly.GetType("OCC.Combat.Presentation.OccSoundEffects").GetMethod("ServiceClip",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{subject});
    }
}
