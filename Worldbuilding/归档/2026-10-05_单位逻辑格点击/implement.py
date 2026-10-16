from pathlib import Path
p=Path('UnityProject/Assets/Game/Runtime/Presentation/FormalBattlefieldView.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('            // The formal intent glyph already owns its outline. Keep the interaction surface but\n            // do not place an opaque badge behind it; the badge was perceived as a black tile.', '            // Intent is presentation only. Even displaced badges leave the\n            // logical cell under the pointer as the sole battlefield input surface.')
a=s.index('            intentBackground.raycastTarget = true;');b=s.index('            cell.IntentIcon = ',a);s=s[:a]+'            intentBackground.raycastTarget = false;\n'+s[b:]
s=s.replace('                cell.StatusIcons[i].raycastTarget = true;\n                BattlefieldCellPointer statusPointer = status.AddComponent<BattlefieldCellPointer>();\n                statusPointer.Initialize(position, SubmitPrimaryCell, SubmitContextCell, BeginCellHover, EndCellHover);','                cell.StatusIcons[i].raycastTarget = false;')
a=s.index('                if (!cell.VitalsPointerBound)');b=s.index('                BattlefieldRect contract',a);s=s[:a]+s[b:]
a=s.index('                if (cell.UnitPixelRaycast == null)');b=s.index('\n            }\n\n            bool isHero',a);s=s[:a]+'                cell.Unit.raycastTarget = false;'+s[b:]
a=s.index('        private void BindVitalPointer(');b=s.index('        private static Rect BoardImageRect',a);s=s[:a]+s[b:]
s=s.replace('            public CombatUnitPixelRaycast UnitPixelRaycast;\n            public Texture2D HitTexture;\n','').replace('            public bool VitalsPointerBound;\n','');p.write_text(s,encoding='utf-8')
p=Path('UnityProject/Assets/Game/Runtime/Presentation/CombatUnitPixelRaycast.cs');p.write_text('''using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>Legacy component: unit art always passes input to the logical grid.</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class CombatUnitPixelRaycast : MonoBehaviour, ICanvasRaycastFilter
    {
        private void OnEnable() => GetComponent<RawImage>().raycastTarget = false;

        // Retained for existing component references; alpha masks are now only
        // used by visual contours and must never re-enable sprite targeting.
        public void Bind(RawImage graphic, CombatTextureAlphaMask alpha)
        {
            if (graphic != null) graphic.raycastTarget = false;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => false;
    }
}
''',encoding='utf-8')
p=Path('UnityProject/Assets/Game/Tests/EditMode/CombatUnitPixelRaycastTests.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('GraphicSpaceHitsOpaquePixelsAndLetsTransparentPixelsThrough','UnitArtPassesAllPixelsThroughAtEveryDisplayScale').replace('UvFlipMovementAndHiddenTintFollowCurrentGraphic','UvFlipMovementAndTintNeverExpandTheLogicalCellInput').replace('Assert.That(image.raycastTarget, Is.True);','Assert.That(image.raycastTarget, Is.False);').replace('Assert.That(Hit(image, filter, .25f, .25f), Is.True);','Assert.That(Hit(image, filter, .25f, .25f), Is.False);').replace('Assert.That(Hit(image, filter, .75f, .75f), Is.True);','Assert.That(Hit(image, filter, .75f, .75f), Is.False);').replace('Assert.That(Hit(image, filter, .75f, .25f), Is.True);','Assert.That(Hit(image, filter, .75f, .25f), Is.False);');p.write_text(s,encoding='utf-8')
