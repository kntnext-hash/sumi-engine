"""Read-only source WAV audit; outputs only mobile_audit/ reports and seam previews.
Requires Python + NumPy. Does not invoke generator, Unity, or audio playback.
"""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'mobile_audit'


def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def db(x):
    return float(20*np.log10(max(float(x), 1e-15)))


def rms(x):
    return float(np.sqrt(np.mean(x*x)))


def preview(name, x, rate):
    pcm = np.rint(x*32768).clip(-32768,32767).astype('<i2')
    with wave.open(str(OUT/name), 'wb') as w:
        w.setnchannels(1 if pcm.ndim == 1 else pcm.shape[1])
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(pcm.tobytes())


def main():
    OUT.mkdir(exist_ok=True)
    paths = sorted(ROOT.glob('*.wav'))
    assert len(paths) == 8, 'Expected the eight delivered sources'
    before = {p.name: digest(p) for p in paths}
    original = json.loads((ROOT/'validation.json').read_text(encoding='utf-8'))
    known = {r['file']:r['sha256'] for r in original['results']}
    rows = []
    for p in paths:
        with wave.open(str(p), 'rb') as w:
            rate, channels = w.getframerate(), w.getnchannels()
            assert w.getsampwidth() == 2 and rate == 48000
            raw = w.readframes(w.getnframes())
        x = np.frombuffer(raw, dtype='<i2').reshape(-1,channels).astype(float)/32768
        mono = x.mean(axis=1)  # equal amplitude mean; mono sources remain unchanged
        reference = rms(x)  # sqrt((L^2+R^2)/2), not unscaled L+R
        window = rate//10
        changes = []
        for start in range(0, len(x), window):
            ref = rms(x[start:start+window])
            if db(ref) >= -60:
                changes.append(db(rms(mono[start:start+window])/ref))
        row = dict(file=p.name, sha256=before[p.name], matches_delivery_hash=before[p.name]==known[p.name],
                   seconds=len(x)/rate, channels=channels, reference_rms_dbfs=db(reference),
                   mono_rms_dbfs=db(rms(mono)), mono_change_db=db(rms(mono)/reference),
                   worst_active_100ms_change_db=min(changes), active_windows=len(changes),
                   mono_peak_dbfs=db(np.abs(mono).max()), mono_dc=float(mono.mean()),
                   stereo_correlation=float(np.corrcoef(x.T)[0,1]) if channels==2 else None,
                   sample_clip_count=int(np.count_nonzero(np.abs(x)>=32767/32768)))
        row['mono_screen_pass'] = row['mono_change_db'] >= -1 and row['worst_active_100ms_change_db'] >= -3
        if p.name.startswith('bgm_'):
            variants = {'stereo':x, 'mono':mono[:,None]}
            joins = {}
            for label, a in variants.items():
                ring = np.concatenate([a[-2:],a[:2]])
                step = float(np.abs(a[0]-a[-1]).max())
                curvature = float(np.abs(np.diff(ring,n=2,axis=0)).max())
                interior_step = float(np.abs(np.diff(a,axis=0)).max())
                interior_curvature = float(np.abs(np.diff(a,n=2,axis=0)).max())
                joins[label] = dict(seam_step_fs=step, seam_step_dbfs=db(step),
                    interior_max_step_fs=interior_step, seam_curvature_fs=curvature,
                    interior_max_curvature_fs=interior_curvature,
                    numerical_screen_pass=step<=interior_step and curvature<=interior_curvature)
                preview(f'bgm_seam_6s_{label}.wav', np.concatenate([a[-3*rate:],a[:3*rate]]), rate)
            # Process all 23 decoded cycles (30m40s), retaining only tiny seam records.
            # This tests PCM concatenation, not hardware timing or Unity scheduling.
            boundaries = []
            count = 0
            energy = np.zeros(channels)
            totals = np.zeros(channels)
            last = None
            for cycle in range(23):
                for start in range(0,len(x),4093):
                    block = x[start:start+4093]
                    if cycle and start == 0:
                        boundaries.append(dict(cycle=cycle, frame=count,
                            seconds=count/rate, step_fs=float(np.abs(block[0]-last).max())))
                    energy += np.sum(block*block,axis=0)
                    totals += block.sum(axis=0)
                    count += len(block)
                    last = block[-1]
            assert len(boundaries)==22 and count==23*len(x)
            assert all(b['frame']==b['cycle']*len(x) for b in boundaries)
            assert all(b['step_fs']==joins['stereo']['seam_step_fs'] for b in boundaries)
            row['loop_audit'] = dict(cycles=23, seconds=count/rate, frames=count,
                boundaries=boundaries, seam_metrics=joins,
                repeated_dc_per_channel=(totals/count).tolist(),
                repeated_rms_dbfs_per_channel=[db(v) for v in np.sqrt(energy/count)],
                method='Offline decoded PCM streaming concatenation, 4093-frame blocks; no real-time playback')
        rows.append(row)
    after = {p.name:digest(p) for p in paths}
    assert before==after, 'Source WAV changed during audit'
    report = dict(date='2026-10-23', source_wavs_unchanged=True,
        listening='NOT PERFORMED; no auditory input tool available',
        thresholds='Project screening only: mean mono change >= -1 dB, active 100 ms >= -3 dB (reference >= -60 dBFS). Not a listening or device standard.',
        mono_definition='(L+R)/2; reference RMS sqrt(mean of all channel squared samples); mono files identity', results=rows)
    (OUT/'report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    for r in rows:
        print(f"{r['file']}: mono {r['mono_change_db']:.5f} dB, worst 100ms {r['worst_active_100ms_change_db']:.5f} dB, pass={r['mono_screen_pass']}")
    print('All eight source hashes unchanged; BGM 23 cycles / 1840 seconds / 22 joins processed.')


if __name__ == '__main__':
    main()
