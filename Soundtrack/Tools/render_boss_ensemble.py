"""오리지널 스템 연주에서 보스별 합주와 반주 응답을 편집한다.

python render_boss_ensemble.py --output /tmp/boss-masters [enforcer-2 ...]
Requires ffmpeg, numpy, scipy, libfluidsynth and TimGM6mb (BOSS_SF2 can override the path).
Originals live in Soundtrack/Stems; output is never written over those takes.
"""
import argparse
import json
import subprocess
from pathlib import Path

import numpy as np
from scipy import ndimage, signal
from ensemble_score import read_score
from boss_fanfare import mix_hooks, sections
from boss_backline import add_rhythm


ROOT = Path(__file__).resolve().parents[1]
RATE = 44100
CONTROL_RATE = 200

# 4마디마다 앞에 서는 실제 연주자. 중간 악장의 독주와 마지막 합주를 보존한다.
SOLOS = {
    'roof-captain': 'leadguitar leadguitar piano haegeum leadguitar leadguitar',
    'iron-suit-1': 'leadguitar leadguitar piri piri piano piano piano piano',
    'iron-suit-2': 'piano piano piano piano piano2 piano leadguitar leadguitar',
    'iron-suit-3': 'leadguitar leadguitar leadguitar leadguitar piano leadguitar taepyeongso leadguitar',
    'enforcer-1': 'piri piri piano piano piano piri piri piri',
    'enforcer-2': 'leadguitar leadguitar leadguitar piano haegeum piano leadguitar leadguitar',
    'enforcer-3': 'taepyeongso leadguitar piano leadguitar piano piano taepyeongso taepyeongso',
    'residence-warden-1': 'piano piano haegeum haegeum piano piano',
    'residence-warden-2': 'haegeum haegeum cleanguitar cleanguitar piano piano haegeum haegeum',
    'residence-warden-3': 'leadguitar leadguitar haegeum haegeum piano leadguitar leadguitar leadguitar',
    'joker-sightline': 'cleanguitar cleanguitar piano piano piano2 cleanguitar cleanguitar',
    'joker-double': 'cleanguitar cleanguitar piano piano haegeum piano cleanguitar cleanguitar',
    'joker-turrets': 'leadguitar leadguitar piano piano synth piano leadguitar leadguitar',
    'joker-beam': 'piano synth synth piano leadguitar leadguitar',
    'clock-secretary': 'piano piano cleanguitar cleanguitar synth piano piano piano'
}
GROUP = {
    'leadguitar':'guitars', 'cleanguitar':'guitars', 'rhythmguitar':'guitars',
    'piano':'keys', 'piano2':'keys', 'organ':'keys',
    'piri':'gugak-winds', 'taepyeongso':'gugak-winds', 'haegeum':'gugak-bow',
    'ajaeng':'gugak-bow', 'synth':'color', 'spiccato':'strings', 'strings':'strings'
}
# 스템 원래 게인은 독주기의 작은 소리까지 살린 값. 비율만 조정한다.
GAINS = {'bass':1.28, 'drums':1.07, 'guitars':1.10, 'keys':1.07, 'strings':1.04,
         'gugak-drums':1.00, 'gugak-bow':1.12, 'gugak-winds':1.13,
         'gugak-pluck':1.04, 'color':1.03, 'filament':.99}
WIDTH = {'bass':0., 'drums':.87, 'keys':.64, 'guitars':.84, 'strings':.72}


def decode(path, frames):
    pcm = subprocess.check_output(['ffmpeg','-nostdin','-v','error','-i',str(path),'-f','f32le',
                                  '-c:a','pcm_f32le','-ac','2','-ar',str(RATE),'-'])
    audio = np.frombuffer(pcm, '<f4').reshape(-1,2)
    if len(audio) != frames:
        raise ValueError(f'{path}: expected {frames} samples, found {len(audio)}')
    return audio


def periodic_filter(x, cutoffs, kind, order=2):
    # 전방향 IIR도 직전 루프의 꼬리를 먼저 통과시킨다.
    sos = signal.butter(order, cutoffs, btype=kind, fs=RATE, output='sos').astype('f')
    pad = min(len(x), RATE // 4)
    return signal.sosfilt(sos, np.concatenate((x[-pad:], x)), axis=0)[pad:]


def interpolate(values, frames):
    t = np.arange(frames, dtype='d') * CONTROL_RATE / RATE
    return np.interp(t, np.arange(len(values)+1), np.r_[values,values[0]]).astype('f')


def mark(lane, points, strength, beat_seconds, tail):
    for beat, value in points:
        offset = round(beat * beat_seconds * CONTROL_RATE)
        length = max(1, round(tail * beat_seconds * CONTROL_RATE))
        if offset < 0 or offset >= len(lane):
            continue
        stop = min(offset+length,len(lane))
        window = np.linspace(1,0,length,endpoint=False,dtype='f')[:stop-offset]**1.45
        lane[offset:stop] = np.maximum(lane[offset:stop], window * value * strength)


def score_lanes(cue, score, info):
    frames, bars = info['frames'], info['bars']
    # 실제 루프 마지막 샘플이 박자표의 끝에 딱 맞도록 분수 마디를 유지한다.
    beat_seconds = frames / RATE / (bars * 4)
    ticks = score.ppq
    notes = {t.name: [(n.start/ticks,n.end/ticks,n.pitch,n.velocity) for n in t.notes]
             for t in score.tracks}
    parts = SOLOS[cue].split()
    if len(parts)*4 != bars:
        raise ValueError(f'{cue}: four-bar lead chart does not cover the loop')
    controls = max(1, int(np.ceil(frames * CONTROL_RATE / RATE)))
    beats = np.arange(controls) / (beat_seconds*CONTROL_RATE)
    leaders = {group:np.zeros(controls,'f') for group in set(GROUP.values())}
    foreground = np.zeros(controls,'f')
    lead_notes = []
    for section, part in enumerate(parts):
        begin, end = section*16, (section+1)*16
        section_notes = [n for n in notes[part] if begin <= n[0] < end]
        lead_notes.extend(section_notes)
        if not section_notes:
            raise ValueError(f'{cue}: no {part} performance in section {section}')
        leaders[GROUP[part]][(beats >= begin) & (beats < end)] = 1
        for a,b,p,v in section_notes:
            start = round(a*beat_seconds*CONTROL_RATE)
            stop = round(min(b+.08, end)*beat_seconds*CONTROL_RATE)
            foreground[start:stop] = 1
    leaders = {g:ndimage.gaussian_filter1d(x,6,mode='wrap') for g,x in leaders.items()}
    foreground = ndimage.gaussian_filter1d(foreground,2.1,mode='wrap')
    kicks = [(a,v/115) for a,b,p,v in notes['drums'] if p in (35,36)]
    snares = [(a,v/128) for a,b,p,v in notes['drums'] if p in (37,38,39,40)]
    anchors = np.zeros(controls,'f')
    mark(anchors,kicks,1,beat_seconds,.61)
    mark(anchors,snares,.83,beat_seconds,.42)
    mark(anchors,[(a,v/117) for a,b,p,v in lead_notes],.69,beat_seconds,.28)
    anchors = ndimage.gaussian_filter1d(anchors,1.35,mode='wrap')
    # 8마디 맺음 직전에는 무대 전체가 숨을 들이마신다. 다음 마디 첫 어택은 보존한다.
    breath = np.zeros(controls,'f')
    for bar in range(7,bars,8):
        before = beats - (bar*4)
        start = 3.4 if (bar//8)%2==0 else 3.55
        breath[(before >= start) & (before < 3.96)] = 1
    breath = ndimage.gaussian_filter1d(breath,1.45,mode='wrap')
    return notes, parts, beat_seconds, leaders, foreground, anchors, breath, kicks, snares


def choose_responses(cue,bar,kicks,snares,section):
    # 원래 연주하는 킥·백비트 중 어느 악센트에 답할지를 보스별로 고른다.
    pattern = [a for a,v in kicks if bar*4-.025 <= a < (bar+1)*4-.025]
    backbeat = [a for a,v in snares if bar*4-.025 <= a < (bar+1)*4-.025 and v > .55]
    if not pattern:
        return []
    if cue.startswith(('iron-suit','roof')):
        times = pattern[::2] + backbeat[-1:]
    elif cue.startswith('joker'):
        times = pattern[1::2] + backbeat[:1]
    elif cue.startswith('enforcer'):
        times = pattern[:2] + backbeat[-1:] if bar%2==0 else pattern[-2:] + backbeat[:1]
    else:
        times = pattern[::2] + backbeat[:1] if section%2==0 else pattern[1::2] + backbeat[-1:]
    result = []
    for onset in sorted(times):
        if (not result or onset-result[-1]>.24) and (bar%8!=7 or onset < bar*4+3.44):
            result.append(onset)
    return result


def comp_edit(take, group, cue, notes, parts, beat_seconds, kicks, snares):
    """같은 화성의 실제 건반/기타 음을 잘라 킥·스네어에 응답하는 짧은 코드로 편집한다."""
    frames = len(take)
    samples_per_beat = beat_seconds * RATE
    source = notes['rhythmguitar'] if group=='guitars' else notes.get('piano',[]) + notes.get('piano2',[])
    # 한 코드의 최저음을 기준으로 소스를 하나만 고른다.
    source = sorted(source, key=lambda n:(n[0],n[2]))
    usable = []
    for a,b,p,v in source:
        if p > (72 if group=='keys' else 68) or (usable and a-usable[-1][0]<.11):
            continue
        usable.append((a,b,p))
    if not usable:
        return 0,0
    bass = sorted(notes.get('pickbass',notes.get('bass',[])))
    def harmony(at):
        # 옥타브나 5도의 지나가는 베이스 대신 그 반마디의 첫 옥타브를 기준으로 잡는다.
        half = int((at+.03)//2)*2
        bass_half = [n for n in bass if half-.03<=n[0]<half+1.97]
        return min(bass_half,key=lambda n:n[2])[2]%12 if bass_half else None
    # 그 마디의 원래 음을 남겨야 할 때를 위해 원본을 읽기 전용으로 참조한다.
    original = take.copy()
    comp_color = periodic_filter(original, (120,2500) if group=='guitars' else (140,3200), 'bandpass')
    edited = moved = 0
    for bar in range(len(parts)*4):
        active = GROUP[parts[bar//4]]
        if group == active or bar%4 not in (0,1,3):
            continue
        if active == 'guitars' and group!='keys':
            continue
        if active != 'guitars' and group!='guitars':
            continue
        lo,hi=bar*4,(bar+1)*4
        local=[n for n in usable if lo-.026<=n[0]<hi-.12]
        if not local:
            continue
        rendered = np.zeros((round(hi*samples_per_beat) - round(lo*samples_per_beat),2),'f')
        responses = 0
        for at in choose_responses(cue,bar,kicks,snares,bar//4):
            candidates=[(a,b,p) for a,b,p in local if harmony(a)==harmony(at)]
            if not candidates:
                continue
            a,b,p=min(candidates,key=lambda n:abs(n[0]-at))
            src=max(0, round(a*samples_per_beat))
            dest=max(0, round(at*samples_per_beat)-round(lo*samples_per_beat))
            gate = min(.51, max(.28,(b-a)*.95), (hi-at))
            duration = min(round(gate*samples_per_beat),len(rendered)-dest,frames-src)
            if duration < round(.025*RATE):
                continue
            clip = comp_color[src:src+duration]
            if np.max(abs(clip)) < .009:
                continue
            envelope = np.ones(duration,'f')
            attack, release = min(duration//3,180), min(duration//3,700)
            envelope[:attack] = np.linspace(0,1,attack,dtype='f')**1.5
            envelope[-release:] = np.linspace(1,0,release,dtype='f')**1.5
            rendered[dest:dest+duration] += clip * envelope[:,None]
            responses+=1
            moved += all(abs(at-o[0]) > .085 for o in local)
        if responses:
            start,end=round(lo*samples_per_beat),round(hi*samples_per_beat)
            region=take[start:end]
            # 마디 양 끝에서 이미 울던 코드의 릴리스를 남겨 새 보이싱을 부드럽게 넘긴다.
            ramp=np.ones(len(region),'f')
            edge=round(beat_seconds * (.30 if group=='keys' else .20) * RATE)
            rise=.5-.5*np.cos(np.linspace(0,np.pi,edge,dtype='f'))
            ramp[:edge],ramp[-edge:] = rise,rise[::-1]
            # 원본의 방 울림이 남도록 완전 음소거 대신 연주 바닥만 낮춘다.
            take[start:end] = region*(1-.83*ramp[:,None]) + 1.16*rendered*ramp[:,None]
            edited += 1
    return edited,moved


def master_bus(mix, info):
    mid = mix.mean(axis=1)
    room_input = periodic_filter(mid,(210,2900),'bandpass')
    # 모든 악기를 한 곳에 배치하는 짧은 공통 초기 반사음. 루프 꼬리도 이어 준다.
    for c,taps in enumerate((((.0092,.065),(.0216,.033),(.036,-.021)),
                             ((.0127,.061),(.0248,.031),(.038,.019)))):
        for seconds,gain in taps:
            mix[:,c] += np.roll(room_input,round(seconds*RATE))*gain
    # 공통 검출기는 스테레오 양쪽에 같은 게인을 준다. 킥마다 바닥이 꺼지지 않게 1.8dB 이내.
    hop=round(RATE*.005)
    pad=(-len(mix))%hop
    blocks=np.pad(mix,((0,pad),(0,0)),mode='wrap').reshape(-1,hop,2)
    power=np.sqrt(np.mean(blocks**2,axis=(1,2)))
    rms = ndimage.gaussian_filter1d(power,1.9,mode='wrap')
    threshold=np.percentile(rms,68)
    gain_db=np.clip(-.36*np.maximum(20*np.log10((rms+1e-8)/(threshold+1e-8)),0),-1.8,0)
    gain=np.interp(np.arange(len(mix))/hop,np.arange(len(power)+1),np.r_[gain_db,gain_db[0]])
    mix *= np.power(10,gain[:,None]/20).astype('f')
    # 짧은 보스 대사 사이에서도 각 곡의 강약은 유지한다.
    target_db = -18.35 if info['bpm']<207 else -17.55
    mix *= (10**(target_db/20) / max(.001,float(np.sqrt(np.mean(mix**2)))))
    # 부드러운 무릎은 큰 어택만 막는다. 코덱 오버슈트 여유를 둔다.
    absolute = np.abs(mix)
    knee = .66
    return np.sign(mix)*(np.minimum(absolute,knee)+.18*np.tanh(np.maximum(absolute-knee,0)/.18))


def render(cue, out, catalog, quality=5.2):
    info = catalog[cue]
    score = read_score(ROOT/'MIDI'/f'{cue}.mid')
    notes, parts, beat, leaders, front, anchors, breath, kicks, snares = score_lanes(cue, score, info)
    frames = info['frames']
    mix=np.zeros((frames,2),'f')
    form = sections(cue, info['bars'])
    phase = np.arange(len(front)) / (CONTROL_RATE * beat * 4)
    # 전 합주를 동시에 숨 쉬게 한다. 연결부를 25ms 동안 열어 게인 계단을 숨긴다.
    contrast = {'head': .94, 'answer': .84, 'rush': 1.03, 'break': .50,
                'lift': .90, 'climax': 1.08, 'return': .99}
    bar_levels = np.asarray([contrast[f] for f in form], dtype='f')
    section_gain = ndimage.gaussian_filter1d(bar_levels[np.minimum(phase.astype(int), len(form)-1)],
                                              5, mode='wrap')
    bed = {'bass': .78, 'drums': .82, 'guitars': .37, 'keys': .39, 'strings': .66,
           'gugak-winds': .26, 'gugak-bow': .40, 'gugak-pluck': .43,
           'color': .45, 'gugak-drums': .72, 'filament': .85}
    edited=moved=0
    for stem in sorted((ROOT/'Stems'/cue).glob('*.ogg')):
        group=stem.stem
        take=decode(stem,frames).copy()
        if group in ('guitars','keys'):
            bars,onsets=comp_edit(take,group,cue,notes,parts,beat,kicks,snares)
            edited+=bars; moved+=onsets
        mid=take.mean(axis=1)
        width=WIDTH.get(group,.79)
        side=take[:,0]-mid
        side=(side-periodic_filter(side,142,'lowpass'))*width
        if group in ('bass','drums','guitars'):
            edges={'bass':(110,315),'drums':(135,515),'guitars':(200,670)}
            body=periodic_filter(mid,edges[group],'bandpass')
            mid+=body*{'bass':.37,'drums':.18,'guitars':.21}[group]
        if group=='bass':
            automation=(.94+.15*anchors)*(1-.75*breath)
        elif group=='drums':
            automation=(.97+.08*anchors)*(1-.38*breath)
        elif group=='gugak-drums':
            automation=(.7+.30*anchors)*(1-.71*breath)
        else:
            lead=leaders.get(group,0.)
            # 중심 악기가 말할 때 다른 파트는 코드를 짚고, 빈 곳에는 응답을 남긴다.
            if group in ('guitars','keys'):
                backing = .71+.22*anchors+.07*(1-front)
            elif group=='strings':
                backing = .64+.15*anchors+.20*(1-front)
            else:
                backing = .60+.16*anchors+.25*(1-front)
            voice = 1.19 if group=='guitars' else 1.31
            automation=((1-lead)*backing + lead*voice)*(1-breath*(.75-.4*lead))
        gain=interpolate(automation*section_gain,frames)*GAINS[group]*bed[group]
        mix[:,0]+=(mid+side)*gain
        mix[:,1]+=(mid-side)*gain
    if not edited or not moved:
        raise ValueError(f'{cue}: no newly coordinated ensemble responses')
    mix_hooks(mix,cue,score,info)
    add_rhythm(mix,cue,info,notes,form)
    master=master_bus(mix,info).astype('<f4')
    path=out/(cue+'.ogg')
    def encode():
        subprocess.run(['ffmpeg','-nostdin','-v','error','-y','-f','f32le','-ar',str(RATE),
                        '-ac','2','-i','-','-map_metadata','-1','-c:a','libvorbis','-q:a',str(quality),
                        str(path)],input=master.tobytes(),check=True)
        return decode(path,frames)
    result=encode()
    if np.max(abs(result[0]-result[-1]))>.016:
        # 코덱을 통과한 루프 경계에서만 짧게 DC 높이를 잇고 첫 킥 어택을 보존한다.
        # 이전 Ogg를 재인코딩하지 않고 원본 믹스에서 다시 한 번 내보낸다.
        ramp=.5-.5*np.cos(np.linspace(0,np.pi,round(RATE*.003),dtype='f'))
        master[-len(ramp):]+=ramp[:,None]*(result[0]-result[-1])
        result=encode()
    peak=float(np.max(np.abs(result)))
    seam=float(np.max(np.abs(result[-1]-result[0])))
    if peak>.97 or seam>.022 or not np.isfinite(result).all():
        raise ValueError(f'{cue}: peak {peak:.4f}, seam {seam:.5f}')
    print(f'{cue}: {edited} bars with new comp, {moved} restrikes on new beats; '
          f'{frames} decoded frames, peak {20*np.log10(peak):.1f} dBFS, boundary step {seam:.4f}', flush=True)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('cues',nargs='*')
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--quality',type=float,default=5.2,help='Vorbis encoding quality')
    args=parser.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    catalog=json.loads((ROOT/'catalog.json').read_text())
    for cue in args.cues or sorted(SOLOS):
        render(cue,args.output,catalog,args.quality)


if __name__=='__main__':
    main()
