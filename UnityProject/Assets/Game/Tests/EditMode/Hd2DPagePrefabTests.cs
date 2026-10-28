using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Tests
{
    public sealed class Hd2DPagePrefabTests
    {
        [TestCase("WorldCarrier")]
        [TestCase("ReadingPlane")]
        [TestCase("LitSurface")]
        [TestCase("PaperContent")]
        [TestCase("DossierEntry")]
        [TestCase("DeskLighting")]
        [TestCase("workshop")]
        [TestCase("medical")]
        [TestCase("shop")]
        [TestCase("event")]
        [TestCase("loadout-equipment")]
        [TestCase("loadout-spells")]
        public void SerializedTemplatesHaveValidScriptsAndRaycasters(string id)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Resources/UI/HD2D/"+id+".prefab");
            Assert.That(root,Is.Not.Null,id);
            foreach(var node in root.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject),Is.Zero,id+"/"+node.name);
            foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if(canvas.renderMode!=RenderMode.WorldSpace)continue;
                Assert.That(canvas.GetComponents<GraphicRaycaster>().Length,Is.EqualTo(1),id+"/"+canvas.name);
                Assert.That(canvas.GetComponent<Hd2DSceneGraphicRaycaster>(),Is.Not.Null,id+"/"+canvas.name);
                Assert.That(canvas.worldCamera,Is.Null,"Prefab must bind the current isolated scene camera at runtime");
            }
            var page=root.GetComponent<Hd2DPageStructure>();
            if(page==null)return;
            Assert.That(page.AuthoredCarrierCount,Is.GreaterThan(0));
            Assert.That(page.GetComponentsInChildren<Hd2DDeskLightingRig>(true),Is.Empty,
                "Page carriers must use their stage's rig rather than instantiate a second lighting setup");
            var stage=AssetDatabase.LoadAssetAtPath<MainMenuHd2DStage>("Assets/Game/Resources/UI/MainMenuEHD2DStage.prefab");
            Assert.That(stage.Lighting,Is.Not.Null);
            var source=PrefabUtility.GetCorrespondingObjectFromSource(stage.Lighting);
            Assert.That(AssetDatabase.GetAssetPath(source),Is.EqualTo("Assets/Game/Resources/UI/HD2D/DeskLighting.prefab"));
            var serialized=new SerializedObject(page);var carriers=serialized.FindProperty("carriers");
            var names=Enumerable.Range(0,carriers.arraySize).Select(i=>carriers.GetArrayElementAtIndex(i).objectReferenceValue.name).ToArray();
            Assert.That(names.Distinct().Count(),Is.EqualTo(names.Length),"Carrier keys must be unique");
        }
    }
}
