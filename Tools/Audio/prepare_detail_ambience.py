"""Reproduce the two licensed ambience loops without changing raw sources."""
from pathlib import Path
import hashlib
import json
import subprocess
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "ArtSource/Audio/Details60"
OUTPUT = ROOT / "UnityProject/Assets/Resources/Audio/OCC/Ambience"
SELECT = {
    "wind": SOURCE / "wind.wav",
    "room": SOURCE / "rooms/TheShopCollection_convenience_store_drinks_fridge_drone.wav",
}

def main():
    missing = [str(path) for path in SELECT.values() if not path.is_file()]
    if missing:
        raise FileNotFoundError(missing)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    records = []
    for name, path in SELECT.items():
        pcm = subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-f", "f32le", "-ac", "1", "-ar", "44100", "pipe:1"], capture_output=True, check=True).stdout
        samples = np.frombuffer(pcm, dtype="<f4").astype(np.float64)
        overlap = min(22050, len(samples) // 4)
        fade = np.linspace(0, 1, overlap, endpoint=False)
        loop = np.concatenate((samples[overlap:-overlap], samples[-overlap:] * (1-fade) + samples[:overlap] * fade))
        gain = min(10**(-28/20) / max(np.sqrt(np.mean(loop**2)), 1e-12), .40/max(np.max(np.abs(loop)), 1e-12))
        loop *= gain
        target = OUTPUT / (name + ".wav")
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", "44100", "-ac", "1", "-i", "pipe:0", "-c:a", "pcm_s16le", str(target)], input=loop.astype("<f4").tobytes(), check=True)
        records.append(dict(name=name, source=path.relative_to(ROOT).as_posix(), output=target.relative_to(ROOT).as_posix(), gain=gain, peak=float(np.max(np.abs(loop))), duration=len(loop)/44100, sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
    (SOURCE / "processing.json").write_text(json.dumps(records, indent=2)+"\n", encoding="utf-8")

if __name__ == "__main__":
    main()
