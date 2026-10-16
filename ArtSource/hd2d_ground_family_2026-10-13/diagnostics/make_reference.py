"""Unchanged integer display zoom of admitted native art; not new albedo."""
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[3]
source=root/'UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_b.png'
output=Path(__file__).resolve().parent/'academy_block_court_b_reference_32x.png'
image=Image.open(source).convert('RGBA')
assert image.size==(32,32)
image.resize((1024,1024),Image.Resampling.NEAREST).save(output)
