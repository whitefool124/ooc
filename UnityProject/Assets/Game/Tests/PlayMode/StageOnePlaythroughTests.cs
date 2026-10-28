using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OCC.Combat.Tests
{
    public sealed class StageOnePlaythroughTests
    {
        private Scene scene;
        [UnityTest,Order(1),Timeout(1800000)]
        public IEnumerator NormalNewSlot_StageOneToFinalSummary()
        {
            // Runtime errors are recorded as findings while continuing the journey;
            // completion assertions still fail on an actual blocked route.
            LogAssert.ignoreFailingMessages=true;
            Assert.That(Application.dataPath.Replace('\\','/'),Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for(int i=0;i<SceneManager.sceneCount;i++)
                Assert.That(SceneManager.GetSceneAt(i).isDirty,Is.False,"Preserve dirty scene");
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/CombatPrototype.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/CombatPrototype.unity");
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OCC.Combat.EditorTools.StageOnePlaythroughAudit")).First(t=>t!=null);
            yield return (IEnumerator)type.GetMethod("RunInTest").Invoke(null,new object[]{false});
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-21_第一阶段卡死修复/runtime"));
            Assert.That(File.Exists(Path.Combine(root,"failure.json")),Is.False,"See playthrough failure evidence");
            Assert.That(File.Exists(Path.Combine(root,"completed.json")),Is.True,"Full stage did not finish");
        }
        [UnityTest,Order(0),Timeout(180000)]
        public IEnumerator SavedRound_MapRecoversAcrossContinueAndLoadoutReturn()
        {
            LogAssert.ignoreFailingMessages=true;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/CombatPrototype.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/CombatPrototype.unity");
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OCC.Combat.EditorTools.StageOnePlaythroughAudit")).First(t=>t!=null);
            yield return (IEnumerator)type.GetMethod("RunInTest").Invoke(null,new object[]{true});
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-21_第一阶段卡死修复/recovery"));
            Assert.That(File.Exists(Path.Combine(root,"failure.json")),Is.False);
            Assert.That(File.Exists(Path.Combine(root,"map_recovery.json")),Is.True);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            LogAssert.ignoreFailingMessages=false;
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
