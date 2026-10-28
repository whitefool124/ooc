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
    public sealed class StageOneRewardBranchTests
    {
        private Scene scene;
        [UnityTest,Timeout(900000)]
        public IEnumerator RewardBranches_RealHostRaycastsSaveRecoveryAndNextRound()
        {
            LogAssert.ignoreFailingMessages=true;
            Assert.That(Application.dataPath.Replace('\\','/'),Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for(int i=0;i<SceneManager.sceneCount;i++)Assert.That(SceneManager.GetSceneAt(i).isDirty,Is.False);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/CombatPrototype.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/CombatPrototype.unity");
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OCC.Combat.EditorTools.StageOnePlaythroughAudit")).First(t=>t!=null);
            yield return (IEnumerator)type.GetMethod("RunRewardsInTest").Invoke(null,null);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-22_第一阶段奖励界面分支验收/runtime"));
            Assert.That(File.Exists(Path.Combine(folder,"failure.json")),Is.False,"See branch failure record");
            Assert.That(File.Exists(Path.Combine(folder,"completed.json")),Is.True);
            Assert.That(File.Exists(Path.Combine(folder,"runtime_errors.txt")),Is.False,"Review runtime error record");
            Assert.That(File.ReadAllText(Path.Combine(folder,"save_isolation.json")),Does.Contain("\"playerSlotsUnchanged\": true"));
            Assert.That(File.ReadAllText(Path.Combine(folder,"save_isolation.json")),Does.Contain("\"reminderRestored\": true"));
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator RewardRendering_InspectLiveSurfaceAndOffscreenCapture()
        {
            Assert.That(Application.dataPath.Replace('\\','/'),Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for(int i=0;i<SceneManager.sceneCount;i++)Assert.That(SceneManager.GetSceneAt(i).isDirty,Is.False);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/CombatPrototype.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/CombatPrototype.unity");
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OCC.Combat.EditorTools.StageOnePlaythroughAudit")).First(t=>t!=null);
            yield return (IEnumerator)type.GetMethod("RunRewardRenderProbeInTest").Invoke(null,null);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            LogAssert.ignoreFailingMessages=false;
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
