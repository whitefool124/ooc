using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
namespace OCC.Combat.Presentation
{
    public sealed class Hd2DSceneGraphicRaycaster : GraphicRaycaster
    {
        internal static Camera ResolveEventCamera(Camera camera)
        {
            var stage=camera==null?null:camera.GetComponentInParent<MainMenuHd2DStage>();
            return stage==null?camera:stage.InputCamera;
        }
        public override Camera eventCamera=>ResolveEventCamera(base.eventCamera);
    }
}
