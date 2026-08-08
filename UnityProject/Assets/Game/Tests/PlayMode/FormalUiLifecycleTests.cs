using System.Collections;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OCC.Combat.Tests
{
    public sealed class FormalUiLifecycleTests
    {
        [UnityTest]
        public IEnumerator CanvasRoot_IsDestroyedWithItsOwningScene()
        {
            Scene originalScene = SceneManager.GetActiveScene();
            EventSystem[] existingEventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            Scene uiScene = SceneManager.CreateScene("FormalUiLifecycleTests");
            SceneManager.SetActiveScene(uiScene);

            Canvas canvas = FormalUiKit.CanvasRoot("Lifecycle test canvas", 1);
            EventSystem createdEventSystem = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include)
                .FirstOrDefault(item => !existingEventSystems.Contains(item));
            Assert.That(canvas.gameObject.scene, Is.EqualTo(uiScene));

            SceneManager.SetActiveScene(originalScene);
            AsyncOperation unload = SceneManager.UnloadSceneAsync(uiScene);
            Assert.That(unload, Is.Not.Null);
            yield return unload;

            Assert.That(canvas == null, Is.True, "Scene-owned UI must not survive a scene reload.");
            if (createdEventSystem != null) Object.Destroy(createdEventSystem.gameObject);
            yield return null;
        }
    }
}
