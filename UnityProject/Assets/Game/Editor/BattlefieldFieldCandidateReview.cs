using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    public static class BattlefieldFieldCandidateReview
    {
        private const string Path="OCC/Art/Review field effect candidates";
        [MenuItem(Path)]
        public static void Toggle()
        {
            BattlefieldFieldArtCandidates.ReviewEnabled=!BattlefieldFieldArtCandidates.ReviewEnabled;
            EditorApplication.QueuePlayerLoopUpdate();
            Debug.Log("Field effect QA review: "+BattlefieldFieldArtCandidates.ReviewEnabled);
        }
        [MenuItem(Path,true)]
        private static bool Validate(){Menu.SetChecked(Path,BattlefieldFieldArtCandidates.ReviewEnabled);return true;}
    }
}
