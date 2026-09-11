using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class RogueLoadoutEquipmentPrefabTests
    {
        [Test]
        public void Prefab_OwnsStableThreeColumnInventorySkeleton()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Resources/UI/Prefabs/RogueLoadoutEquipmentView.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetType(prefab), Is.EqualTo(PrefabAssetType.Regular));
            Assert.That(prefab.transform.Cast<Transform>().Select(child => child.name), Is.EquivalentTo(new[]
            {
                "角色信息栏", "装备工作区", "背包工作区"
            }));

            RogueLoadoutEquipmentView view = prefab.GetComponent<RogueLoadoutEquipmentView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsValid(), Is.True);
            Assert.That(view.EquipmentSlots, Has.Length.EqualTo(9));
            Assert.That(view.TacticalSlots, Has.Length.EqualTo(4));
            Assert.That(view.BackpackCells, Has.Length.EqualTo(60));
            Assert.That(view.BackpackCells.Select(cell => cell.name).Distinct().Count(), Is.EqualTo(60));
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab), Is.Zero);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                RogueLoadoutEquipmentView runtimeView = instance.GetComponent<RogueLoadoutEquipmentView>();
                Assert.That(runtimeView.ConfigureBackpackGrid(6, 4), Is.EqualTo(64f));
                Assert.That(runtimeView.BackpackCells.Count(cell => cell.gameObject.activeSelf), Is.EqualTo(24));
                Assert.That(runtimeView.BackpackItemsRoot.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(runtimeView.BackpackItemsRoot.sizeDelta, Is.EqualTo(new Vector2(384f, 256f)));
                Assert.That(runtimeView.BackpackPanel.Find("背包标题").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("背包 6列×4行"));
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
