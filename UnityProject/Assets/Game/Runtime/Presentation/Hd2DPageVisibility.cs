using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Explicit construction barrier: no screen-space source can be presented
    // until its final scene, text and input geometry are ready together.
    [DefaultExecutionOrder(32000)]
    public sealed class Hd2DPageVisibility : MonoBehaviour
    {
        private CanvasGroup gate;
        private bool ready;
        private float revealStarted, revealDuration;
        public bool IsReady => ready;
        internal float ReadingOpacity => gate == null ? 1f : gate.alpha;
        internal static void Begin(GameObject page)
        {
            var visibility=page.GetComponent<Hd2DPageVisibility>();
            if(visibility==null)visibility=page.AddComponent<Hd2DPageVisibility>();
            if(visibility.gate==null)visibility.gate=page.AddComponent<CanvasGroup>();
            visibility.ready=false;
            visibility.gate.alpha = 0; visibility.gate.interactable = false; visibility.gate.blocksRaycasts = false;
            var backdrop=page.GetComponentInChildren<AcademyDeskBackdrop>();
            var menu=page.GetComponentInChildren<MainMenuDeskScene>();
            visibility.sceneCamera=backdrop!=null&&backdrop.Stage!=null?backdrop.Stage.sceneCamera:
                menu!=null&&menu.Stage!=null?menu.Stage.sceneCamera:null;
            if(visibility.sceneCamera!=null)visibility.sceneCamera.enabled=false;
        }
        internal static void Complete(GameObject page)
        {
            var visibility = page.GetComponent<Hd2DPageVisibility>();
            if (visibility == null) return;
            visibility.ready = true;
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            float intensity=host==null?1f:host.UiPreferences.AnimationIntensity;
            visibility.revealStarted=Time.unscaledTime;
            visibility.revealDuration=.14f*Mathf.Clamp01(intensity);
            visibility.gate.alpha = intensity<=0?1f:.65f;
            // Presentation never holds a ready page's input behind an animation.
            visibility.gate.interactable = true; visibility.gate.blocksRaycasts = true;
            if(visibility.sceneCamera!=null)visibility.sceneCamera.enabled=true;
        }
        private Camera sceneCamera;
        private void LateUpdate()
        {
            if (gate == null) return;
            if(ready)
            {
                float t=revealDuration<=0?1f:Mathf.Clamp01((Time.unscaledTime-revealStarted)/revealDuration);
                gate.alpha=Mathf.Lerp(.65f,1f,1f-(1f-t)*(1f-t));
                return;
            }
            gate.alpha = 0; gate.interactable = false; gate.blocksRaycasts = false;
        }
    }
}
