"""Original OCC score and cues. Requires only NumPy; no external samples."""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "UnityProject/Assets/Resources/Audio/OCC"
SR = 32000
RNG = np.random.default_rng(413)


def tone(note, duration, instrument):
    t = np.arange(max(8, int(duration * SR))) / SR
    f = 440 * 2 ** ((note - 69) / 12)
    phase = 2 * np.pi * f * t
    if instrument == "bell":
        y = sum(a * np.sin(phase * r) * np.exp(-t * d) for r, a, d in
                [(1, 1, 1.6), (2.756, .22, 3.5), (5.404, .07, 6)])
        env = np.minimum(t / .008, 1)
    elif instrument == "pluck":
        y = sum(np.sin(phase * k) * np.exp(-t * (.9 + k * 1.7)) / k ** 1.15 for k in range(1, 10))
        env = np.minimum(t / .003, 1)
    elif instrument == "piano":
        y = sum(np.sin(phase * k * (1 + .00014 * k * k)) * np.exp(-t * (.8 + k * .45)) /
                k ** 1.8 for k in range(1, 9))
        env = np.minimum(t / .006, 1)
    elif instrument == "flute":
        vibrato = .012 * np.sin(2 * np.pi * 4.8 * t) * np.minimum(t / .35, 1)
        p = phase + vibrato
        y = np.sin(p) + .10 * np.sin(2 * p) + .025 * np.sin(3 * p)
        breath = RNG.normal(0, .035, len(t))
        breath = np.convolve(breath, np.ones(9) / 9, mode="same")
        y += breath
        env = np.minimum(t / .11, 1) * np.minimum((duration - t) / .16, 1)
    elif instrument == "strings":
        y = sum((np.sin(phase * k) + .5 * np.sin(phase * k * 1.0015 + .4)) /
                k ** 2.2 for k in range(1, 6))
        env = np.minimum(t / .45, 1) * np.minimum((duration - t) / .5, 1)
    else:  # Soft tuned drum with a small wooden attack.
        p = 2 * np.pi * (f * t + 22 * (1 - np.exp(-t * 20)) / 20)
        y = np.sin(p) * np.exp(-t * 8) + RNG.normal(0, .13, len(t)) * np.exp(-t * 70)
        env = np.minimum(t / .003, 1)
    # Every note ends at zero. Tails are wrapped into the start of the loop.
    env *= np.minimum((duration - t) / .025, 1)
    return y * np.maximum(env, 0)


def place(mix, note, start, duration, gain, instrument, pan=0):
    y = tone(note, duration, instrument) * gain
    indices = (np.arange(len(y)) + round(start * SR)) % len(mix)
    mix[indices, 0] += y * np.sqrt((1 - pan) / 2)
    mix[indices, 1] += y * np.sqrt((1 + pan) / 2)


def ambience(mix, loop=True):
    dry = mix.copy()
    for delay, amount in [(.073, .10), (.137, .12), (.271, .08), (.409, .06), (.613, .035)]:
        shift = round(delay * SR)
        if loop:
            mix += np.roll(dry[:, ::-1], shift, axis=0) * amount
        else:
            mix[shift:] += dry[:-shift, ::-1] * amount
    return mix


def save(name, mix, music=False, loop=False):
    mix = ambience(mix, loop)
    rms = np.sqrt(np.mean(mix ** 2))
    target = .105 if music else .16
    mix *= min(target / max(rms, 1e-8), .79 / max(np.max(np.abs(mix)), 1e-8))
    if loop:
        # Remove the last-sample boundary step without a silence gap.
        n = 128
        delta = mix[-1] - mix[0]
        mix[-n:] -= np.linspace(0, 1, n)[:, None] * delta
    else:
        mix[:160] *= np.linspace(0, 1, 160)[:, None]
        mix[-640:] *= np.linspace(1, 0, 640)[:, None]
    mix = np.clip(mix, -.89, .89)
    path = (ROOT / "ArtSource/Audio/OriginalCandidates" / Path(name).name) if music else OUT / name
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((mix * 32767).astype("<i2").tobytes())
    return dict(file=str(path.relative_to(ROOT)).replace("\\", "/"),
                seconds=round(len(mix) / SR, 3), sample_rate=SR, channels=2,
                peak_dbfs=round(20 * np.log10(np.max(np.abs(mix))), 2),
                rms_dbfs=round(20 * np.log10(np.sqrt(np.mean(mix ** 2))), 2),
                loop_boundary_delta=float(np.max(np.abs(mix[-1] - mix[0]))) if loop else None,
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())


# D Dorian: bright sixth above a minor home chord. A single theme ties the three rooms together.
CHORDS = [(50, [62, 65, 69, 76]), (53, [60, 65, 69, 76]),
          (48, [60, 64, 67, 74]), (55, [62, 67, 71, 76]),
          (50, [62, 65, 69, 74]), (57, [60, 64, 69, 74]),
          (53, [60, 65, 69, 74]), (48, [60, 64, 67, 74])]
MELODY = [[74, 76, 81], [79, 77, 76], [74, 72, 69], [71, 74, 76],
          [77, 81, 86], [84, 81, 79], [77, 76, 74], [72, 76, 74]]


def score(name, bpm, beats, combat=False, archive=False):
    beat = 60 / bpm
    length = 32 * beats * beat
    mix = np.zeros((round(length * SR), 2))
    for bar in range(32):
        root, chord = CHORDS[bar % 8]
        start = bar * beats * beat
        section = bar // 8
        for j, note in enumerate(chord):
            place(mix, note - 12, start, beats * beat + .8, .045 if archive else .065, "strings", (j - 1.5) * .20)
        place(mix, root - (12 if combat else 0), start, beat * 2.6, .15, "piano", -.12)
        steps = beats * (2 if combat or not archive else 1)
        for step in range(steps):
            note = chord[(step + section) % len(chord)] + (12 if step % 4 == 3 else 0)
            place(mix, note, start + step * beats * beat / steps,
                  beat * 1.4, (.10 if archive else .14) * RNG.uniform(.85, 1.05),
                  "piano" if archive else "pluck", -.35 if step % 2 == 0 else .35)
        melody = MELODY[bar % 8]
        if section != 0 or bar % 2 == 0:
            for j, note in enumerate(melody):
                offset = j * beats / 3
                note += (12 if section == 2 and archive else 0)
                place(mix, note, start + offset * beat, beat * beats / 3 * .88,
                      .09 if archive else .13, "bell" if archive else "flute", .13)
        if bar % 4 == 3:
            for j, note in enumerate([chord[1] + 12, chord[2] + 12, chord[3] + 12]):
                place(mix, note, start + (beats - .75 + j * .25) * beat, 1.2, .055, "bell", -.25)
        if combat:
            for offset in [0, 1.5, 2, 3.5]:
                place(mix, 33, start + offset * beat, .55, .18 if offset in [0, 2] else .10, "drum")
            for offset in [1, 3]:
                place(mix, 57, start + offset * beat, .16, .065, "drum", .22)
    return save("Music/" + name + ".wav", mix, music=True, loop=True)


def cue(name, notes, duration, instrument="bell", paper=False):
    mix = np.zeros((round(duration * SR), 2))
    for note, start, length, gain in notes:
        place(mix, note, start, length, gain, instrument)
    if paper:
        t = np.arange(len(mix)) / SR
        noise = RNG.normal(0, 1, len(mix))
        noise = noise - np.convolve(noise, np.ones(20) / 20, mode="same")
        envelope = np.exp(-t * 34) * np.minimum(t / .004, 1)
        mix += (noise * envelope * .30)[:, None]
    return save("SFX/" + name + ".wav", mix)


def main():
    records = [score("archive_lamplight", 76, 4, archive=True),
               score("academy_courtyard", 88, 3), score("aether_practicum", 116, 4, combat=True)]
    records += [cue("archive_select", [(86, 0, .28, .15)], .42),
                cue("archive_confirm", [(38, 0, .15, .40), (74, .075, .45, .17), (81, .14, .50, .10)], .95, paper=True),
                cue("archive_reject", [(62, 0, .25, .18), (61, .13, .27, .15)], .65, instrument="piano"),
                cue("aether_ready", [(74, 0, .5, .18), (81, .08, .6, .15), (86, .17, .7, .10)], 1.1),
                cue("archive_reward", [(74, 0, .7, .2), (77, .13, .8, .18), (81, .26, .9, .17), (86, .42, 1, .12)], 1.6),
                cue("practicum_complete", [(62, 0, 1.3, .14), (69, 0, 1.3, .12), (74, .12, 1.2, .18),
                                           (77, .32, 1.2, .18), (81, .55, 1.4, .17)], 2.4),
                cue("practicum_retry", [(65, 0, 1.2, .15), (62, .34, 1.4, .15), (57, .7, 1.4, .14)], 2.4, "piano")]
    manifest = ROOT / "ArtSource/Audio/occ_original_score_2026-10-01.json"
    manifest.parent.mkdir(parents=True, exist_ok=True)
    manifest.write_text(json.dumps(dict(authoring="Original synthesized OCC composition; no external audio samples",
        source="Tools/Audio/compose_occ_score.py", seed=413, assets=records), indent=2), encoding="utf-8")
    print(json.dumps(records, indent=2))


if __name__ == "__main__":
    main()
