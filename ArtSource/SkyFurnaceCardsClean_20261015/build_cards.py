"""User-authorized reference-to-card production, NOT lossless source decoding.

The generated image supplies the design. This scoped route explicitly constructs
one native canvas and cuts complementary parts; it does not claim a source-grid PASS.
"""
from pathlib import Path
import sys,json,hashlib,math
from collections import deque
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/OCCArt'))
from decode_generated_pixel_grid import consolidate_to_observed_palette
FOLDER=Path(__file__).resolve().parent
SOURCE=FOLDER/'raw/modular_kit.png'
def rel(p):return p.relative_to(ROOT).as_posix()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
source=Image.open(SOURCE).convert('RGBA')
# New art supplies complete separate components; no circular masks split any glyph.
source_width,source_height=source.size
layers={};removed=0;production=[]
for index,(name,canvas_size) in enumerate([('outer',160),('sigils',128),('core',96)]):
 left=round(index*source_width/3);right=round((index+1)*source_width/3)
 panel=source.crop((left,0,right,source_height));alpha=panel.getchannel('A').point(lambda a:255 if a>=220 else 0);panel.putalpha(alpha)
 bounds=alpha.getbbox();part=panel.crop(bounds)
 # Uniform native production grid, actual world pitch1/32m; target shape sizes are authored here, not stretched at runtime.
 available=canvas_size-12;ratio=min(available/part.width,available/part.height)
 size=(round(part.width*ratio),round(part.height*ratio));part=part.resize(size,Image.Resampling.NEAREST)
 part=consolidate_to_observed_palette(part,12);im=Image.new('RGBA',(canvas_size,canvas_size));im.alpha_composite(part,((canvas_size-size[0])//2,(canvas_size-size[1])//2))
 pixels=im.load();seen=set();components=[]
 for y in range(canvas_size):
  for x in range(canvas_size):
   if not pixels[x,y][3] or (x,y) in seen:continue
   q=deque([(x,y)]);seen.add((x,y));component=[]
   while q:
    a,b=q.popleft();component.append((a,b))
    for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
     n=(a+dx,b+dy)
     if 0<=n[0]<canvas_size and 0<=n[1]<canvas_size and n not in seen and pixels[n][3]:seen.add(n);q.append(n)
   if len(component)<6:
    for a,b in component:pixels[a,b]=(0,0,0,0);removed+=1
   else:components.append(len(component))
 layers[name]=im;production.append({'layer':name,'source_panel':[left,0,right,source_height],'subject_bounds':bounds,'native_canvas':canvas_size,'native_subject':size,'world_size':canvas_size/32,'kept_component_areas':components})
# The sun is a complete central design component with a clear surrounding gap.
core=layers['core'];canvas=Image.new('RGBA',(64,64))
for y in range(64):
 for x in range(64):
  if math.hypot(x-31.5,y-31.5)<=13.5:canvas.putpixel((x,y),core.getpixel((x+16,y+16)))
layers['caster']=canvas
for name,im in layers.items():
 output=FOLDER/f'cards/{name}.png';im.save(output)
 qa=FOLDER/f'qa/cards/{name}';qa.mkdir(parents=True,exist_ok=True)
 im.save(qa/'1x.png');im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST).save(qa/'4x.png')
 gray=im.convert('L').convert('RGBA');gray.putalpha(im.getchannel('A'));gray.save(qa/'grayscale.png')
 checker=Image.new('RGBA',im.size)
 for y in range(im.height):
  for x in range(im.width):checker.putpixel((x,y),(190,190,190,255) if (x//4+y//4)%2 else (230,230,230,255))
 checker.alpha_composite(im);checker.save(qa/'checker.png')
 write(FOLDER/f'{name}.occ-art-manifest-v1.json',{
  'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':'sky_furnace_clean_card_'+name,'role':'hd2d_environment_module','status':'QA_PENDING',
  'provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'User2026-10-15: image supplies aesthetic design; produce and cut layered pixel cards. Reference-derived processing, NOT lossless lattice decode.','source_path':rel(SOURCE),'source_sha256':sha(SOURCE)},
  'delivery':{'output_path':rel(output),'native_output_path':rel(output),'output_sha256':sha(output),'native_ppu':32,'palette_max':12,'required_color_families':[]},
  'application':{'rendering_mode':'battlefield_hd2d_environment','camera_projection':'perspective','runtime_draw_rect':f'{im.width/32}m x {im.height/32}m at32PPU, no layer-specific texel scaling','default_integer_scale':1,'minimum_integer_scale':1,'integration_authorization':'Explicit reference-to-paper-card construction route authorized by user; isolated cinematic only'},
  'evidence':dict({k:rel(qa/f) for k,f in [('one_x','1x.png'),('four_x','4x.png'),('grayscale','grayscale.png'),('checker','checker.png')]},application_contact='Worldbuilding/归档/2026-10-15_天穹熔炉纸板精修/fixed_frames/frame_04.50.png'),
  'human_review':{'overall':'PENDING','reviewer':'','date':'','notes':'Reference original did not satisfy strict generated-grid route. This is a separate user-authorized production route. Current scene aesthetics/density remain pending.'},
  'unity_import':{'asset_path':'Assets/Game/Resources/Art/SkyFurnace/magic_card_'+name+'.png','formal_promotion':False}})
write(FOLDER/'card_production.json',{'route':'reference-derived native card construction','source':rel(SOURCE),'source_sha256':sha(SOURCE),'script_sha256':sha(Path(__file__)),
 'source_grid_confidence_inherited':False,'lossless_source_decode':False,'geometric_resampling_performed':True,'resampling':'Complete separate source panels, authored160/128/96native canvases, nearest neighbour; not source-lattice decoding','panels':production,'new_colours_introduced':False,
 'palette_limit':12,'hard_alpha_threshold':220,'background_speck_pixels_removed':removed,'layer_partition':'Three complete separate generated components, no arbitrary radial splitting; central sun cropped in clear gap','world_pitch':1/32,'human_review':'PENDING'})
print('Produced four reference-derived cards at32PPU; QA_PENDING, NOT source-grid/lossless PASS.')
