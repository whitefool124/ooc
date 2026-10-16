from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
src=ROOT/'UnityProject/Assets/Game/Editor/Hd2DLiveAcademyCoverCapture.cs'
s=src.read_text(encoding='utf-8')
s=s.replace('Hd2DLiveAcademyCoverCapture','Hd2DContinuousBattleCapture').replace('OCC.LiveAcademyCoverCapture20261012','OCC.ContinuousBattleCapture20261012')
s=s.replace('2026-10-12_OCC实际掩体材质接入/academy_live','2026-10-12_OCC战斗连续提升/live')
s=s.replace('Capture live mapped academy covers (no focus)','Capture continuous battle material sample (no focus)')
s=s.replace('sample.RenderCamera.Render();var source=sample.RenderCamera.targetTexture;','var source=sample.RenderCamera.targetTexture;\n                var cameraData=sample.RenderCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();\n                bool post=cameraData.renderPostProcessing;\n                foreach(string variant in new[]{"full","no_local_lighting","no_contact","no_diffuse","no_post"})\n                {\n                sample.SetLocalLightingReview(variant!="no_local_lighting"&&variant!="no_contact",variant!="no_local_lighting"&&variant!="no_diffuse");\n                cameraData.renderPostProcessing=variant!="no_post"&&post;\n                sample.RenderCamera.Render();')
s=s.replace('"academy_covers_live.png"','variant+"_battle_rt.png"')
needle='var bound=sample.RenderCamera.transform.parent.GetComponentsInChildren<MeshRenderer>(true)'
s=s.replace(needle,'}\n                sample.SetLocalLightingReview(true,true);cameraData.renderPostProcessing=post;\n                '+needle)
s=s.replace('camera=sample.RenderCamera.name,postProcessing=', 'diffuseStatus=sample.LocalDiffuseStatus,diffuseUpdates=sample.LocalDiffuseUpdates,diffuseSurfaces=sample.LocalDiffuseSurfaces,\n                    camera=sample.RenderCamera.name,postProcessing=')
(ROOT/'UnityProject/Assets/Game/Editor/Hd2DContinuousBattleCapture.cs').write_text(s,encoding='utf-8')
print('Created one local capture helper; no Editor/window operations in this script')
