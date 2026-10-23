"""Original six-weapon synthesis, Python 3 + NumPy. No external audio inputs.
Writes only alongside this script; existing audio/ is never accessed or modified.
Run: python art-source/audio-v2/generate_weapon_audio.py
"""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

ROOT = Path(__file__).resolve().parent
SR = 48000
SEED = 20261024
SPECS = [('Point','点墨',.14,-21), ('Pierce','貫筆',.23,-20),
         ('Bleed','滲墨',.34,-22), ('Sweep','払い',.28,-21),
         ('Enso','円相',.32,-21), ('Resonance','共鳴',.38,-22)]


def db(x):
    return float(20*np.log10(max(float(x),1e-15)))


def render(index, kind, seconds, peak):
    rng = np.random.default_rng(SEED + index)
    t = np.arange(round(seconds*SR))/SR
    noise = rng.normal(size=len(t))
    # Finite smoothing kernels reduce high-frequency hiss without sampled material.
    fine = np.convolve(noise, np.ones(15)/15, mode='same')
    soft = np.convolve(noise, np.ones(65)/65, mode='same')
    def drop(f, fall, decay):
        return np.sin(2*np.pi*(f*t + fall*.025*(1-np.exp(-t/.025))))*np.exp(-t/decay)
    if kind == 'Point':
        x = .75*fine*np.exp(-t/.017) + .15*drop(175,100,.025)
    elif kind == 'Pierce':
        x = fine*np.exp(-t/.052) + .11*drop(235,150,.043)
    elif kind == 'Bleed':
        x = soft*(1-np.exp(-t/.022))*np.exp(-t/.075)*(1+.22*np.sin(2*np.pi*31*t))
        x += .035*drop(105,25,.065)
    elif kind == 'Sweep':
        x = (fine*.32+soft)*np.exp(-((t-.075)/.058)**2)
    elif kind == 'Enso':
        x = soft*(np.exp(-((t-.035)/.019)**2)+.65*np.exp(-((t-.12)/.03)**2))
        x += .06*drop(155,18,.075)
    else:
        x = np.zeros_like(t)
        for onset, gain, freq in [(0,1,195),(.065,.55,163),(.135,.30,137)]:
            u = np.maximum(0,t-onset)
            env = (1-np.exp(-u/.003))*np.exp(-u/.035)*(t>=onset)
            x += gain*env*(.22*np.sin(2*np.pi*freq*u)+soft)
    fade = np.ones(len(t))
    attack = round(.003*SR)
    release = round(.025*SR)
    fade[:attack] = np.linspace(0,1,attack)**2
    fade[-release:] = np.linspace(1,0,release)**2
    x *= fade
    # Remove DC with an endpoint-zero window, preserving silence at both ends.
    window = np.sin(np.linspace(0,np.pi,len(t)))**2
    x -= window*x.mean()/window.mean()
    x *= 10**(peak/20)/np.abs(x).max()
    return np.rint(x*32767).astype('<i2')


def main():
    rows, decoded = [], []
    for index,(kind,japanese,seconds,peak) in enumerate(SPECS):
        pcm = render(index,kind,seconds,peak)
        assert np.array_equal(pcm,render(index,kind,seconds,peak))
        path = ROOT/f'sfx_attack_{kind.lower()}.wav'
        with wave.open(str(path),'wb') as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
            w.writeframes(pcm.tobytes())
        with wave.open(str(path),'rb') as w:
            x = np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
        decoded.append(x)
        # 10 s of 80 ms triggers; <= 4 concurrent voices, oldest replaced.
        train = np.zeros(10*SR)
        interval = round(.08*SR)
        for start in range(0,len(train),interval):
            size = min(len(x),4*interval,len(train)-start)
            train[start:start+size] += x[:size]
        row = dict(weapon_kind=kind,name=japanese,file=path.name,seconds=len(x)/SR,
            peak_dbfs=db(np.abs(x).max()),rms_dbfs=db(np.sqrt(np.mean(x*x))),
            dc_fs=float(x.mean()),first_sample=float(x[0]),last_sample=float(x[-1]),
            max_adjacent_step_fs=float(np.abs(np.diff(x)).max()),
            clip_samples=int(np.count_nonzero(np.abs(x)>=32767/32768)),
            four_coincident_peak_dbfs=db(4*np.abs(x).max()),
            rapid_80ms_four_voice_peak_dbfs=db(np.abs(train).max()),
            rapid_80ms_clip_samples=int(np.count_nonzero(np.abs(train)>=1)),
            deterministic_repeat=True,sha256=hashlib.sha256(path.read_bytes()).hexdigest())
        assert row['clip_samples']==0 and row['rapid_80ms_clip_samples']==0
        assert abs(row['dc_fs'])<1e-5 and x[0]==0 and x[-1]==0
        rows.append(row)
    # Triangle inequality covers ANY four of these six assets with arbitrary offsets,
    # assuming per-voice gain <= 1. Other sounds/BGM are not part of this bound.
    bound = 4*max(np.abs(x).max() for x in decoded)
    assert bound<1
    report = dict(date='2026-10-24',sample_rate=SR,format='PCM16 mono',seed=SEED,
        numpy_version=np.__version__,listening='NOT PERFORMED',
        arbitrary_four_voice_upper_bound_fs=bound,arbitrary_four_voice_upper_bound_dbfs=db(bound),
        overlap_method='10s train, 80ms triggers, oldest replaced at 4 voices; plus coherent 4-copy and triangle-inequality bound. No BGM/notifications/master gain modeled.',
        results=rows)
    (ROOT/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    for r in rows:
        print(f"{r['weapon_kind']}: peak {r['peak_dbfs']:.2f}, 80ms {r['rapid_80ms_four_voice_peak_dbfs']:.2f}, DC {r['dc_fs']:.9f}")
    print(f'Any 4 attack voices upper bound: {db(bound):.2f} dBFS')


if __name__ == '__main__':
    main()
