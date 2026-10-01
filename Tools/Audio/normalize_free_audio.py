"""Normalize decoded Unity PCM, leaving downloaded originals untouched."""
from pathlib import Path
import base64, hashlib, json, sys, wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
receipt = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
records = []
for clip in receipt['data']['returnValue']:
    pcm = np.frombuffer(base64.b64decode(clip['pcm']), dtype='<f4').astype(np.float64)
    peak = float(np.max(np.abs(pcm)))
    rms = float(np.sqrt(np.mean(pcm * pcm)))
    gain = min(10 ** (-22 / 20) / max(rms, 1e-8), .80 / max(peak, 1e-8))
    output = ROOT / 'UnityProject/Assets/Resources/Audio/OCC/SFX/FreeProcessed' / (clip['name'] + '.wav')
    output.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(output), 'wb') as writer:
        writer.setnchannels(clip['channels'])
        writer.setframerate(clip['frequency'])
        writer.setsampwidth(2)
        writer.writeframes(np.round(pcm * gain * 32767).astype('<i2').tobytes())
    records.append(dict(source='ArtSource/Audio/FreeLibrary/ImportedOriginals/' + Path(clip['path']).name,
        output=output.relative_to(ROOT).as_posix(), gain=gain,
        rms_db_after=20*np.log10(max(rms*gain, 1e-12)), peak_after=peak*gain,
        sha256=hashlib.sha256(output.read_bytes()).hexdigest()))
(ROOT/'ArtSource/Audio/free_audio_processing.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
print(f'Normalized {len(records)} SFX; peak <= 0.8, target RMS -22 dBFS')
