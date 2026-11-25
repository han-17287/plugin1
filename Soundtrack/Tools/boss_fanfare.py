"""보스별 주제 선율과 응답. 16칸은 한 마디, 0도는 해당 곡의 단조 으뜸음이다."""
from pathlib import Path
import os

import numpy as np

from ensemble_score import Note, Track
from render_field import DrySampler, active_level, filter_audio

RATE = 44100
SCALE = (0, 2, 3, 5, 7, 8, 10)

# 각 구절은 (진입 위치, 음계 도수, 길이). 쉼표 역시 패턴의 일부다.
# 각 보스의 A/B 두 마디와 종지 C는 모든 페이즈가 공유한다.
PHRASES = {
    'captain': (
        [(0,0,2.6),(3,4,2.4),(6,5,3.6),(10,4,2.5),(13,2,2.5)],
        [(0,1,4.9),(6,-1,1.8),(8,0,4.4),(13,2,1.3),(15,3,.75)],
        [(0,4,5.8),(7,5,1.6),(9,7,3.6),(13,6,1),(14,4,1.8)]),
    'iron': (
        [(0,-3,2),(3,-3,1.4),(6,0,2.6),(9,0,3.6),(14,-1,1.4)],
        [(0,-3,5.5),(7,1,1.5),(9,0,4.5),(15,-4,.75)],
        [(0,4,1.8),(3,4,2),(6,5,1.7),(9,4,2.6),(13,0,2.7)]),
    'enforcer': (
        [(0,4,1.5),(2,5,2.6),(5,4,2.2),(8,1,3.6),(12,2,3.4)],
        [(0,0,5.4),(6,-1,1.8),(8,1,1.6),(10,2,4.3)],
        [(0,4,2.5),(3,6,1.7),(5,7,1.5),(7,8,3.7),(12,7,3.4)]),
    'warden': (
        [(0,0,3.6),(4,2,2.7),(7,4,2.6),(10,7,3.4),(14,6,1.5)],
        [(0,5,5.4),(6,2,1.5),(8,4,4.2),(13,3,2.5)],
        [(0,5,3.6),(4,8,2.7),(7,7,2.7),(10,4,3.2),(14,2,1.6)]),
    'joker': (
        [(0,4,1.35),(3,3,1.35),(7,0,1.5),(10,1,1.45),(14,-3,1.35)],
        [(2,2,3.4),(8,-1,2.1),(13,0,2.3)],
        [(0,4,2.5),(4,6,1.3),(7,5,2.4),(11,1,2.2),(15,0,.8)]),
    'clock': (
        [(0,7,2.5),(3,4,2.4),(6,2,1.5),(8,6,2.7),(11,4,2.6),(14,1,1.6)],
        [(0,5,2.8),(3,2,2.4),(6,0,1.5),(9,4,3.7),(14,-1,1.6)],
        [(0,9,2.4),(3,7,2.5),(6,4,1.45),(8,6,1.5),(10,4,1.5),(12,2,1.5),(14,0,1.5)]),
}

# 주 악기, 응답 악기, 합주 때 주제를 같은 리듬으로 받는 악기. GM 프로그램은 0부터 센다.
VOICES = {
    'roof-captain': (30,18,0), 'iron-suit-1': (29,57,48), 'iron-suit-2': (30,61,48),
    'iron-suit-3': (30,60,48), 'enforcer-1': (60,0,48), 'enforcer-2': (61,29,48),
    'enforcer-3': (61,30,49), 'residence-warden-1': (0,49,11),
    'residence-warden-2': (48,0,4), 'residence-warden-3': (48,30,0),
    'joker-sightline': (27,4,50), 'joker-double': (27,28,4),
    'joker-turrets': (30,28,62), 'joker-beam': (30,81,48), 'clock-secretary': (0,11,48),
}


def family(cue):
    for prefix, group in (('roof','captain'), ('iron','iron'), ('enforcer','enforcer'),
                          ('residence','warden'), ('joker','joker'), ('clock','clock')):
        if cue.startswith(prefix):
            return group
    raise ValueError(cue)


def sections(cue, bars):
    forms = {
        24: ('head','answer','break','rush','climax','return'),
        28: ('head','answer','rush','break','lift','climax','return'),
        32: ('head','answer','rush','lift','break','lift','climax','return'),
    }
    shape = list(forms[bars])
    if cue == 'iron-suit-3':
        shape[2], shape[4] = 'break', 'rush'
    if cue == 'clock-secretary':
        shape[5] = 'rush'
    return [name for name in shape for _ in range(4)]


def scale_pitch(tonic, degree):
    octaves, index = divmod(degree, 7)
    return tonic + 12*octaves + SCALE[index]


def closest_chord_tone(pitch, root, tonic):
    # 각 마디의 길게 받치는 음은 이전 베이스 성부의 화성과 함께 해결한다.
    minor = (root-tonic) % 12 in (0, 2, 5, 7)
    intervals = (0, 3 if minor else 4, 7)
    return min((pitch + step for step in range(-6,7)
                if (pitch + step - root) % 12 in intervals), key=lambda n: (abs(n-pitch), n))


def write_parts(cue, score, info):
    ppq, bars = score.ppq, info['bars']
    rhythm = next(t for t in score.tracks if t.name in ('bass', 'pickbass'))
    roots = [next(n.pitch for n in rhythm.notes if bar*4-.04 <= n.start/ppq < (bar+1)*4-.04)
             for bar in range(bars)]
    group = family(cue)
    tonic = 60 + roots[0] % 12
    if group == 'iron':
        tonic -= 0 if roots[0] % 12 < 7 else 12
    if group == 'warden' and tonic > 67:
        tonic -= 12
    parts = [Track(role, i, 0, program, [], [], bars*4*ppq)
             for i, (role, program) in enumerate(zip(('signature','reply','tutti'),VOICES[cue]))]
    form = sections(cue,bars)

    def note(seat, step, pitch, length, velocity):
        start = round((bar*4 + step/4) * ppq)
        end = min(round((bar*4 + (step+length)/4) * ppq), (bar+1)*4*ppq)
        if end > start:
            parts[seat].notes.append(Note(start, end, pitch, velocity))

    fast = cue.endswith(('-2','-3','double','turrets','beam')) or group == 'clock'
    for bar,section in enumerate(form):
        root = roots[bar]
        phrase = PHRASES[group][2 if bar%4==3 else bar%2]
        if section == 'break':
            # 한 손의 긴 음. 마지막 마디의 응답으로 다음 전합주의 박을 예고한다.
            phrase = [(0, phrase[0][1], 9.5), (12, phrase[-1][1], 2.5)]
        elif section == 'return' and bar%4 == 3:
            phrase = [(0,4,5.2), (7,3,2), (10,1,2.9), (14,-1,1.4)]
        call = 1 if (section == 'answer' and bar%2) or section == 'break' else 0
        for i,(step,degree,length) in enumerate(phrase):
            pitch=scale_pitch(tonic,degree)
            if i == 0 or length >= 4:
                pitch=closest_chord_tone(pitch,root,tonic)
            velocity = (109 if i==0 else 100 if step%4==0 else 93) - (12 if section=='break' else 0)
            note(call,step,pitch,length,velocity)
            if section == 'climax' or (section=='return' and bar%2==0):
                if i in (0,len(phrase)-1) or length>=3.4:
                    # 현/금관은 주제의 강박만 길게 받는다.
                    note(2,step,pitch - (12 if pitch>=65 else 0),length+.25,velocity-9)
            if section in ('rush','lift','climax') and length>=2.4 and (i+bar)%2==0:
                # 주제 사이 빈 자리에만 짧은 16분음표 대답을 넣는다.
                next_step = phrase[i+1][0] if i+1<len(phrase) else 16
                if step+2<=next_step-1:
                    note(1,step+1,pitch-12,.82,83)
                    if fast and step+3 < next_step:
                        note(1,step+2,scale_pitch(tonic,degree-1)-12,.78,76)
        if group == 'joker' and cue != 'joker-sightline' and section in ('answer','rush','climax'):
            # 분신은 소총의 다섯 어택을 서로 다른 높이에서 짧게 흉내 낸다.
            for step,degree,_ in PHRASES[group][0][::2]:
                note(2,step+1,scale_pitch(tonic,degree)-12,.72,76)
        if group == 'clock' and section in ('rush','climax') and bar%2:
            for step,degree in zip((1,4,7,10,13,15),(9,7,5,4,2,0)):
                note(1,step,scale_pitch(tonic,degree),.8,85)
    return parts, form


def mix_hooks(mix, cue, score, info):
    tracks, form = write_parts(cue,score,info)
    total, ticks = info['frames'], info['bars']*4*score.ppq
    to_frame = lambda tick: round(tick * total/ticks)
    sampler = DrySampler(os.environ.get('BOSS_SF2','/usr/share/sounds/sf2/TimGM6mb.sf2'))
    try:
        for seat,track in enumerate(tracks):
            # 위상이 루프 경계를 지나도 음의 잔향을 자르지 않는다.
            take = sampler.take(track,total + round(.45*RATE),to_frame)
            tail = len(take)-total
            take[:tail] += take[total:]
            take = take[:total]
            cutoff = 5400 if track.program in (27,28,29,30) else 8300
            take = filter_audio(take, (115 if seat==0 else 145,cutoff),'bandpass')
            level = active_level(take)
            if level<=0:
                raise ValueError(f'{cue}: empty {track.name}')
            targets = (.095, .063, .054)
            gain = targets[seat]/level
            mono = take.mean(axis=1)*gain
            side = (take[:,0]-take[:,1])*(gain*.20)
            if track.program in (29,30):
                # 작은 스피커에서도 기타 피킹의 중역이 남도록 앰프의 약한 포화와 캐비닛을 거친다.
                mono = filter_audio(.37*np.tanh(mono/.37), 4700, 'lowpass')
            pan = (0,.19,-.18)[seat]
            mix[:,0] += mono*(1-pan)+side
            mix[:,1] += mono*(1+pan)-side
    finally:
        sampler.close()
    return form
