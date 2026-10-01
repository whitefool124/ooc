using System.Collections;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace OCC.Combat.Tests
{
    public sealed class OccAudioVolumeTests
    {
        private Scene scene;
        private float previous;
        [UnityTest]
        public IEnumerator OpeningArchivePreservesMutedAndPreviewVolume()
        {
            previous = AudioListener.volume;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/CombatPrototype.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/CombatPrototype.unity");
            Assert.IsTrue(scene.IsValid());
            var host = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CombatPrototypeBootstrap>()).Single();
            var owner = host.gameObject;
            var opening = owner.GetComponent<FirstExperiencePrototypeController>();
            Assert.IsNotNull(opening);
            opening.PendingVolume = 0;
            host.OpenEncyclopedia();
            yield return null;
            Assert.AreEqual(0, AudioListener.volume, "Archive initialization must preserve mute.");
            Assert.AreEqual(0, host.UiPreferences.MasterVolume);
            opening.PendingVolume = .23f;
            yield return null;
            Assert.AreEqual(.23f, AudioListener.volume, .001f);
            Assert.AreEqual(.23f, host.UiPreferences.MasterVolume, .001f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            AudioListener.volume = previous;
        }
    }
}
