from pathlib import Path
from PIL import Image
import numpy as np
import json

p=Path(__file__).parent
repo=p.parents[3]
sprite=np.array(Image.open(repo/'UnityProject/Assets/Game/Resources/Art/FormalAcademyEquipmentIcons32/aca_eq_ch03.png').convert('RGBA'))
neutral=np.array(Image.open(p/'metal-neutral.png').convert('RGB'))
warm=np.array(Image.open(p/'metal-warm.png').convert('RGB'))
factor=warm.shape[0]//sprite.shape[0]
source=sprite.repeat(factor,axis=0).repeat(factor,axis=1)
changed=np.any(warm!=neutral,axis=2)
outside=int((changed & (source[:,:,3]==0)).sum())
black=int((changed & (source[:,:,:3].max(axis=2)==0) & (source[:,:,3]>0)).sum())
assert changed.sum()>0, 'No actual reflected-light pixels'
assert outside==0 and black==0, 'Light changes contour or black detail'
restored=np.array(Image.open(p/'metal-restored.png').convert('RGB'))
zero=np.array(Image.open(p/'metal-animation-zero.png').convert('RGB'))
reopened=np.array(Image.open(p/'reopened-metal.png').convert('RGB'))
assert np.array_equal(restored,warm), 'Light was not restored'
assert np.array_equal(zero,warm), 'Static light changes with animation setting'
assert np.array_equal(reopened,warm), 'Reopening changes the local material'
data={'warm_changed_pixels':int(changed.sum()),'outside_alpha':outside,'black_detail_changes':black,'restore_identical':True,'animation_zero_identical':True,'reopened_identical':True,'note':'Actual target image under the fixed formal vice, warm uniform 0 versus .065. Bench and pre-existing side light remain unchanged.'}
(p/'pixel-evidence.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
comparison=Image.new('RGB',(528,256),(244,237,219))
comparison.paste(Image.fromarray(neutral).resize((256,256),Image.Resampling.NEAREST),(0,0))
comparison.paste(Image.fromarray(warm).resize((256,256),Image.Resampling.NEAREST),(272,0))
comparison.save(p/'metal-comparison.png')
Image.open(p/'metal-context.png').crop((140,230,346,334)).resize((618,312),Image.Resampling.NEAREST).save(p/'bench-runtime-crop.png')
print(json.dumps(data,ensure_ascii=False))
