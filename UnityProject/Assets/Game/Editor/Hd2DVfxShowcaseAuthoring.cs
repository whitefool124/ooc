using System;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Hd2DVfxShowcaseAuthoring
{
    public const string ScenePath = "Assets/Scenes/EpicToonVfxShowcase.unity";

    [MenuItem("OCC/VFX/Install HD2D Explosion Laboratory")]
    public static void Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring the VFX scene.");
        string root = System.IO.Path.GetFullPath(Application.dataPath + "/../..").Replace('\\', '/');
        if (root != "E:/数据库/OCC_Codex" && root != "D:/occart/OCC_latest")
            throw new InvalidOperationException("Expected the OCC main checkout.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open the dedicated EpicToonVfxShowcase scene first.");
        for (int i=0;i<SceneManager.sceneCount;i++)
            if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Existing unsaved scene changes; nothing was modified.");
        if(UnityEngine.Object.FindFirstObjectByType<Hd2DVfxShowcase>(FindObjectsInactive.Include)!=null)
            throw new InvalidOperationException("HD2D laboratory already installed; inspect it rather than replacing it.");
        Shader ring=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Game/Runtime/Presentation/Shaders/Hd2DVfxGroundRing.shader");
        Shader smoke=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Game/Runtime/Presentation/Shaders/Hd2DVfxLitSmoke.shader");
        Shader particles=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Game/Runtime/Presentation/Shaders/EpicToonParticle.shader");
        if(ring==null||smoke==null||particles==null)throw new InvalidOperationException("Missing imported HD2D shaders.");
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install OCC HD2D VFX lab");
        // Preserve the old two-dimensional laboratory for reversible comparison.
        foreach(var old in scene.GetRootGameObjects()){Undo.RecordObject(old,"Preserve original VFX scene");old.SetActive(false);}
        var obj=new GameObject("OCC HD2D 光影特效实验室");Undo.RegisterCreatedObjectUndo(obj,"Create HD2D VFX laboratory");
        var lab=Undo.AddComponent<Hd2DVfxShowcase>(obj);
        lab.GroundRingShader=ring;lab.SmokeShader=smoke;lab.ParticleShader=particles;
        EditorUtility.SetDirty(lab);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(group);
        Debug.Log("OCC_HD2D_VFX_LAB_SAVED "+ScenePath);
    }
}
