"""보스별 공통 악센트에서 킥·베이스·앰프 기타를 함께 연주한다."""

from functools import lru_cache

import numpy as np
from scipy import signal

RATE = 44100
# 리드의 한 마디를 16분음표로 센 공격 위치. 보스마다 다른 장단을 사용한다.
ACCENTS = {
    'roof': (0, 3, 6, 10, 13), 'iron': (0, 3, 6, 9, 14),
    'enforcer': (0, 2, 5, 8, 12), 'residence': (0, 4, 7, 10, 14),
    'joker': (0, 3, 7, 10, 14), 'clock': (0, 3, 6, 8, 11, 14),
}
# 킥 몸통 주파수, 스네어 통 주파수, 기타 앰프 드라이브, 기타 스피커 컷오프.
RIG = {
    'roof': (60, 194, 2.5, 4250), 'iron': (53, 173, 3.1, 3300),
    'enforcer': (62, 201, 2.25, 3650), 'residence': (57, 194, 2.15, 3900),
    'joker': (65, 216, 2.6, 4150), 'clock': (59, 222, 2.20, 3800),
}


def band(x, limits):
    sos = signal.butter(2, limits, fs=RATE, btype='bandpass', output='sos')
    return signal.sosfilt(sos, x).astype('f')


def timebase(duration):
    return np.arange(round(duration * RATE)) / RATE


def finish(x):
    # 같은 악기라도 똑같은 흰 잡음이 반복되지 않도록 만든 테이크의 양 끝을 정리한다.
    x = x.astype('f')
    x[:24] *= np.linspace(0, 1, 24)
    x[-150:] *= np.linspace(1, 0, 150)
    return x / max(1e-5, np.max(abs(x)))


@lru_cache(maxsize=128)
def drum(kind, rig, variant):
    low, shell, _, _ = RIG[rig]
    rng = np.random.default_rng(913 + variant * 479 + low)
    if kind == 'kick':
        t = timebase(.24)
        phase = 2*np.pi*(low*t + 103*.021*(1-np.exp(-t/.021)))
        body = np.sin(phase) * np.exp(-t/.062)
        beater = band(rng.standard_normal(len(t)), (1100, 3800)) * np.exp(-t/.005)
        return finish(body + .30*beater)
    if kind == 'snare':
        t = timebase(.27)
        noise = rng.standard_normal(len(t))
        attack = band(noise, (640, 5700)) * np.exp(-t/.039)
        wire = band(noise, (880, 3750)) * np.exp(-t/.071)
        head = np.sin(2*np.pi*(shell*t + 1.1*(1-np.exp(-t/.016)))) * np.exp(-t/.056)
        ring = np.sin(2*np.pi*shell*1.68*t) * np.exp(-t/.034)
        return finish(.56*head + .30*ring + 1.55*wire + 1.21*attack)
    t = timebase(.21)
    pitch = (98, 132, 168)[variant % 3]
    phase = 2*np.pi*(pitch*t + 1.1*(1-np.exp(-t/.026)))
    return finish((np.sin(phase)+.21*np.sin(phase*1.53))*np.exp(-t/.067))


def string(t, hz, pick, damping):
    # 탄현 위치에 생기는 배음의 마디와 브리지에서 감쇠하는 고역을 함께 계산한다.
    wave = np.zeros_like(t)
    for harmonic in range(1, min(25, int(8400 / hz)) + 1):
        amplitude = np.sin(np.pi*pick*harmonic) / harmonic**1.35
        partial = hz * harmonic * np.sqrt(1 + .000013*harmonic**2)
        wave += amplitude*np.cos(2*np.pi*partial*t)*np.exp(-t*damping*(1+.30*harmonic))
    return wave


@lru_cache(maxsize=280)
def amp_chord(rig, root, opened, variation):
    _, _, drive, cutoff = RIG[rig]
    t = timebase(.50 if opened else .24)
    damping = 4.4 if opened else 15.0
    takes = []
    for side, cents in enumerate((-.0016, .0013)):
        hz = 440 * 2**((root-69)/12) * (1+cents)
        pick = .18 + .034*side + .006*variation
        fundamental = string(t, hz, pick, damping)
        fifth = string(t, hz*2**(7/12), pick+.04, damping*1.1)
        delay = int(RATE*(.0015+.001*side))
        fifth = np.pad(fifth[:-delay], (delay, 0))
        wave = fundamental*(1-np.exp(-t/.0019)) + .62*fifth*(1-np.exp(-t/.0029))
        # 스피커의 저음 공진을 베이스 위로 두고 브리지 픽업의 중역을 앞으로 보낸다.
        driven = np.tanh(drive*wave)
        cabinet = .60*band(driven, (155, cutoff)) + 2.7*band(driven, (520, cutoff))
        takes.append(cabinet)
    left, right = takes
    # 두 번 연주한 기타를 무대 양옆으로 벌리되 모노에서도 저음의 중심은 유지한다.
    stereo = np.stack((.78*left+.22*right, .22*left+.78*right), axis=1)
    return (stereo / max(.001, np.max(abs(stereo)))).astype('f')


@lru_cache(maxsize=96)
def bass(pitch, variation):
    t = timebase(.48)
    hz = 440 * 2**((pitch-69)/12)
    body = string(t, hz, .28 + variation*.014, 3.3)
    fundamental = np.cos(2*np.pi*hz*t) * np.exp(-t*4.2)
    body = np.tanh(1.2*body + .42*fundamental) * (1-np.exp(-t/.005))
    return finish(band(body, (36, 1650)))


def add_take(track, start, take, volume, gate=None):
    # 마지막 마디 끝의 킥 꼬리도 다음 루프 첫 샘플까지 재생한다.
    if gate is not None and gate < len(take):
        take = take[:gate].copy()
        length = min(gate, round(.015*RATE))
        release = np.linspace(1, 0, length, dtype='f')
        take[-length:] *= release if take.ndim == 1 else release[:, None]
    if take.ndim == 1:
        take = take[:, None]
    start %= len(track)
    body = min(len(take), len(track) - start)
    track[start:start+body] += volume*take[:body]
    if body < len(take):
        track[:len(take)-body] += volume*take[body:]


def add_rhythm(mix, cue, info, notes, form):
    """원래 시료 수·템포·루프 위치를 유지하면서 합주 전용 리듬 섹션을 더한다."""
    rig = cue.split('-')[0]
    bars = info['bars']
    sixteenth = len(mix) / (bars * 16)
    section = np.zeros_like(mix)
    harmony = sorted(notes.get('pickbass') or notes['bass'])
    harmony_ticks = np.array([note[0] for note in harmony])
    accents = ACCENTS[rig]
    for bar, part in enumerate(form):
        quiet = part == 'break'
        big = part in ('rush', 'climax', 'return')
        pattern = list(accents)
        # 고요한 부분은 킥 두 번에 공간을 남기고 절정에서만 연속 발구르기를 연주한다.
        if quiet:
            pattern = [0, accents[-2]] if bar % 2 == 0 else [accents[1]]
        elif part == 'answer' and bar % 2:
            pattern = [accents[0], accents[2], accents[-1]]
        elif big and bar % 2 and rig != 'joker':
            pattern = sorted(set(pattern + [12, 14, 15]))
        if bar % 4 == 3 and not big:
            pattern = [step for step in pattern if step < 14]
        for i, step in enumerate(pattern):
            frame = round((bar*16+step)*sixteenth)
            accent = 1. if step == 0 else .88 if step in accents[2::2] else .72
            stop = pattern[i+1] if i+1 < len(pattern) else 16
            space = (stop-step)*sixteenth
            index = max(0, harmony_ticks.searchsorted(bar*4+step/4+.06, side='right')-1)
            pitch = harmony[index][2]
            if pitch > 46:
                pitch -= 12
            if pitch < 30:
                pitch += 12
            root = pitch if pitch >= 40 else pitch+12
            opened = (space > 2.5*sixteenth and (big or quiet or step == 0))
            gate = max(220, round(space * (.91 if opened else .70)))
            variation = (bar+i)%3
            add_take(section, frame, drum('kick', rig, variation), (.52 if quiet else .74)*accent)
            add_take(section, frame, bass(pitch, variation), (.31 if quiet else .45)*accent, gate)
            if not quiet or i == 0:
                chord = amp_chord(rig, root, opened, variation)
                add_take(section, frame+38, chord, (.52 if quiet else .75)*accent, gate)
        backbeats = [8] if quiet or (rig == 'iron' and part == 'head') else [4, 12]
        for step in backbeats:
            frame = round((bar*16+step)*sixteenth)
            add_take(section, frame, drum('snare', rig, bar%3), .95 if big else .56 if quiet else .81)
        if big and bar % 4 == 3:
            for j, step in enumerate((11, 13, 15)):
                add_take(section, round((bar*16+step)*sixteenth), drum('tom', rig, 2-j), .35+.06*j)
    # 같은 버스에서 기존 스템과 신곡의 어택이 만난다. 원곡마다 다른 게인을 따르되 과도한 증폭은 막는다.
    # 베이스와 킥의 저역이 합쳐질 때 기타 피킹과 스네어의 중역이 묻히지 않도록 기울기를 준다.
    # 반주 스템의 저음은 그대로 두고, 새 리듬 버스만 대역 분리해서 섞는다.
    sos = signal.butter(2, 275, fs=RATE, btype='lowpass', output='sos').astype('f')
    guard = 2000
    padded = np.concatenate((section[-guard:], section, section[:guard]))
    low = signal.sosfiltfilt(sos, padded, axis=0)[guard:-guard]
    section = 1.17*section - .46*low
    mix_rms = np.sqrt(np.mean(mix**2, dtype=np.float64))
    rhythm_rms = np.sqrt(np.mean(section**2, dtype=np.float64))
    gain = .80 * max(.05, mix_rms) / max(1e-5, rhythm_rms)
    mix += section*gain
    return float(gain)
