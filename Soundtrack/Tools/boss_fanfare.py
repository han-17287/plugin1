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
        [(0,1,1.8),(3,1,2),(6,3,1.7),(9,2,2.6),(13,-2,2.7)]),
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
        [(2,-2,3.4),(8,0,2.6),(13,2,2.4)],
        [(0,-2,2.5),(4,0,1.3),(7,2,2.4),(11,1,2.2),(15,3,.8)]),
    'clock': (
        [(0,7,2.5),(3,5,2.4),(6,3,1.5),(8,5,2.7),(11,4,2.6),(14,2,1.6)],
        [(0,3,2.8),(3,1,2.4),(6,0,1.5),(9,3,3.7),(14,5,1.6)],
        [(0,2,2.4),(3,4,2.5),(6,6,1.45),(8,7,1.5),(10,5,1.5),(12,4,1.5),(14,6,1.5)]),
}

# 마지막 네 음은 첫 마디의 음역과 리듬으로 되돌아가는 각 보스의 여린박이다.
TURN = {'captain': (2,3,1,1), 'iron': (-1,1,-1,-2), 'enforcer': (2,3,5,3),
        'warden': (5,3,2,1), 'joker': (-2,0,2,3), 'clock': (2,4,5,6)}

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
        end = round((bar*4 + (step+length)/4) * ppq)
        if end > start:
            parts[seat].notes.append(Note(start, end, pitch, velocity))

    # 성부를 렌더링하기 전에 다음 마디 첫 음까지 포함한 프레이즈를 만든다.
    chart = []
    seats = []
    for bar, section in enumerate(form):
        phrase = PHRASES[group][2 if bar%4==3 else bar%2]
        if section == 'break':
            phrase = [(0, phrase[0][1], 9.5), (12, phrase[-1][1], 2.5)]
        elif bar == bars-1:
            phrase = [(step, degree, gate) for step,degree,gate in
                      zip((0,7,10,14), TURN[group], (5.2,2,2.9,1.8))]
        chart.append(phrase)
        seats.append(int((section == 'answer' and bar%2) or section == 'break'))

    fast = cue.endswith(('-2','-3','double','turrets','beam')) or group == 'clock'
    for bar,section in enumerate(form):
        root = roots[bar]
        phrase = chart[bar]
        call = seats[bar]
        next_bar = (bar + 1) % bars
        for i,(step,degree,length) in enumerate(phrase):
            pitch=scale_pitch(tonic,degree)
            final = i == len(phrase)-1
            if i == 0 or length >= 4:
                pitch=closest_chord_tone(pitch,root,tonic)
            velocity = (109 if i==0 else 100 if step%4==0 else 93) - (12 if section=='break' else 0)

            # 눈금과 킥은 그대로 두고, 프레이즈가 계속되는 음만 손가락/활을 늦게 뗀다.
            # 조커의 첫 다섯 저격과 다음 마디 첫 쉼, 슈트의 반복 피킹은 의도한 스타카토다.
            next_step = 16 if final else phrase[i+1][0]
            space = next_step-step
            pulse = (group=='joker' and bar%2==0 and section!='break') or (
                group=='iron' and length<=2.0 and section!='break')
            if pulse:
                gate = length
            else:
                # 같은 간격이라도 활과 금관은 더 잇고, 시계의 타건과 쉬어 가는 마디는 숨을 둔다.
                join = .82 if parts[call].program in (48,49,50,60,61) else .66
                if section=='break' or (group=='clock' and step in (0,8)):
                    join = .48
                gate = length + join * (space-length)
                if final and seats[next_bar] != call and chart[next_bar][0][0] == 0:
                    # 악기가 교대하면 잠깐 앞 음을 겹쳐 두 성부가 하나의 선율로 들리게 한다.
                    gate = space + .16
                if bar%4 == 3 and final and section in ('head','climax'):
                    gate = min(gate, space-.50)
            note(call,step,pitch,gate,velocity)
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
    # 답하는 악기가 네 마디 중 일부에서 주성부를 넘겨받을 때 음량과 위치도 중앙으로 잇는다.
    is_reply = np.array([s=='break' or (s=='answer' and b%2==1) for b,s in enumerate(form)], dtype='f')
    samples_per_bar = total / len(form)
    # 마디 전후 1/8박씩 교차한다. 앰프 기타의 페이드로 연주 타이밍을 미루지 않는다.
    bar_time = np.arange(total, dtype='f') / samples_per_bar
    current = np.floor(bar_time + .5).astype(int)
    cross = np.clip((bar_time-current)*16 + .5, 0, 1)
    cross *= cross*(3-2*cross)
    handoff = is_reply[(current-1)%len(form)]*(1-cross) + is_reply[current%len(form)]*cross
    sampler = DrySampler(os.environ.get('BOSS_SF2','/usr/share/sounds/sf2/TimGM6mb.sf2'))
    try:
        for seat,track in enumerate(tracks):
            # 위상이 루프 경계를 지나도 음의 잔향을 자르지 않는다.
            take = sampler.take(track,total + round(.45*RATE),to_frame)
            tail = len(take)-total
            take[:tail] += take[total:]
            take = take[:total]
            cutoff = 5400 if track.program in (27,28,29,30) else 8300
            pad = RATE//10
            take = filter_audio(np.concatenate((take[-pad:],take)), (115 if seat==0 else 145,cutoff),'bandpass')[pad:]
            level = active_level(take)
            if level<=0:
                raise ValueError(f'{cue}: empty {track.name}')
            targets = (.095, .063, .054)
            gain = targets[seat]/level
            if seat==1:
                gain = gain * (1 + .35*handoff)
            mono = take.mean(axis=1)*gain
            side = (take[:,0]-take[:,1])*(gain*.20)
            if track.program in (29,30):
                # 작은 스피커에서도 기타 피킹의 중역이 남도록 앰프의 약한 포화와 캐비닛을 거친다.
                mono = filter_audio(.37*np.tanh(mono/.37), 4700, 'lowpass')
            pan = .19*(1-handoff) if seat==1 else (0 if seat==0 else -.18)
            mix[:,0] += mono*(1-pan)+side
            mix[:,1] += mono*(1+pan)-side
    finally:
        sampler.close()
    return form
