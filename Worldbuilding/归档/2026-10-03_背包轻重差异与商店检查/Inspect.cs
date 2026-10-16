using UnityEngine;
using System.Reflection;
using System.IO;
using OCC.Combat.Presentation;
public static class WeightInspect
{
    public static void Main()
    {
        var ui=Object.FindAnyObjectByType<FormalRogueliteUi>();
        var f=BindingFlags.Instance|BindingFlags.NonPublic;
        var go=(GameObject)typeof(FormalRogueliteUi).GetField("loadoutDragGhost",f).GetValue(ui);
        var lines=new System.Collections.Generic.List<string>();
        lines.Add("mouse="+UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        lines.Add("lastPointer="+typeof(FormalRogueliteUi).GetField("loadoutLastPointer",f).GetValue(ui));
        lines.Add("goggleSprite="+(Resources.Load<Sprite>(OCC.Combat.FormalArtRegistry.EquipmentFootprintPath("ACA-EQ-HD01"))!=null));
        if(go!=null){lines.Add("active="+go.activeSelf);foreach(var t in go.GetComponentsInChildren<Transform>(true))lines.Add(t.name+" active="+t.gameObject.activeInHierarchy);}
        File.WriteAllLines(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_背包轻重差异与商店检查/inspect.txt")),lines);
    }
}
