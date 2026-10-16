using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    public static class StageSceneryBindingAudit
    {
        public static void Write()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong OCC project");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scenes");
            var levels=Hd2DBattlefieldProfile.Levels.Select(level=>new{
                id=level.Id,name=level.DisplayName,level.Width,level.Height,
                environment=Hd2DBattlefieldProfile.Resolve(level.Id).Environment.ToString(),
                legacyStructures=AcademyBattlefieldLayoutCatalog.Structures(level.Id).Select(p=>new{p.AssetId,p.X,p.TopY,p.WidthCells,p.HeightCells}).ToArray(),
                acceptedStructures=AcademyBattlefieldLayoutCatalog.VisualModules(level.Id).Where(p=>!p.IsGroundAttachment).Select(p=>p.AssetId).ToArray(),
                groundAttachments=AcademyBattlefieldLayoutCatalog.VisualModules(level.Id).Where(p=>p.IsGroundAttachment).Select(p=>new{id=p.AssetId,p.X,p.TopY,asset=AssetDatabase.GetAssetPath(Resources.Load<Texture2D>("Art/FormalAcademyStructures32/"+p.AssetId))}).ToArray(),
                peripheralFixtures=AcademyBattlefieldLayoutCatalog.PeripheralFixtureIds(level.Id).Select(id=>new{id,asset=AssetDatabase.GetAssetPath(Resources.Load<Texture2D>("Art/FormalAcademyStructures32/"+id))}).ToArray()
            }).ToArray();
            var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-16_关卡陈设盘点"));Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"bindings.json"),JsonConvert.SerializeObject(new{
                source="Current compiled catalog and actual resource lookup; binding inventory only, not gameplay or aesthetic approval",
                dataPath=Application.dataPath,levels,
                totals=new{levels=levels.Length,environmentFamilies=levels.Select(l=>l.environment).Distinct().Count(),
                    legacyPlacements=levels.Sum(l=>l.legacyStructures.Length),acceptedRaisedModules=levels.Sum(l=>l.acceptedStructures.Length),
                    groundPlacements=levels.Sum(l=>l.groundAttachments.Length),groundAssetTypes=levels.SelectMany(l=>l.groundAttachments).Select(p=>p.id).Distinct().Count(),
                    levelsWithRestoredFurniture=levels.Count(l=>l.peripheralFixtures.Length>0),restoredFixturePlacements=levels.Sum(l=>l.peripheralFixtures.Length)},
                runtimeRegistry=new{unitBindings=FormalArtRegistry.Units.Count,unitResourceFiles=FormalArtRegistry.Units.Select(e=>e.ResourcePath).Distinct().Count(),
                    runtimeSkillBindings=FormalArtRegistry.RuntimeSkills.Count,fireSpellBindings=FormalArtRegistry.FireSpells.Count,equipmentBindings=FormalArtRegistry.EquipmentItems.Count},
                fileCounts=new{structures=Directory.GetFiles(Path.Combine(Application.dataPath,"Game/Resources/Art/FormalAcademyStructures32"),"*.png").Length,
                    enemyAnimationPngs=Directory.GetFiles(Path.Combine(Application.dataPath,"Game/Resources/Art/FormalEnemyAnimations64"),"*.png",SearchOption.AllDirectories).Length,
                    skillIconPngs=Directory.GetFiles(Path.Combine(Application.dataPath,"Game/Resources/Art/FormalSkillIcons32"),"*.png",SearchOption.AllDirectories).Length},
                missingResources=levels.SelectMany(l=>l.groundAttachments.Select(p=>new{p.id,p.asset}).Concat(l.peripheralFixtures)).Where(p=>string.IsNullOrEmpty(p.asset)).Select(p=>p.id).Distinct().ToArray()
            },Formatting.Indented));
        }
    }
}
