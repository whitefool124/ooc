using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    public static class AcademyTutorialVerificationMenu
    {
        [MenuItem("OCC/Teaching/Build verified Windows delivery")]
        public static void BuildDelivery()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Aether_Stage1_20261024_FullSystemTeaching/以太.exe"));
            string report = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-23_全系统教学落地/build-status.txt"));
            if (File.Exists(output)) throw new InvalidOperationException("Delivery already exists; inspect it before rebuilding.");
            File.WriteAllText(report, "QUEUED");
            EditorApplication.delayCall += () =>
            {
                try { File.WriteAllText(report, "BUILDING"); OccWindowsPlayerBuild.BuildTo(output); File.WriteAllText(report, "PASS\n" + output); }
                catch (Exception error) { File.WriteAllText(report, "FAIL\n" + error); Debug.LogException(error); }
            };
        }
        [MenuItem("OCC/Teaching/Verify normal entry and layout")]
        public static void VerifyVisuals()
        {
            if (Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets") throw new InvalidOperationException("Wrong main project.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene; verification will not save it.");
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-23_全系统教学落地/visual-check-v3"));
            SessionState.SetString("OCC.Teaching.VisualProbe", folder);
            EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");
            EditorApplication.isPlaying = true;
        }
    }
}
