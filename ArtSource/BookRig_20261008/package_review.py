from pathlib import Path
from PIL import Image
import json,hashlib
root=Path(__file__).resolve().parent
im=Image.open(root/'cover-native-r2.png').convert('RGBA')
im.save(root/'one_x.png');im.resize((256,256),Image.Resampling.NEAREST).save(root/'four_x.png')
gray=Image.merge('RGBA',(*([im.convert('L')]*3),im.getchannel('A')));gray.save(root/'grayscale.png')
board=Image.new('RGBA',(256,256),(210,210,210,255))
for y in range(0,256,16):
    for x in range(0,256,16):
        if (x//16+y//16)%2:board.paste((245,245,245,255),(x,y,x+16,y+16))
board.alpha_composite(im.resize((256,256),Image.Resampling.NEAREST));board.save(root/'checker.png')
m=json.loads(Path('ArtSource/MainMenuE_Native64_20261008/book_closed/manifest.json').read_text())
m['asset_id']='ui.main_menu_e.rig.flat_cover';m['status']='REVIEW_READY'
m['provenance']={'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent static overhead cover generation r2. Not an animation sheet. r1 mixed lattice rejected, r3 exceeds 64px role canvas rejected.','source_path':'ArtSource/BookRig_20261008/cover-source-r2.png','source_sha256':hashlib.sha256((root/'cover-source-r2.png').read_bytes()).hexdigest()}
m['delivery']['output_path']='ArtSource/BookRig_20261008/cover-native-r2.png';m['delivery']['output_sha256']=hashlib.sha256((root/'cover-native-r2.png').read_bytes()).hexdigest()
m['evidence']={k:'ArtSource/BookRig_20261008/'+v for k,v in {'one_x':'one_x.png','four_x':'four_x.png','grayscale':'grayscale.png','checker':'checker.png','application_contact':'rig-closed.png'}.items()}
m['human_review']['notes']='Pending human review of static material and physical hinge. Source native subject21x26 stays intact; Unity surfaces rotated orthographically, no source resizing.'
m['unity_import']['runtime_lattice']={'tier':6,'pass':False,'scope':'Pending review capture'}
m['processing']={'method':'subject_grid_lossless_decode','geometric_component_resizing':False,'reports':['ArtSource/BookRig_20261008/cover-qa-r2.json'],'human_aesthetic_acceptance':False}
(root/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2))
(root/'run-log.json').write_text(json.dumps({'provider':'Codex built-in image_gen','calls':3,'known_cost':None,'attempts':[{'id':'r1','decision':'REJECTED mixed grid','qa':'cover-qa-r1.json'},{'id':'r2','decision':'REVIEW_READY static cover','qa':'cover-qa-r2.json'},{'id':'r3','decision':'REJECTED exceeds target','qa':'cover-qa-r3.json'}]},ensure_ascii=False,indent=2))
print('Prepared static cover evidence and pending review manifest')
