"""Original BOKUMYAKU procedural audio. Python 3 + NumPy; no sampled assets.
Run from any directory: python generate_audio.py
Writes deterministic 48 kHz PCM16 WAVs and measured validation.json beside script.
"""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

ROOT = Path(__file__).resolve().parent
SR = 48000
RNG = np.random.default_rng(20261023)


def time(duration):
    return np.arange(round(duration * SR)) / SR


def soften(x, fade=.008):
    n = min(round(fade * SR), len(x) // 2)
    x = x.copy()
    x[:n] *= np.linspace(0, 1, n) ** 2
    x[-n:] *= np.linspace(1, 0, n) ** 2
    return x


def pluck(freq, duration, decay=1.0):
    t = time(duration)
    x = sum(a * np.sin(2 * np.pi * freq * r * t) * np.exp(-t * (1 + r * .25) / decay)
            for r, a in [(1, 1), (2.003, .25), (3.008, .12), (4.013, .04)])
    return soften(x)


def paper(duration, decay):
    t = time(duration)
    noise = RNG.normal(size=len(t))
    # Smooth noise keeps frequent attacks from becoming sharp hiss.
    noise = np.convolve(noise, np.ones(19) / 19, mode='same')
    return soften(noise * np.exp(-t / decay), .004)


def event_mix(duration, events):
    x = np.zeros(round(duration * SR))
    for onset, sound, gain in events:
        i = round(onset * SR)
        n = min(len(sound), len(x) - i)
        x[i:i+n] += sound[:n] * gain
    return soften(x)


def finish(x, peak_db, loop=False):
    x = x - np.mean(x, axis=0)
    if not loop:
        if x.ndim == 1:
            x = soften(x)
        else:
            x = np.stack([soften(c) for c in x.T], axis=1)
        window = np.sin(np.linspace(0, np.pi, len(x))) ** 2
        if x.ndim > 1:
            window = window[:, None]
        x -= window * (np.mean(x, axis=0) / np.mean(window, axis=0))
    x *= 10 ** (peak_db / 20) / max(np.max(np.abs(x)), 1e-12)
    return np.round(x * 32767).astype('<i2')


def write(name, pcm, loop=False):
    path = ROOT / (name + '.wav')
    with wave.open(str(path), 'wb') as w:
        w.setnchannels(1 if pcm.ndim == 1 else pcm.shape[1])
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    # Validate decoded delivery bytes, not just the floating point mix.
    with wave.open(str(path), 'rb') as w:
        channels = w.getnchannels()
        a = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').reshape(-1, channels).astype(float) / 32768
    peak = float(np.max(np.abs(a)))
    rms = float(np.sqrt(np.mean(a*a)))
    delta = np.abs(np.diff(a, axis=0))
    seam = float(np.max(np.abs(a[0] - a[-1])))
    stats = dict(file=path.name, seconds=len(a)/SR, sample_rate=SR, channels=channels,
                 peak_dbfs=20*np.log10(peak), rms_dbfs=20*np.log10(rms),
                 dc_per_channel=np.mean(a, axis=0).tolist(),
                 clipped_samples=int(np.count_nonzero(np.abs(a) >= 32767/32768)),
                 seam_step=seam, max_interior_step=float(delta.max()),
                 first_samples=a[0].tolist(), last_samples=a[-1].tolist(),
                 sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    assert stats['clipped_samples'] == 0
    assert max(abs(v) for v in stats['dc_per_channel']) < 1e-4
    if loop:
        assert seam <= float(delta.max())
        # Cyclic first derivative around the join must stay within the interior envelope.
        ring = np.concatenate([a[-2:], a[:2]])
        stats['seam_curvature'] = float(np.abs(np.diff(ring, n=2, axis=0)).max())
        stats['max_interior_curvature'] = float(np.abs(np.diff(a, n=2, axis=0)).max())
        assert stats['seam_curvature'] <= stats['max_interior_curvature']
    return stats


def main():
    report = []
    sounds = {
        'sfx_click': (paper(.12, .018) + .13 * pluck(740, .12, .025), -13),
        'sfx_purchase': (event_mix(.85, [(0, pluck(440, .8, .18), .5), (.095, pluck(660, .7, .24), .35)]), -10),
        'sfx_attack': (paper(.23, .035) + .24 * pluck(185, .23, .045), -16),
        'sfx_kill': (paper(.38, .05) + .34 * pluck(330, .38, .09), -14),
        'sfx_core_hit': (event_mix(.75, [(0, paper(.75, .075), 1), (0, pluck(98, .75, .18), .8), (.02, pluck(147, .7, .12), .24)]), -8),
        'sfx_defeat': (event_mix(3.6, [(0, pluck(220, 3, .75), .6), (.36, pluck(196, 3, .8), .45), (.8, pluck(146.832, 2.8, .9), .6)]), -9),
        'sfx_research_complete': (event_mix(3.2, [(0, pluck(293.665, 3, .6), .5), (.2, pluck(440, 2.8, .65), .4), (.48, pluck(587.33, 2.7, .8), .3), (.5, pluck(880, 2.6, .7), .08)]), -10),
    }
    for name, (x, peak) in sounds.items():
        report.append(write(name, finish(x, peak)))

    # 80 seconds = 20 bars at 60 BPM / 4 beats. Every tail wraps into the next cycle.
    length = 80
    n = length * SR
    bgm = np.zeros((n, 2))
    # Newly composed, sparse D/E/G/A/C pentatonic phrases, no external melody input.
    score = [(1,62),(4.5,69),(9,67),(14,64),(18.5,62),
             (23,60),(27,67),(32.5,69),(38,64),
             (42,62),(46.5,72),(52,69),(57,67),
             (62,64),(66.5,60),(72,62),(77.5,57)]
    for j, (onset, midi) in enumerate(score):
        note = pluck(440 * 2**((midi-69)/12), 7.5, 1.65) * .12
        pan = [-.3, .2, -.1, .3][j % 4]
        gains = np.sqrt([(1-pan)/2, (1+pan)/2])
        for delay, gain, swap in [(0,1,False),(.173,.16,True),(.347,.10,False),(.613,.065,True)]:
            ids = (round((onset+delay)*SR) + np.arange(len(note))) % n
            bgm[ids] += note[:,None] * (gains[::-1] if swap else gains)[None,:] * gain
    t = time(length)
    # Integer cycle frequencies and modulation periods guarantee periodic pad phase.
    for freq, gain in [(73.4125,.012),(110,.009),(146.825,.005),(220,.003)]:
        freq = round(freq*length)/length
        for channel in range(2):
            phase = channel * .12
            breath = .75 + .25*np.cos(2*np.pi*t/20 + phase)
            bgm[:,channel] += gain * breath * np.sin(2*np.pi*freq*t + phase)
    report.append(write('bgm_sumi_no_ma_80s', finish(bgm, -10, loop=True), loop=True))
    (ROOT/'validation.json').write_text(json.dumps(dict(generator='generate_audio.py', seed=20261023,
        numpy_version=np.__version__, format='PCM16 little-endian WAV', listening='Not performed; signal validation only',
        results=report), ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    for item in report:
        print(f"{item['file']}: {item['seconds']:.2f}s peak {item['peak_dbfs']:.2f} dBFS, RMS {item['rms_dbfs']:.2f} dBFS, DC {max(abs(v) for v in item['dc_per_channel']):.8f}")


if __name__ == '__main__':
    main()
