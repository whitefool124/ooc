from pathlib import Path
import copy,json,hashlib,shutil
from PIL import Image
root=Path(__file__).resolve().parent
repo=root.parent.parent
template=json.loads((repo/'ArtSource/BookRig_20261008/manifest.json').read_text(encoding='utf-8'))
for name,attempt in [('label',1),('right',2),('left',3)]:
    folder=root/name;folder.mkdir(exist_ok=True)
    path=root/f'paper-native-r{attempt}.png';im=Image.open(path).convert('RGBA')
    im.save(folder/'one_x.png');im.resize((256,256),Image.Resampling.NEAREST).save(folder/'four_x.png')
    Image.merge('RGBA',(im.convert('L'),im.convert('L'),im.convert('L'),im.getchannel('A'))).save(folder/'grayscale.png')
    board=Image.new('RGBA',(256,256),(210,210,210,255))
    for y in range(0,256,16):
        for x in range(0,256,16):
            if (x//16+y//16)%2:board.paste((245,245,245,255),(x,y,x+16,y+16))
    board.alpha_composite(im.resize((256,256),Image.Resampling.NEAREST));board.save(folder/'checker.png')
    m=copy.deepcopy(template);m['asset_id']='ui.main_menu_e.book.'+name;m['status']='REVIEW_READY'
    source=root/f'paper-source-r{attempt}.png'
    m['provenance']={'source_channel':'codex_builtin_imagegen','source_path':source.relative_to(repo).as_posix(),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'source_descriptor':'Independent complete paper leaf; label is composed onto a cloth hard cover without pixel changes.'}
    m['delivery']['output_path']=path.relative_to(repo).as_posix();m['delivery']['output_sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
    m['evidence']={key:(folder/file).relative_to(repo).as_posix() for key,file in {'one_x':'one_x.png','four_x':'four_x.png','grayscale':'grayscale.png','checker':'checker.png','application_contact':'application.png'}.items()}
    m['human_review']['notes']='Pending actual composed book review. Independent source grid passed; no resizing or drawing; material remains at native texel scale.'
    m['unity_import']['runtime_lattice']={'tier':6,'pass':False,'scope':'Pending final application'}
    m['processing']['reports']=[(root/f'paper-qa-r{attempt}.json').relative_to(repo).as_posix()]
    (folder/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
    shutil.copy2(path,repo/f'UnityProject/Assets/Game/Resources/Art/MainMenuE/BookRigReview/{name}.png')
attempts=[]
for kind in ['cover','paper']:
    for i in range(1,4):
        q=json.loads((root/f'{kind}-qa-r{i}.json').read_text())
        attempts.append({'kind':kind,'round':i,'status':q['status'],'confidence':q['subject_lattice_confidence'],'native_subject':q['subject_logical_size']})
(root/'production-log.json').write_text(json.dumps({'provider':'codex_builtin_imagegen','calls':6,'known_cost':None,'attempts':attempts,'composition':'Qualified cloth colour plus complete independent paper label and two independent leaves; physical cover50x33. Rejected full-cover raw art is not imported.'},ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared provenance, review manifests and processed preview textures')
