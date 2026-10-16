from pathlib import Path
from PIL import Image, ImageOps
import json, hashlib, copy

root=Path(__file__).resolve().parents[3]
base=Path(__file__).resolve().parent
template=json.loads((base/'工作托盘/manifest.json').read_text(encoding='utf-8-sig'))
for name,attempt,asset,page in [('药剂提箱','03','hd2d_apothecary_satchel','medical'),('市集天平','01','hd2d_market_balance','shop')]:
    folder=base/name
    qa=json.loads((folder/'qa-final.json').read_text(encoding='utf-8'))
    assert qa['status']=='PASS_LOSSLESS_TARGET'
    art=Image.open(folder/'delivery.png').convert('RGBA')
    for scale in [4,6]:
        art.resize((art.width*scale,art.height*scale),Image.Resampling.NEAREST).save(folder/f'{scale}x.png')
    gray=ImageOps.grayscale(art).convert('RGBA');gray.putalpha(art.getchannel('A'));gray.save(folder/'gray.png')
    enlarged=art.resize((art.width*4,art.height*4),Image.Resampling.NEAREST)
    board=Image.new('RGBA',enlarged.size,(204,204,204,255))
    for y in range(0,board.height,16):
        for x in range(0,board.width,16):
            if (x//16+y//16)%2:board.paste((238,238,238,255),(x,y,min(x+16,board.width),min(y+16,board.height)))
    board.alpha_composite(enlarged);board.save(folder/'checker.png')
    relative=lambda p:str(p.relative_to(root)).replace('\\','/')
    digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
    m=copy.deepcopy(template);m['asset_id']=asset
    m['provenance']['source_path']=relative(folder/f'source-{attempt}.png')
    m['provenance']['source_sha256']=digest(folder/f'source-{attempt}.png')
    m['delivery']['output_path']=m['delivery']['native_output_path']=relative(folder/'delivery.png')
    m['delivery']['output_sha256']=digest(folder/'delivery.png')
    m['decoder']=qa
    for key,file in [('one_x','delivery.png'),('four_x','4x.png'),('six_x','6x.png'),('grayscale','gray.png'),('checker','checker.png')]:m['evidence'][key]=relative(folder/file)
    capture=f'UnityProject/ReviewEvidence/MainMenuE/specific-{page}.png'
    m['evidence']['application_contact']=capture
    m['human_review']={'overall':'PENDING','notes':'Independent new source; numerical pass is not aesthetic approval.'}
    m['unity_import']['runtime_verified']=False
    m['unity_import']['hd2d_runtime']['capture']=capture
    m['placement_semantics']['native_anchor']=[art.width//2,art.height-1]
    m['placement_semantics']['real_dimensions_m']=[art.width/32,art.height/32]
    m['placement_semantics']['visible_contact_line']['native_px']=art.height-2
    m['placement_semantics']['depth_sort_baseline']=art.height-2
    (folder/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
attempts={}
for name in ['校准工具架','药剂提箱','市集天平','学院餐盘']:
    attempts[name]=[json.loads(p.read_text(encoding='utf-8')) for p in sorted((base/name).glob('qa-0*.json'))]
(base/'专属物件生产记录.json').write_text(json.dumps(attempts,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
