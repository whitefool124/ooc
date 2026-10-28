using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class RewardGalleryLifecycleTests
    {
        private GameObject root;
        [UnityTest]
        public IEnumerator ClosingRevealRestoresDecorations_WholePageTeardownDoesNotReactivateDestroyedObjects()
        {
            root=new GameObject("Reward lifecycle regression",typeof(RectTransform),typeof(Canvas),typeof(Image));
            var image=root.GetComponent<Image>();AcademyDeskBackdrop.Attach(image,false);
            var backdrop=root.GetComponent<AcademyDeskBackdrop>();
            var stage=(MainMenuHd2DStage)typeof(AcademyDeskBackdrop).GetProperty("Stage",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(backdrop);
            var before=stage.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==stage.transform).ToDictionary(t=>t.gameObject,t=>t.gameObject.activeSelf);
            var window=new GameObject("Reveal window",typeof(RectTransform));window.transform.SetParent(root.transform,false);
            var type=typeof(AcademyDeskBackdrop).Assembly.GetType("OCC.Combat.Presentation.Hd2DRewardGallery");
            type.GetMethod("Attach",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{window,false});
            for(int i=0;i<3;i++)yield return null;
            Assert.That(before.Any(p=>p.Value&&!p.Key.activeSelf),Is.True,"Reveal must actually hide some owned decorations");
            window.SetActive(false);yield return null;
            foreach(var item in before)Assert.That(item.Key.activeSelf,Is.EqualTo(item.Value),item.Key.name);
            window.SetActive(true);for(int i=0;i<3;i++)yield return null;
            Object.Destroy(root);root=null;yield return null;yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup(){if(root!=null)Object.Destroy(root);yield return null;}
    }
}
