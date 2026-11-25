"""Arrange the editable City Score MIDI parts for a common rhythm section.

Run from the repository with Python 3.10+:
  python Soundtrack/Tools/ensemble_score.py --output /tmp/ensemble-v2

Original programs, ports, control changes, conductor track, and loop markers are
retained. The generated MIDI is an alternate performance of the source score.
"""
from __future__ import annotations

import argparse
import json
import struct
from collections import Counter, defaultdict, deque
from dataclasses import dataclass, field, replace
from math import ceil
from pathlib import Path
from statistics import median


@dataclass(frozen=True)
class Note:
    start: int
    end: int
    pitch: int
    velocity: int


@dataclass
class Track:
    name: str
    channel: int
    port: int
    program: int
    notes: list[Note]
    controls: list[tuple[int, bytes]]
    end: int


@dataclass
class Score:
    ppq: int
    tracks: list[Track]
    style: str = ""
    lead: list[str] = field(default_factory=list)


def _read_vlq(data, pos):
    value = 0
    for _ in range(4):
        byte = data[pos]
        pos += 1
        value = (value << 7) | (byte & 127)
        if byte < 128:
            return value, pos
    raise ValueError("invalid MIDI variable-length quantity")


def _vlq(value):
    digits = [value & 127]
    value >>= 7
    while value:
        digits.append(128 | (value & 127))
        value >>= 7
    return bytes(reversed(digits))


def _track(data):
    pos = tick = 0
    channel = port = program = 0
    status = None
    name = ""
    notes, controls = [], []
    holding = defaultdict(deque)
    channels = set()
    while pos < len(data):
        delta, pos = _read_vlq(data, pos)
        tick += delta
        if data[pos] & 128:
            status = data[pos]
            pos += 1
        if status is None:
            raise ValueError("missing MIDI status")
        if status == 255 or status in (240, 247):
            start = pos
            kind = data[pos] if status == 255 else None
            if status == 255:
                pos += 1
            size, pos = _read_vlq(data, pos)
            payload = data[pos:pos + size]
            pos += size
            if kind == 47:
                break
            if kind == 3:
                name = payload.decode()
            elif kind == 33:
                port = payload[0]
            controls.append((tick, bytes((status,)) + data[start:pos]))
            status = None
            continue
        kind, channel = divmod(status, 16)
        channels.add(channel)
        size = 1 if kind in (12, 13) else 2
        event = data[pos:pos + size]
        pos += size
        if kind == 9 and event[1]:
            holding[event[0]].append((tick, event[1]))
        elif kind in (8, 9):
            if not holding[event[0]]:
                raise ValueError(f"{name}: orphan note off")
            onset, velocity = holding[event[0]].popleft()
            if tick > onset:
                notes.append(Note(onset, tick, event[0], velocity))
        else:
            if kind == 12:
                program = event[0]
            controls.append((tick, bytes((status,)) + event))
    if len(channels) > 1 or any(holding.values()):
        raise ValueError(f"{name}: expected finished single-channel stems")
    return Track(name, channel, port, program, sorted(notes, key=lambda n: (n.start, n.pitch)), controls, tick)


def read_score(path):
    data = Path(path).read_bytes()
    if data[:4] != b"MThd":
        raise ValueError("missing MIDI header")
    size, fmt, count, ppq = struct.unpack_from(">IHHH", data, 4)
    if fmt != 1 or not ppq or ppq >= 32768:
        raise ValueError("expected type-1 MIDI, beat timing")
    pos = 8 + size
    tracks = []
    for _ in range(count):
        if data[pos:pos + 4] != b"MTrk":
            raise ValueError("missing MIDI track")
        length = int.from_bytes(data[pos + 4:pos + 8], "big")
        tracks.append(_track(data[pos + 8:pos + 8 + length]))
        pos += 8 + length
    return Score(ppq, tracks)


def write_score(score, path):
    data = bytearray(struct.pack(">4sIHHH", b"MThd", 6, 1, len(score.tracks), score.ppq))
    for track in score.tracks:
        events = [(t, 0, payload) for t, payload in track.controls]
        for note in track.notes:
            events.extend(((note.start, 2, bytes((0x90 | track.channel, note.pitch, note.velocity))),
                           (note.end, 1, bytes((0x80 | track.channel, note.pitch, 0)))))
        previous = 0
        raw = bytearray()
        for tick, _, payload in sorted(events, key=lambda e: (e[0], e[1])):
            raw.extend(_vlq(tick - previous))
            raw.extend(payload)
            previous = tick
        raw.extend(_vlq(max(0, track.end - previous)) + b"\xff\x2f\x00")
        data.extend(struct.pack(">4sI", b"MTrk", len(raw)))
        data.extend(raw)
    Path(path).write_bytes(data)


# The numbers are bar numbers as printed on a score (starting at one).
# These handoffs follow the actual melodic entrances in the original MIDI.
HANDOFF = {
    "shift-current": "1:piano 13:leadguitar 21:piano 37:leadguitar 45:piano",
    "filament": "1:gayageum 9:piano 17:haegeum 25:gayageum",
    "ignition-title": "1:daegeum 13:piano 17:daegeum 25:piano",
    "sealed-gate": "1:gayageum 5:daegeum 9:gayageum",
    "death": "1:piano",
    "checkpoint": "1:glass",
    "retreat": "1:daegeum",
    "main-theme": "1:piano 5:leadguitar 13:piano 21:daegeum 29:piano 37:leadguitar",
    "lift-between": "1:piano 9:daegeum 17:piano",
    "rank-access": "1:piano",
    "vacant-office": "1:piano 9:haegeum 17:piano",
    "descent-convoy": "1:piano 9:leadguitar 17:piano 25:leadguitar",
    "core-ignition": "1:cleanguitar 5:piano",
    "first-ending": "1:piano 9:cleanguitar 17:haegeum 25:piano",
    "morning-inventory": "1:piano 9:daegeum 17:piano",
    "breach-alarm": "1:coil 5:piri",
    "truck-pursuit": "1:piano 9:leadguitar 25:piano 29:leadguitar",
    "redline-remount": "1:piano 9:leadguitar 17:piano 25:leadguitar",
    "cargo-verdict": "1:piano 5:leadguitar 9:harp 13:piano",
    "alley-trail": "1:cleanguitar 9:daegeum 17:cleanguitar",
    "spotlight-grid": "1:piano 9:haegeum 17:piano",
    "warehouse-crew": "1:leadguitar 9:piri 17:leadguitar",
    "numbered-facades": "1:piano 9:piri 17:cleanguitar 25:piano",
    "shutter-index": "1:piano2 9:spiccato 17:piano2",
    "glass-terrace": "1:daegeum 9:cleanguitar 17:piano 25:daegeum",
    "greenhouse-fault": "1:spiccato 9:daegeum 17:piano",
    "summit-trespass": "1:cleanguitar 5:piano 13:cleanguitar 21:piano",
    "clock-ante": "1:piano 5:daegeum 13:piano 17:daegeum",
    "heat-riser": "1:leadguitar 9:piano 17:leadguitar",
    "salvage-orbit": "1:synth 9:organ 17:synth",
    "service-lantern": "1:cleanguitar 9:daegeum",
    "prism-circuit": "1:piano 9:cleanguitar 17:synth",
    "ledger-imprint": "1:cleanguitar 9:haegeum",
    "witness-crossing": "1:cello 9:daegeum 13:cello 21:daegeum",
    "sixty-to-recovery": "1:coil 5:piano",
}
PRECISION = {"filament", "spotlight-grid", "greenhouse-fault", "shutter-index", "prism-circuit"}
PERCUSSION = {"drums", "janggu", "buk", "timpani", "kkwaenggwari"}
KEYS = {"piano", "piano2", "cleanguitar", "harp", "spiccato", "organ"}
DRONES = {"cello", "strings", "pad", "ajaeng", "brass"}


def _voices(cue, bars):
    current = ""
    schedule = {int(mark) - 1: name for mark, name in (token.split(":") for token in HANDOFF[cue].split())}
    for bar in range(bars):
        current = schedule.get(bar, current)
        yield current


def _style(cue, info):
    if info["bars"] <= 5:
        return "stinger"
    if cue in PRECISION:
        return "precision"
    if info["bpm"] >= 190:
        return "drive"
    if info["bpm"] >= 120:
        return "groove"
    return "atmosphere"


def _velocity(note, factor):
    return replace(note, velocity=max(1, min(122, round(note.velocity * factor))))


def _clusters(notes, tolerance):
    groups = []
    for note in sorted(notes, key=lambda n: n.start):
        if not groups or note.start - groups[-1][0].start > tolerance:
            groups.append([])
        groups[-1].append(note)
    return groups


def _tidy(notes, limit):
    # Make same-key retriggers explicit. Some GM players stop a newly-repeated
    # note when the previous note's release falls after the second attack.
    by_pitch = defaultdict(list)
    for n in sorted(notes, key=lambda n: (n.start, -n.velocity)):
        same = by_pitch[n.pitch]
        if n.start >= limit or (same and same[-1].start == n.start):
            continue
        if same and same[-1].end > n.start:
            same[-1] = replace(same[-1], end=n.start)
        same.append(replace(n, end=min(n.end, limit)))
    return sorted((n for same in by_pitch.values() for n in same if n.end > n.start),
                  key=lambda n: (n.start, n.pitch))


def _accent_grid(ppq, start, end, lead, drums, groups, style):
    """One anchor in each half-bar, plus an actual pickup if the phrase has one."""
    kicks = [n for n in drums if n.pitch in (35, 36)]
    # In an unscored break, don't invent a new accent to fill the silence.
    base = kicks or lead or [chord[0] for chord in groups]
    if not base:
        return []
    strong = []
    for half in (start, start + 2 * ppq):
        pool = [n for n in base if half <= n.start < min(end, half + 2 * ppq)]
        if pool:
            def salience(n):
                shared = any(abs(m.start - n.start) < .11 * ppq for m in lead)
                return n.velocity + 19 * shared + 16 * (abs(n.start - half) < .12 * ppq)
            strong.append(max(pool, key=salience).start)
    if style in ("drive", "groove"):
        slots = [n.start for n in base if (n.start - start) >= 2.5 * ppq and n.start not in strong]
        if slots:
            strong.append(slots[-1])
    return sorted(set(strong))


def _articulate(note, start, duration, velocity, pitch=None):
    return replace(_velocity(note, velocity), start=start, end=start + max(1, round(duration)),
                   pitch=note.pitch if pitch is None else pitch)


def _role_bar(name, notes, lead_name, melody, focus, grid, drums, start, end, ppq, bar, style):
    if not notes or name == lead_name:
        return notes
    drive = style == "drive"
    moving = style in ("drive", "groove", "precision")
    groups = _clusters(notes, ppq * .04)
    foreground = [n for n in melody if n.end > start]
    has_bass = name in ("pickbass", "bass") or (name in ("geomungo", "synth") and median(n.pitch for n in notes) < 46)
    cadence = moving and bar % 8 == 7
    brake = start + int((3.45 if drive else 3.62) * ppq)
    def accent(t):
        return any(abs(mark - t) < .10 * ppq for mark in grid)
    def singer(t):
        return any(n.start - .10 * ppq <= t < n.end + .08 * ppq for n in foreground)

    answer = []
    for index, chord in enumerate(groups):
        note = chord[0]
        t = note.start
        beat = (t - start) / ppq
        last_in_bar = index == len(groups) - 1

        if name == "drums":
            for hit in chord:
                if hit.pitch in (35, 36, 38, 40):
                    scale = 1.04 if (accent(t) or hit.pitch in (38, 40)) else .93
                elif hit.pitch in (42, 44, 46, 51, 53, 59):
                    scale = .94 if abs(beat - round(beat)) < .06 else .77
                    # Leave the band stop to the snare/tom pickup.
                    if cadence and t >= brake and hit.pitch in (42, 44, 46):
                        continue
                else:
                    scale = .94
                answer.append(_velocity(hit, scale))
            continue

        if style == "stinger":
            # Preserve the final cadential chord and the intervals of the signal.
            # Let each harmony part attack with the main punctuation where close.
            nearest = min(foreground, key=lambda n: abs(n.start-t), default=None)
            move = nearest.start-t if nearest and abs(nearest.start-t) < .16*ppq else 0
            answer.extend(_articulate(n, n.start+move, n.end-n.start, .88 if name in DRONES else .94) for n in chord)
            continue

        if has_bass:
            if cadence and t >= brake:
                continue
            next_attack = groups[index+1][0].start if not last_in_bar else end
            for n in chord:
                duration = n.end - n.start
                if moving and accent(t):
                    duration = min(next_attack - t - .04*ppq, max(duration, (.45 if drive else .68)*ppq))
                if cadence:
                    duration = min(duration, brake - t)
                # Keep the kick's fundamental clear. This lifts only very low
                # roots; the written harmony and the bass figure stay intact.
                pitch = n.pitch + 12 if n.pitch < 31 else n.pitch
                answer.append(_articulate(n, t, duration, 1.01 if accent(t) else .88, pitch))
            continue

        if name == "rhythmguitar" or (name in KEYS and moving and len(groups) >= 5):
            # Leave one of each pair of offbeats free for the lead. The accented
            # chord hit and its pickup still share one rhythm with the drummer.
            retained = accent(t) or (not singer(t) and index % 2 == bar % 2) or index == 0
            if name in KEYS:
                if not retained and index % 3 == bar % 3 and (not singer(t) or len(foreground) == 0):
                    retained = True
                if index == 0 and grid and not accent(t) and abs(grid[0]-t) <= .30*ppq:
                    t = grid[0]
            if not retained or (cadence and t >= brake):
                continue
            for n in chord:
                pitch = n.pitch
                if name in KEYS and singer(t) and pitch >= 70:
                    pitch -= 12
                if name == "rhythmguitar" and singer(t) and pitch < 46:
                    pitch += 12
                length = (n.end-n.start) * (1.25 if accent(t) else .85)
                answer.append(_articulate(n, t, min(length, end-t), .94 if accent(t) else .84, pitch))
            continue

        if name in DRONES:
            for n in chord:
                pitch = n.pitch
                if singer(t) and pitch >= 67 and any(abs(n.pitch-m.pitch) <= 5 for m in foreground):
                    pitch -= 12
                length = n.end - n.start
                if cadence:
                    if t >= brake:
                        continue
                    length = min(length, brake-t)
                answer.append(_articulate(n, n.start, length, .79 if singer(t) else .95, pitch))
            continue

        if name in PERCUSSION:
            # Janggu/buk reinforce selected kit accents and answer the kit fills.
            if name != "kkwaenggwari" and drums and not accent(t) and singer(t) and index % 2:
                continue
            answer.extend(_velocity(n, .91 if accent(t) else .76) for n in chord)
            continue

        # Only one countervoice owns the high register while the theme is sung.
        # Keep low harmony notes, and allow high ornaments to answer in its gaps.
        if cadence and t >= brake:
            continue
        for n in chord:
            if singer(t) and n.pitch >= 62 and foreground:
                coincident = any(abs(n.start - m.start) < .045*ppq and n.pitch % 12 == m.pitch % 12 for m in foreground)
                if coincident and name == focus and accent(t):
                    answer.append(_velocity(n, .78))
                    continue
                if name != focus:
                    if n.pitch >= 70 and name not in ("piano", "piano2"):
                        continue
                    if index % 2:
                        continue
                    answer.append(_articulate(n, n.start, n.end-n.start, .70,
                                               n.pitch-12 if n.pitch >= 67 else n.pitch))
                    continue
                # Short answering pickup after the melody releases, confined to
                # the same half-bar so it can't cross into a different chord.
                release = min((m.end for m in foreground if m.start <= t < m.end), default=t)
                delayed = release + int(.035*ppq)
                if (0 < delayed-t <= .46*ppq and (delayed-start)//(2*ppq) == (t-start)//(2*ppq)
                        and not any(m.start <= delayed < m.end for m in foreground)):
                    room = min((m.start for m in foreground if m.start > delayed), default=end) - delayed
                    if room >= .14*ppq:
                        answer.append(_articulate(n, delayed, min(n.end-n.start, room*.88), .96))
                        continue
                answer.append(_articulate(n, n.start, n.end-n.start, .78,
                                           n.pitch-12 if n.pitch >= 70 else n.pitch))
            else:
                answer.append(_velocity(n, .96 if name == focus else .87))
    return answer


def arrange(score, cue_id, catalog_entry):
    """Return a new arranged score; keep the original MIDI as editable source."""
    ppq = score.ppq
    bars = ceil(catalog_entry["bars"])
    end = round(catalog_entry["bars"] * 4 * ppq)
    style = _style(cue_id, catalog_entry)
    leads = list(_voices(cue_id, bars))
    # The extra 'circuit bass' from the last field mix is an exact MIDI doubling
    # of a pre-existing part. Keep its parent, never stack the two attacks.
    parts = [t for t in score.tracks if t.name != "circuit bass"]
    by_name = {t.name: t for t in parts}
    if not set(leads) <= by_name.keys():
        raise ValueError(f"{cue_id}: lead instrument missing")
    parts_by_bar = {t.name: [[] for _ in range(bars)] for t in parts}
    result = {t.name: [] for t in parts}
    for track in parts:
        for note in track.notes:
            bar = note.start // (4*ppq)
            if bar < bars:
                parts_by_bar[track.name][bar].append(note)
    for bar, lead_name in enumerate(leads):
        start, stop = bar * 4 * ppq, min(end, (bar+1) * 4 * ppq)
        phrase = parts_by_bar[lead_name][bar]
        sounding = [n for n in by_name[lead_name].notes if n.start < stop and n.end > start]
        drums = parts_by_bar["drums"][bar] if "drums" in by_name else []
        bass = next((parts_by_bar[r][bar] for r in ("bass", "pickbass", "geomungo")
                     if r in by_name and parts_by_bar[r][bar]), [])
        grid = _accent_grid(ppq,start,stop,phrase,drums,_clusters(bass,ppq*.04),style)
        candidates = {}
        for t in parts:
            if t.name == lead_name or t.name in PERCUSSION | DRONES | {"coil","glass","bass","pickbass","rhythmguitar"}:
                continue
            pair = parts_by_bar[t.name][bar//2*2:min(bars,bar//2*2+2)]
            line = [n for group in pair for n in group if n.pitch>=60]
            if line:
                candidates[t.name] = median(n.velocity for n in line) + min(8,len(line)) - max(0,len(line)-13)
        focus = max(candidates, key=candidates.get, default=None)
        for t in parts:
            result[t.name].extend(_role_bar(t.name, parts_by_bar[t.name][bar], lead_name, sounding,
                                             focus, grid, drums,start,stop,ppq,bar,style))
    return Score(ppq,[replace(t,notes=_tidy(result[t.name],end)) for t in parts],style,leads)


def _foreground_attacks(score, leads):
    attacks = Counter()
    for part in score.tracks:
        for note in part.notes:
            bar = note.start // (4 * score.ppq)
            if bar < len(leads) and leads[bar] == part.name:
                attacks[part.name, note.start, note.pitch, note.velocity] += 1
    return attacks


def verify_arrangement(source, arranged, exported):
    if arranged.ppq != exported.ppq or arranged.tracks != exported.tracks:
        raise ValueError("the edited score did not survive MIDI serialization")
    # If comp notes are moved into a later phrase, they must not accidentally
    # become extra lead notes there. Check the written motif in both directions.
    if _foreground_attacks(source, arranged.lead) != _foreground_attacks(exported, arranged.lead):
        raise ValueError("a foreground attack, pitch, or velocity changed")
    original_parts = {track.name: track for track in source.tracks}
    for track in exported.tracks:
        original = original_parts[track.name]
        if (original.controls, original.end) != (track.controls, track.end):
            raise ValueError(f"{track.name}: conductor or phrase markers changed")


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    root = Path(__file__).resolve().parents[1]
    ap.add_argument("--source", type=Path, default=root/"MIDI")
    ap.add_argument("--catalog", type=Path, default=root/"catalog.json")
    ap.add_argument("--output", type=Path, required=True)
    args = ap.parse_args()
    catalog = json.loads(args.catalog.read_text())
    midi_dir, role_dir = args.output/"midi", args.output/"roles"
    midi_dir.mkdir(parents=True,exist_ok=True)
    role_dir.mkdir(parents=True,exist_ok=True)
    for cue in HANDOFF:
        source = read_score(args.source/(cue+".mid"))
        revised = arrange(source,cue,catalog[cue])
        out = midi_dir/(cue+".mid")
        write_score(revised,out)
        verify_arrangement(source, revised, read_score(out))
        (role_dir/(cue+".json")).write_text(json.dumps({"style":revised.style, "lead":revised.lead})+"\n")
        print(f"{cue}: {sum(len(t.notes) for t in source.tracks)} -> {sum(len(t.notes) for t in revised.tracks)} notes")


if __name__ == "__main__":
    main()
