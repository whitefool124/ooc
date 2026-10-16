from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
cases=[('Aether intact: local Play preview','PrismCandidate/full_output_offscreen_960x540.png'),('Aether damaged: constructed render fixture','PrismCandidate/constructed_damage_fixture_960x540.png'),('Pressure intact: local Play preview','PressureCandidate/full_output_offscreen_960x540.png'),('Pressure damaged: constructed render fixture','PressureCandidate/constructed_damage_fixture_960x540.png')]
board=Image.new('RGB',(1920,1140),(24,24,24));draw=ImageDraw.Draw(board)
for i,(title,path) in enumerate(cases):
 x=i%2*960;y=i//2*570
 im=Image.open(OUT/'profiles'/path).convert('RGB');assert im.size==(960,540)
 board.paste(im,(x,y+30));draw.text((x+12,y+9),title,fill='white')
board.save(OUT/'crystal_four_state_review.png')
