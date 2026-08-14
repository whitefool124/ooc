"""Make strict 24px review candidates from separately generated redraw sources."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).parent; OUT=ROOT/'Items24'/'Fruits_v02'; QA=ROOT/'QA'
OUT.mkdir(parents=True,exist_ok=True); QA.mkdir(exist_ok=True)
SOURCES={
 'blueberries':Path(r'C:\Users\FNHF\.codex\generated_images\019ff714-5e0a-7ed0-8cc2-b42cf7aa433c\exec-2c88c7ea-5048-44a8-8483-1f6484dc533e.png'),
 'strawberry':Path(r'C:\Users\FNHF\.codex\generated_images\019ff714-5e0a-7ed0-8cc2-b42cf7aa433c\exec-0631ee0f-506c-4eb3-9180-2923267f1a24.png'),
}
def transparent_white(im):
 im=im.convert('RGBA'); p=im.load()
 for y in range(im.height):
  for x in range(im.width):
   r,g,b,a=p[x,y]
   if min(r,g,b)>242 and max(r,g,b)-min(r,g,b)<12:p[x,y]=(0,0,0,0)
 return im
def convert(src):
 raw=transparent_white(Image.open(src)); raw=raw.crop(raw.getbbox()); raw.thumbnail((22,22),Image.Resampling.BOX)
 rgb=raw.convert('RGB').quantize(colors=10,method=Image.Quantize.MEDIANCUT).convert('RGBA'); out=Image.new('RGBA',raw.size,(0,0,0,0));out.paste(rgb,mask=raw.getchannel('A'))
 cell=Image.new('RGBA',(24,24),(0,0,0,0));cell.alpha_composite(out,((24-out.width)//2,(24-out.height)//2));return cell
def main():
 ims=[]
 for name,source in SOURCES.items():
  im=convert(source); im.save(OUT/f'occ_fruit_{name}_24_v02_redraw.png');ims.append((name,im))
 board=Image.new('RGBA',(600,370),'#16161c'); d=ImageDraw.Draw(board); f=ImageFont.load_default()
 for i,(name,im) in enumerate(ims):
  x=i*300;d.text((x+12,12),name,fill='#e9e5dc',font=f);d.text((x+12,30),'separate redraw / native 24x24',fill='#aaa7b1',font=f)
  for yy in range(24):
   for xx in range(24):d.point((x+138+xx,60+yy),fill='#383842'if(xx+yy)%2 else'#2b2b34')
  board.alpha_composite(im,(x+138,60));board.alpha_composite(im.resize((288,288),Image.Resampling.NEAREST),(x+6,85))
 board.save(QA/'fruits24_v02_redraw_overview.png')
if __name__=='__main__':main()
