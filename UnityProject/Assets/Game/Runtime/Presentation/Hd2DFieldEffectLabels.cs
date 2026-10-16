using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Screen text follows the live camera, independently of the old hidden board.
    // The backing and text never participate in pointer hit testing.
    public sealed class Hd2DFieldEffectLabels:MonoBehaviour
    {
        private readonly Dictionary<GridPosition,Text> labels=new Dictionary<GridPosition,Text>();
        public void Refresh(IReadOnlyDictionary<GridPosition,BattlefieldCellPresentation> models,
            Func<GridPosition,Vector2> project)
        {
            foreach(var item in labels)
                if(!models.TryGetValue(item.Key,out var existing)||existing==null||string.IsNullOrEmpty(existing.GroundLabel))
                    item.Value.transform.parent.gameObject.SetActive(false);
            foreach(var item in models)
            {
                var model=item.Value;
                if(model==null||string.IsNullOrEmpty(model.GroundLabel))continue;
                if(!labels.TryGetValue(item.Key,out var text))
                {
                    var holder=new GameObject("场地效果占位_"+item.Key,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                    holder.transform.SetParent(transform,false);var rect=(RectTransform)holder.transform;
                    rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(110,36);
                    var background=holder.GetComponent<Image>();background.color=new Color(.07f,.075f,.07f,.82f);background.raycastTarget=false;
                    text=FormalUiKit.Label("名称",string.Empty,rect,Vector2.zero,new Vector2(110,36),FormalUiTheme.BodyFontSize,
                        new Color(.96f,.94f,.84f),TextAnchor.MiddleCenter);
                    text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
                    text.rectTransform.offsetMin=Vector2.zero;text.rectTransform.offsetMax=Vector2.zero;
                    text.raycastTarget=false;labels.Add(item.Key,text);
                }
                if(!text.transform.parent.gameObject.activeSelf)text.transform.parent.gameObject.SetActive(true);
                if(text.text!=model.GroundLabel)text.text=model.GroundLabel;
                ((RectTransform)text.transform.parent).sizeDelta=new Vector2(110,model.GroundLabel.Contains("\n")?64:36);
                ((RectTransform)text.transform.parent).anchoredPosition=project(item.Key);
            }
        }
    }
}
