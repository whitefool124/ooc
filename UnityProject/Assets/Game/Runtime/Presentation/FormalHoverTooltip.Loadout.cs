using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed partial class FormalHoverTooltip
    {
        private bool loadoutContent;
        private readonly List<LoadoutImageStyle> loadoutStyles=new List<LoadoutImageStyle>();
        private sealed class LoadoutImageStyle
        {
            internal Image image;internal Sprite sprite;internal Color color;
            internal Image.Type type;internal float ppu;internal bool active;
        }
        private void RestoreLoadoutChrome()
        {
            if(loadoutStyles.Count>0)metricLabels[0].verticalOverflow=VerticalWrapMode.Truncate;
            foreach(var state in loadoutStyles)
            {
                if(state.image==null)continue;
                state.image.sprite=state.sprite;state.image.color=state.color;
                state.image.type=state.type;state.image.pixelsPerUnitMultiplier=state.ppu;
                state.image.gameObject.SetActive(state.active);
            }
            loadoutStyles.Clear();
        }
        private void MeasureLoadoutMetrics()
        {
            var values=metricLabels.Where(label=>label!=null&&!string.IsNullOrWhiteSpace(label.text)&&label.text!="—").Select(label=>label.text).ToArray();
            for(int i=0;i<metricCells.Length;i++)metricCells[i].gameObject.SetActive(i==0&&values.Length>0);
            metricLabels[0].text=string.Join("　·　",values);
            var label=metricLabels[0];label.alignment=TextAnchor.UpperLeft;
            label.verticalOverflow=VerticalWrapMode.Overflow;
            label.rectTransform.anchoredPosition=new Vector2(8,-4);
            label.rectTransform.sizeDelta=new Vector2(384,160);
            Canvas.ForceUpdateCanvases();
            metricRowHeight=values.Length==0?0:Mathf.Max(48,Mathf.Ceil(label.preferredHeight)+12);
            metricCells[0].anchoredPosition=new Vector2(16,-metricRowTop);
            metricCells[0].sizeDelta=new Vector2(400,metricRowHeight);
            label.rectTransform.sizeDelta=new Vector2(384,Mathf.Max(1,metricRowHeight-8));
        }
        private void ApplyLoadoutChrome()
        {
            foreach(var image in panel.GetComponentsInChildren<Image>(true))
            {
                loadoutStyles.Add(new LoadoutImageStyle{image=image,sprite=image.sprite,color=image.color,
                    type=image.type,ppu=image.pixelsPerUnitMultiplier,active=image.gameObject.activeSelf});
                if(image.name.StartsWith("内容卡描边")||image.name.StartsWith("内容指标描边")||
                    image.name.StartsWith("图标画框描边")||image.name=="正式皮肤")image.gameObject.SetActive(false);
            }
            AetherPaperChrome.ApplyOverlay(panel.GetComponent<Image>(),false);
            artworkFrame.sprite=null;artworkFrame.color=Color.clear;
            foreach(var metric in metricCells)
            {var image=metric.GetComponent<Image>();if(image!=null){image.sprite=null;image.color=Color.clear;}}
        }
    }
}
