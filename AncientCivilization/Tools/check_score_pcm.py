"""Decode actual game BGM. Requires ffmpeg, numpy and scipy.

python3 AncientCivilization/Tools/check_score_pcm.py --json /tmp/after.json
Use --music to inspect an archived mix, --catalog for a separate source checkout,
or --cue ID to inspect individual pieces. --before compares level-matched samples. Mix measurements describe the master,
not isolated instruments. The attack overlap is a comparison aid, not a pass mark.
"""
import argparse
import json
import re
import subprocess
from pathlib import Path

import numpy as np
from scipy import fft, signal

from check_feedback_audio import music_stream_info

SAMPLE_RATE = 44100
PROJECT = Path(__file__).resolve().parents[1]


def db(amplitude):
    return 20 * np.log10(max(float(amplitude), 1e-10))


def read_pcm(path):
    proc = subprocess.run([
        'ffmpeg', '-hide_banner', '-nostdin', '-nostats', '-threads', '1', '-i', str(path),
        '-map', '0:a:0', '-af', 'ebur128=peak=true:framelog=verbose',
        '-c:a', 'pcm_f32le', '-f', 'f32le', '-'
    ], stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
    audio = np.frombuffer(proc.stdout, dtype='<f4').reshape(-1, 2)
    log = proc.stderr.decode(errors='replace').rsplit('Summary:', 1)[-1]
    lufs = float(re.search(r'^\s*I:\s*([\d.+-]+) LUFS', log, re.M)[1])
    peak = float(re.search(r'Peak:\s*([\d.+-]+) dBFS', log)[1])
    return audio, lufs, peak


def side_mid_db(audio):
    left, right = audio.T
    mid = (left + right) * .5
    side = (left - right) * .5
    return db(np.linalg.norm(side) / max(np.linalg.norm(mid), 1e-10))


def band_attack_overlap(mid, bpm):
    # 저역과 중역의 강한 어택이 25ms 안에 함께 나타난 비율(양방향 F1)이다.
    # 킥이나 기타 한 악기만 분리한 값은 아니며 쉬는 구간을 맞추려고 점수를 올리지 않는다.
    down = signal.resample_poly(mid, 1, 4)
    sample_rate, hop, size = SAMPLE_RATE / 4, 64, 512
    windows = np.lib.stride_tricks.sliding_window_view(down, size)[::hop]
    tapers = signal.windows.hann(size, sym=False).astype('f')
    power = np.abs(fft.rfft(windows * tapers, axis=1)) / size
    frequency = fft.rfftfreq(size, 1 / sample_rate)
    peaks = []
    for bottom, top in [(40, 160), (250, 2000)]:
        band = np.log1p(100 * power[:, (frequency >= bottom) & (frequency <= top)])
        growth = np.maximum(0, np.diff(band, axis=0)).mean(axis=1)
        strongest = np.quantile(growth, .95)
        if strongest <= 1e-8:
            return None
        indices, _ = signal.find_peaks(growth, height=strongest * .50,
                                      prominence=strongest * .20,
                                      distance=max(1, round((60 / bpm / 8) * sample_rate / hop)))
        peaks.append(indices)
    a, b = peaks
    if not len(a) or not len(b):
        return None
    # 시간순으로 한 쌍씩만 소비해 한 번의 저역 충격이 여러 번의 어택과 겹치지 않게 한다.
    limit = round(.025 * sample_rate / hop)
    i = j = pairs = 0
    while i < len(a) and j < len(b):
        if abs(a[i] - b[j]) <= limit:
            pairs += 1
            i += 1
            j += 1
        elif a[i] < b[j]:
            i += 1
        else:
            j += 1
    return round(200 * pairs / (len(a) + len(b)), 2)


def phrase_reentry_contrast(audio, cue):
    # 8마디 내부 경계 양쪽의 같은 길이 검체를 쓴다. 마지막 파일 루프나 무음 꼬리는 제외한다.
    beat = len(audio) / (4 * cue['bars'])
    contrasts = []
    for bar in range(8, int(np.ceil(cue['bars'])), 8):
        if (bar * 4 + .35) * beat > len(audio):
            continue
        centre = bar * 4
        before = audio[round((centre - .42)*beat):round((centre - .12)*beat)]
        after = audio[round((centre + .05)*beat):round((centre + .35)*beat)]
        before_rms = np.sqrt(np.mean(before ** 2))
        after_rms = np.sqrt(np.mean(after ** 2))
        if min(before_rms, after_rms) > .0001:
            contrasts.append(db(after_rms / before_rms))
    return round(float(np.median(contrasts)), 2) if contrasts else None


def matched_difference(current, former):
    proc = subprocess.run(['ffmpeg', '-nostdin', '-loglevel', 'error', '-i', str(former),
                           '-map', '0:a:0', '-c:a', 'pcm_f32le', '-f', 'f32le', '-'],
                          stdout=subprocess.PIPE, check=True)
    old = np.frombuffer(proc.stdout, dtype='<f4')
    now = current.reshape(-1)
    if len(old) != len(now):
        raise ValueError(f'{former.stem}: previous and current recordings have different timelines')
    # 이전 믹스와 가장 잘 맞는 볼륨 하나를 구한 뒤, 그것으로 설명되지 않는 차이를 계산한다.
    gain = np.dot(now, old) / max(np.dot(old, old), 1e-10)
    error = np.linalg.norm(now - old * gain) / max(np.linalg.norm(now), 1e-10)
    return round(db(error), 2)


def measure(path, cue, former):
    frames = music_stream_info(path)
    audio, loudness, peak = read_pcm(path)
    if frames != cue['frames'] or len(audio) != frames:
        raise ValueError(f'{path.stem}: timeline {cue["frames"]}, container {frames}, decoded {len(audio)}')
    if not np.isfinite(audio).all() or np.abs(audio).max() < .01:
        raise ValueError(f'{path.stem}: empty or nonfinite audio')
    if (np.abs(audio) >= 1).any() or peak >= 0:
        raise ValueError(f'{path.stem}: clipped decoded signal or inter-sample full scale ({peak} dBTP)')
    result = {
        'frames': frames, 'lufs_i': loudness, 'true_peak_dbtp': peak,
        'side_mid_db': round(side_mid_db(audio), 2),
        'eight_bar_reentry_contrast_db': phrase_reentry_contrast(audio, cue),
        'low_body_attack_overlap_pct': band_attack_overlap(audio.mean(axis=1), cue['bpm'])
    }
    if former:
        result['gain_matched_difference_dbr'] = matched_difference(audio, former)
    if cue['loop']:
        boundary = float(np.abs(audio[0] - audio[-1]).max())
        # 루프 양끝의 이음새가 주변 80ms에 원래 있던 샘플 간 변화와 비교해 얼마나 강한지 본다.
        context = np.concatenate((np.diff(audio[:3528], axis=0), np.diff(audio[-3528:], axis=0)))
        local = float(np.quantile(np.abs(context), .995))
        result.update(loop_step_dbfs=round(db(boundary), 2),
                      loop_step_relative_db=round(db(boundary / max(local, 1e-10)), 2))
        if boundary > max(.07, 2.5 * local):
            raise ValueError(f'{path.stem}: loop jump {boundary:.3f}, nearby 99.5% step {local:.3f}')
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--music', type=Path, default=PROJECT / 'Assets/Resources/Music/CityScore')
    parser.add_argument('--catalog', type=Path, help='optional score timing reference JSON')
    parser.add_argument('--before', type=Path, help='directory with the previous OGG masters')
    parser.add_argument('--cue', action='append', help='one catalog ID, option may be repeated')
    parser.add_argument('--json', type=Path, help='write per-cue report')
    args = parser.parse_args()
    if args.catalog:
        catalog = json.loads(args.catalog.read_text(encoding='utf-8'))
    else:
        bundled = Path(__file__).with_name('score_timing.json')
        catalog = json.loads(bundled.read_text(encoding='utf-8'))
        album_catalog = PROJECT.parent / 'Soundtrack/catalog.json'
        if album_catalog.is_file():
            original = json.loads(album_catalog.read_text(encoding='utf-8'))
            keys = ('bpm', 'bars', 'frames', 'loop')
            expected = {name: {k: cue[k] for k in keys} for name, cue in original.items()}
            if expected != catalog:
                parser.error('project timing reference differs from album catalog')
    if not args.cue and {p.stem for p in args.music.glob('*.ogg')} != set(catalog):
        parser.error('OGG inventory differs from catalog')
    report = {}
    for name in args.cue or catalog:
        filename = name + '.ogg'
        report[name] = measure(args.music / filename, catalog[name], args.before / filename if args.before else None)
    if args.json:
        args.json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'{len(report)} decoded stereo Vorbis masters, {sum(catalog[n]["loop"] for n in report)} loop joins; '
          f'highest peak {max(r["true_peak_dbtp"] for r in report.values()):.1f} dBTP')


if __name__ == '__main__':
    main()
