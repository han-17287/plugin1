"""기존 음표의 실제 진입 위치에서 선율을 넘기고, 한 호흡 안의 아티큘레이션을 다듬는다."""
from bisect import bisect_left
from dataclasses import replace
from statistics import median


ANSWERS = ('piano', 'leadguitar', 'daegeum', 'cleanguitar', 'haegeum', 'piano2',
           'cello', 'organ', 'synth', 'gayageum', 'harp', 'piri')
BED = {'strings', 'pad', 'cello', 'ajaeng'}
SHORT = {'spiccato', 'glass', 'coil', 'gayageum', 'harp'}


def upper_notes(notes, tolerance):
    # 코드의 여러 성부를 같은 선율의 큰 도약으로 잘못 읽지 않는다.
    groups = []
    for note in notes:
        if not groups or note.start - groups[-1][0].start > tolerance:
            groups.append([note])
        else:
            groups[-1].append(note)
    return [max(group, key=lambda n: n.pitch) for group in groups]


def assign_responses(parts, chart, ppq, style):
    resolved = list(chart)
    if style == 'stinger':
        return resolved
    for bar, assigned in enumerate(chart):
        start, end = bar*4*ppq, (bar+1)*4*ppq
        if any(n.start < end and n.end > start for n in parts[assigned]):
            continue
        # 편성표의 독주가 쉬는 마디에서 이미 연주되는 응답만 앞에 둔다.
        choices = []
        for rank, name in enumerate(ANSWERS):
            notes = [n for n in parts.get(name, ()) if start <= n.start < end and 55 <= n.pitch <= 88]
            line = upper_notes(notes, .035*ppq)
            if len({n.pitch for n in line}) < 2 or len(line) > 12:
                continue
            motion = [abs(b.pitch-a.pitch) for a,b in zip(line,line[1:])]
            if median(motion) > 7:
                continue
            weight = (3 * (name in chart) + 1.8 * (bar > 0 and name == resolved[bar-1])
                      + min(3.3,len(line)*.72) + median(n.velocity for n in line)/42 - rank*.075)
            choices.append((weight,name))
        if choices:
            resolved[bar] = max(choices)[1]
    return resolved


def tie_harmony(notes, name, leads, ppq):
    if name not in BED:
        return notes
    tied = []
    last = {}
    for note in notes:
        idx = last.get(note.pitch)
        prior = tied[idx] if idx is not None else None
        bar = note.start // (4*ppq)
        if (prior and bar < len(leads) and leads[bar] != name and leads[prior.start//(4*ppq)] != name
                and 0 <= note.start-prior.end <= .36*ppq and .75*ppq <= note.end-note.start
                and .75*ppq <= prior.end-prior.start < 4.8*ppq
                and (bar%4 != 0 or note.start%(4*ppq) > .22*ppq)):
            tied[idx] = replace(prior, end=note.end)
            continue
        last[note.pitch] = len(tied)
        tied.append(note)
    return tied


def phrase(score, chart, info, style):
    ppq = score.ppq
    parts = {track.name:track.notes for track in score.tracks}
    leads = assign_responses(parts, chart, ppq, style)
    line = []
    for bar, name in enumerate(leads):
        lo = 4*ppq*bar
        local = [n for n in parts[name] if lo <= n.start < lo+4*ppq]
        line.extend((n, name) for n in upper_notes(local,.035*ppq))
    updates = {name:{} for name in parts}
    # 응답으로 끌어온 반주 선율이 본래 한 옥타브 낮은 자리라면, 앞뒤 선율 사이에서
    # 매끄러운 음역으로 구절 전체를 옮긴다. 원래의 음정 진행과 저음 화성은 유지한다.
    cursor = 0
    while cursor < len(line):
        note, name = line[cursor]
        bar = note.start // (4*ppq)
        if chart[bar] == name:
            cursor += 1
            continue
        stop = cursor + 1
        while stop < len(line):
            following, part = line[stop]
            if part != name or chart[following.start//(4*ppq)] == name:
                break
            stop += 1
        pitches = [n.pitch for n, _ in line[cursor:stop]]
        before = line[cursor-1][0].pitch if cursor else None
        after = line[stop][0].pitch if stop < len(line) else None
        def distance(shift):
            return ((abs(pitches[0]+shift-before) if before is not None else 0)
                    + (abs(pitches[-1]+shift-after) if after is not None else 0))
        if before is not None or after is not None:
            offsets = [s for s in (-12,12) if min(pitches)+s >= 50 and max(pitches)+s <= 86]
            offset = min([0]+offsets, key=distance)
            if offset and distance(0)-distance(offset) >= 6:
                for j in range(cursor,stop):
                    old, _ = line[j]
                    updates[name][id(old)] = replace(old,pitch=old.pitch+offset)
        cursor = stop

    loop_ticks = round(info['bars']*4*ppq)
    for (original,voice),(next_note,reply) in zip(line,line[1:]):
        a = updates[voice].get(id(original),original)
        b = updates[reply].get(id(next_note),next_note)
        step = b.start-a.start
        space = b.start-a.end
        written = a.end-a.start
        if step <= 0:
            continue
        span = abs(b.pitch-a.pitch)
        handoff = voice != reply
        punctuated = (voice in SHORT or style == 'precision') and not handoff
        if (span > 10 or space < .04*ppq or space > (.85 if handoff else .61)*ppq
                or step > (2.05 if handoff else 1.62)*ppq or written < .23*ppq
                or written/step < (.46 if punctuated else .34)):
            continue
        if punctuated:
            gap = max(.13*ppq, .24*step)
        elif b.pitch == a.pitch:
            gap = min(.12*ppq, .13*step)
        else:
            gap = min(.075*ppq,.065*step)
        # 절의 마지막 숨과 슬러 안의 작은 음 사이를 다르게 처리한다.
        if b.start//(16*ppq) != a.start//(16*ppq) and not handoff:
            gap = max(gap, .18*ppq)
        end = min(loop_ticks,max(a.end,round(b.start-gap)))
        # 같은 음높이의 내부 성부가 먼저 재발음하는 위치를 넘지 않는다.
        end = min(end,next((n.start for n in parts[voice]
                            if a.start < n.start < end and n.pitch == a.pitch),end))
        if end > a.end:
            updates[voice][id(original)] = replace(a,end=end)
    # 짧은 신호는 길이가 고정된 여운 구간 안에서 마지막 화음을 조금 더 울린다.
    if not info['loop'] and line:
        original,name = line[-1]
        last = updates[name].get(id(original),original)
        if last.end-last.start >= .25*ppq:
            tail = min(round(.42*ppq),max(0,(loop_ticks-last.end)//3))
            updates[name][id(original)] = replace(last,end=last.end+tail)
    tracks = []
    for track in score.tracks:
        edits = updates[track.name]
        notes = [edits.get(id(n),n) for n in track.notes]
        notes = tie_harmony(notes,track.name,leads,ppq)
        tracks.append(replace(track,notes=notes))
    return replace(score,tracks=tracks),leads


def entrances(score, leads):
    # 소리가 나기 전에 채널만 바뀌는 일을 피하려고 실제 첫 어택에서 교대한다.
    parts = {track.name:track.notes for track in score.tracks}
    positions = {name:[n.start for n in notes] for name,notes in parts.items()}
    result = [(0,leads[0])]
    for bar,name in enumerate(leads[1:],1):
        if name == result[-1][1]:
            continue
        tick = bar*4*score.ppq
        starts = positions[name]
        index = bisect_left(starts,tick)
        if any(n.start < tick < n.end for n in parts[name]):
            result.append((tick,name))
        elif index < len(starts) and starts[index] < tick + 4*score.ppq:
            result.append((starts[index],name))
    return result
