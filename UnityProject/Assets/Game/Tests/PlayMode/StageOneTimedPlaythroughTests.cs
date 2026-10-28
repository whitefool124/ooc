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
    public sealed class StageOneTimedPlaythroughTests
    {
        private Scene scene;
        [UnityTest,Timeout(1800000)]
        public IEnumerator PreservedNormalRound_ContinueToTimedFinaleAndSummary()
        {
            LogAssert.ignoreFailingMessages=true;
            Assert.That(Application.dataPath.Replace('\\','/'),Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for(int i=0;i<SceneManager.sceneCount;i++)Assert.That(SceneManager.GetSceneAt(i).isDirty,Is.False);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/CombatPrototype.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/CombatPrototype.unity");
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OCC.Combat.EditorTools.StageOnePlaythroughAudit")).First(t=>t!=null);
            yield return (IEnumerator)type.GetMethod("RunTimedInTest").Invoke(null,null);
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,Environment.GetEnvironmentVariable("OCC_BALANCE_UI")=="1" ? "../../Worldbuilding/归档/2026-10-22_第一阶段战斗测试与平衡/runtime" : "../../Worldbuilding/归档/2026-10-21_第一阶段继续试玩/runtime"));
            Assert.That(File.Exists(Path.Combine(root,"failure.json")),Is.False,"See captured UI and failure record");
            Assert.That(File.Exists(Path.Combine(root,"completed.json")),Is.True);
            Assert.That(File.Exists(Path.Combine(root,"runtime_errors.txt")),Is.False,"Runtime errors must be reviewed");
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            LogAssert.ignoreFailingMessages=false;
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
