"""Decode selected CC0 foley, trim silence, normalize and retain provenance."""
from pathlib import Path
import hashlib,json,shutil,subprocess,wave
import numpy as np
ROOT=Path(__file__).resolve().parents[2];RAW=ROOT/'ArtSource/Audio/FoleyExpansion20261003'
OUT=ROOT/'UnityProject/Assets/Resources/Audio/OCC/SFX/Foley';OUT.mkdir(parents=True,exist_ok=True)
SELECT={
 'cloth_pick':'rpg/Audio/cloth1.ogg','cloth_drop':'rpg/Audio/cloth2.ogg',
 'leather_pick':'rpg/Audio/handleSmallLeather.ogg','leather_drop':'rpg/Audio/dropLeather.ogg',
 'metal_pick':'rpg/Audio/metalLatch.ogg','metal_light':'impact/Audio/impactMetal_light_001.ogg',
 'metal_heavy':'impact/Audio/impactMetal_heavy_001.ogg','glass_pick':'rpg/Audio/metalClick.ogg',
 'glass_drop':'impact/Audio/impactGlass_light_001.ogg','paper_pick':'rpg/Audio/bookFlip3.ogg',
 'paper_drop':'rpg/Audio/bookPlace3.ogg','neutral_drop':'impact/Audio/impactGeneric_light_001.ogg',
 'coins':'rpg/Audio/handleCoins2.ogg','forge':'impact/Audio/impactMining_001.ogg',
 'latch':'rpg/Audio/metalLatch.ogg','meal':'impact/Audio/impactPlate_light_001.ogg',
 'reject':'rpg/Audio/clothBelt.ogg','object_break':'impact/Audio/impactWood_heavy_001.ogg',
 'object_hit':'impact/Audio/impactGeneric_light_004.ogg',
 'defeat':'impact/Audio/impactSoft_heavy_001.ogg',
 'stone_0':'impact/Audio/footstep_concrete_000.ogg','stone_1':'impact/Audio/footstep_concrete_001.ogg','stone_2':'impact/Audio/footstep_concrete_002.ogg',
 'water_0':'splash1.wav','water_1':'splash2.wav',
}
SELECT.update({
 'room_shop':'rpg/Audio/doorOpen_1.ogg','room_workshop':'rpg/Audio/metalPot1.ogg',
 'room_medical':'rpg/Audio/cloth4.ogg','shop_exit':'impact/Audio/impactBell_heavy_001.ogg',
 'tab_tools':'rpg/Audio/handleSmallLeather2.ogg','tab_paper':'rpg/Audio/bookFlip1.ogg',
 'meal_cup':'impact/Audio/impactGlass_light_002.ogg','meal_bowl':'impact/Audio/impactPlate_light_002.ogg',
 'meal_plate':'impact/Audio/impactPlate_light_003.ogg','return_soft':'rpg/Audio/cloth3.ogg',
 'push':'impact/Audio/impactSoft_medium_001.ogg','pull':'rpg/Audio/handleCoins.ogg',
 'collision':'impact/Audio/impactMetal_medium_001.ogg','preview':'rpg/Audio/metalClick.ogg',
})
records=[]
for relative in SELECT.values():
 if not (RAW/relative).is_file():raise FileNotFoundError(relative)
for name,rel in SELECT.items():
 source=RAW/rel
 pcm=subprocess.run([shutil.which('ffmpeg'),'-v','error','-i',str(source),'-ac','1','-ar','44100','-f','f32le','-'],stdout=subprocess.PIPE,check=True).stdout
 samples=np.frombuffer(pcm,dtype='<f4').copy();peak=float(np.max(abs(samples)))
 active=np.flatnonzero(abs(samples)>max(.0001,peak*.001))
 if not len(active):raise ValueError('Silent '+name)
 lo=max(0,int(active[0])-220);hi=min(len(samples),int(active[-1])+441)
 samples=samples[lo:hi];fade=min(128,len(samples)//4);samples[:fade]*=np.linspace(0,1,fade);samples[-fade:]*=np.linspace(1,0,fade)
 rms=float(np.sqrt(np.mean(samples*samples)));gain=min(10**(-22/20)/max(rms,1e-8),.78/max(float(np.max(abs(samples))),1e-8))
 samples*=gain;output=OUT/(name+'.wav')
 with wave.open(str(output),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(44100);w.writeframes(np.round(samples*32767).astype('<i2').tobytes())
 records.append(dict(cue=name,source=source.relative_to(ROOT).as_posix(),output=output.relative_to(ROOT).as_posix(),gain=gain,trim_seconds=[lo/44100,hi/44100],duration=len(samples)/44100,peak=float(np.max(abs(samples))),rms_db=20*np.log10(max(float(np.sqrt(np.mean(samples*samples))),1e-12)),sha256=hashlib.sha256(output.read_bytes()).hexdigest()))
(RAW/'processing.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(f'{len(records)} mono cues prepared; peak <=0.78. Raw clips unchanged.')
