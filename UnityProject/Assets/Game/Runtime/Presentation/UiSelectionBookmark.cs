using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // A stationary shape cue survives reduced motion and grayscale presentation.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiSelectionBookmark : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int i = 0; i < 3; i++)
            {
                float x = -3f + i * 2f;
                float h = 12f - i * 4f;
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(x, -h * .5f), color, Vector2.zero);
                vh.AddVert(new Vector3(x, h * .5f), color, Vector2.zero);
                vh.AddVert(new Vector3(x + 2f, h * .5f), color, Vector2.zero);
                vh.AddVert(new Vector3(x + 2f, -h * .5f), color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
