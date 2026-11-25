"""Render the 35 field, title, chase and signal cues from their arranged MIDI scores.

Requires Python 3, NumPy, SciPy, libfluidsynth 2.x and FFmpeg with libvorbis.
TimGM6mb is an external rendering instrument and is not shipped in the game.
"""
import argparse
import ctypes
import ctypes.util
import json
import math
import os
import subprocess
from pathlib import Path

import numpy as np
from scipy import ndimage, signal

from ensemble_score import read_score

SR = 44100
ROOT = Path(__file__).resolve().parents[1]
F32 = np.float32
# Role balance is measured at the active portion of a take. Sparse phrases keep
# their natural space and don't get boosted to the energy of a continuous bed.
# Values: active-window RMS dB, stereo seat, HPF, LPF, room send.
SEATS = {
    'piano': (-18, -.12, 108, 10600, .85), 'piano2': (-23, .17, 130, 8000, 1.08),
    'leadguitar': (-18, .07, 130, 5400, .9), 'rhythmguitar': (-23, -.21, 110, 4200, .75),
    'cleanguitar': (-21, .23, 130, 6100, .95),
    'bass': (-16.2, 0, 42, 2900, .10), 'pickbass': (-16.2, 0, 42, 3600, .10),
    'circuit bass': (-24, 0, 41, 640, .0),
    'drums': (-15, 0, 39, 14400, .66), 'janggu': (-26, -.13, 120, 6900, .9),
    'buk': (-23, .04, 55, 3400, .88), 'kkwaenggwari': (-29, .20, 740, 8200, 1.0),
    'gayageum': (-24, .17, 150, 8000, 1.1), 'geomungo': (-23, -.07, 71, 3900, .8),
    'taepyeongso': (-23, .12, 170, 4200, .85), 'piri': (-23, .08, 210, 4600, .95),
    'daegeum': (-21, .09, 150, 6200, 1.13), 'haegeum': (-23, .16, 150, 6000, 1.18),
    'ajaeng': (-22, -.11, 82, 4300, 1.1), 'glass': (-27, -.15, 550, 7700, 1.05),
    'coil': (-28, .09, 190, 5400, .85), 'synth': (-22, -.08, 84, 6300, .85),
    'pad': (-26, .21, 174, 5100, 1.25), 'strings': (-23, .16, 168, 7600, 1.28),
    'spiccato': (-22, -.17, 136, 6900, 1.07), 'cello': (-21, -.10, 85, 5100, 1.13),
    'brass': (-23, .15, 150, 6100, 1.16), 'harp': (-24, -.17, 130, 7400, 1.12),
    'organ': (-24, -.13, 148, 4900, 1.1), 'timpani': (-22, -.08, 52, 3800, 1.12),
}
PATCH = {'leadguitar': 29, 'rhythmguitar': 28, 'cleanguitar': 27, 'circuit bass': 33,
         'piri': 68, 'taepyeongso': 68, 'haegeum': 40, 'ajaeng': 42, 'spiccato': 45,
         'coil': 11, 'synth': 81, 'glass': 10, 'kkwaenggwari': 112}


def coef(db):
    return 10 ** (db / 20)


def filter_audio(x, cutoff, kind):
    sos = signal.butter(2, cutoff, fs=SR, btype=kind, output='sos').astype(F32)
    return signal.sosfilt(sos, x, axis=0)


class DrySampler:
    """All parts share the same clock. No per-patch synthetic reverb or chorus."""
    def __init__(self, sf2):
        self.lib = ctypes.CDLL(ctypes.util.find_library('fluidsynth'))
        ptr, integer, string = ctypes.c_void_p, ctypes.c_int, ctypes.c_char_p
        signatures = {
            'new_fluid_settings': (ptr, []), 'delete_fluid_settings': (None, [ptr]),
            'fluid_settings_setnum': (integer, [ptr, string, ctypes.c_double]),
            'fluid_settings_setint': (integer, [ptr, string, integer]),
            'new_fluid_synth': (ptr, [ptr]), 'delete_fluid_synth': (None, [ptr]),
            'fluid_synth_sfload': (integer, [ptr, string, integer]),
            'fluid_synth_program_select': (integer, [ptr, integer, integer, integer, integer]),
            'fluid_synth_all_sounds_off': (integer, [ptr, integer]),
            'fluid_synth_noteon': (integer, [ptr, integer, integer, integer]),
            'fluid_synth_noteoff': (integer, [ptr, integer, integer]),
            'fluid_synth_cc': (integer, [ptr, integer, integer, integer]),
            'fluid_synth_write_float': (integer, [ptr, integer, ptr, integer, integer, ptr, integer, integer]),
        }
        for name, (ret, args) in signatures.items():
            f = getattr(self.lib, name)
            f.restype, f.argtypes = ret, args
        self.settings = self.lib.new_fluid_settings()
        self.lib.fluid_settings_setnum(self.settings, b'synth.sample-rate', SR)
        self.lib.fluid_settings_setnum(self.settings, b'synth.gain', .7)
        for field in ('synth.reverb.active', 'synth.chorus.active'):
            self.lib.fluid_settings_setint(self.settings, field.encode(), 0)
        self.synth = self.lib.new_fluid_synth(self.settings)
        self.bank = self.lib.fluid_synth_sfload(self.synth, os.fsencode(sf2), 1)
        if self.bank < 0:
            raise RuntimeError(f'Cannot load SoundFont {sf2}')

    def close(self):
        self.lib.delete_fluid_synth(self.synth)
        self.lib.delete_fluid_settings(self.settings)

    def take(self, track, frames, note_frame):
        drums = track.name == 'drums'
        channel, bank = (9, 128) if drums else (0, 0)
        self.lib.fluid_synth_all_sounds_off(self.synth, channel)
        program = 0 if drums else PATCH.get(track.name, track.program)
        if self.lib.fluid_synth_program_select(self.synth, channel, self.bank, bank, program) < 0:
            raise RuntimeError(f'Cannot render {track.name} (bank {bank}, patch {program})')
        for cc, val in ((7, 106), (10, 64), (11, 118), (64, 0), (91, 0), (93, 0)):
            self.lib.fluid_synth_cc(self.synth, channel, cc, val)
        events = []
        for i, n in enumerate(track.notes):
            start, end = note_frame(n.start), max(note_frame(n.start) + 1, note_frame(n.end))
            if start >= frames:
                continue
            events.extend(((start, 1, i, n.pitch, n.velocity), (min(frames, end), 0, i, n.pitch, 0)))
        output = np.zeros((frames, 2), F32)
        cursor = 0
        active = {}
        for t, attack, ident, key, velocity in sorted(events) + [(frames, 0, -1, -1, 0)]:
            if t > cursor:
                address = output.ctypes.data + cursor * 8
                self.lib.fluid_synth_write_float(self.synth, t - cursor, address, 0, 2, address, 1, 2)
                cursor = t
            if key < 0:
                break
            if attack:
                self.lib.fluid_synth_noteon(self.synth, channel, key, velocity)
                active[key] = ident
            elif active.get(key) == ident:
                self.lib.fluid_synth_noteoff(self.synth, channel, key)
                del active[key]
        return output


def physical_take(track, frames, note_frame):
    """Resonating strings and paired drum skins, articulated per written note."""
    name = track.name
    out = np.zeros(frames, F32)
    for i, n in enumerate(track.notes):
        start = note_frame(n.start)
        if start >= frames:
            continue
        played = (note_frame(n.end) - start) / SR
        length = min(frames-start, round((max(.055, played) + .23) * SR))
        t = np.arange(length, dtype=F32) / SR
        stroke = n.velocity / 100
        if name in ('gayageum', 'geomungo'):
            low = name == 'geomungo'
            frequency = 440 * 2 ** ((n.pitch - 69) / 12)
            # A short initial bridge deflection relaxes into the fretted pitch.
            radians = 2 * np.pi * frequency * (t + (.00019 if low else .00009) * (1 - np.exp(-t/.028)))
            wave = np.zeros(length, F32)
            for h in range(1, 8 if low else 10):
                if frequency*h >= SR*.43:
                    break
                partial = np.sin(np.pi*h*(.245 if low else .19)) / h**1.32
                wave += partial * np.sin(radians*h) * np.exp(-t * (1.65 + h*(.95 if low else .66)))
            env = (1 - np.exp(-t/.0022)) * np.exp(-np.maximum(t-played, 0)/.041)
            sample = wave * env
        else:
            deep = name == 'buk' or n.pitch % 2 == 0
            fundamental = (72 if name == 'buk' else 116) if deep else 215
            phase = 2*np.pi*(fundamental*t + .72*(1-np.exp(-t/.022)))
            body = np.sin(phase) + .37*np.sin(phase*1.52) + .18*np.sin(phase*2.26)
            skin = np.random.default_rng(i+1597).normal(0, 1, length).astype(F32)
            skin = filter_audio(skin, (620, 3300 if deep else 5500), 'bandpass')
            sample = (body*np.exp(-t/(.096 if deep else .058)) + .65*skin*np.exp(-t/.022))*(1-np.exp(-t/.0016))
        out[start:start+length] += stroke**1.55 * sample
    return np.column_stack((out, out))


def low_shelf(stereo, frequency, gain_db):
    gain = 10 ** (gain_db / 40)
    omega = 2 * np.pi * frequency / SR
    cosine = np.cos(omega)
    slope = np.sqrt(2 * gain) * np.sin(omega)
    b = gain * np.array([(gain+1) - (gain-1)*cosine + slope,
                         2*((gain-1) - (gain+1)*cosine),
                         (gain+1) - (gain-1)*cosine - slope])
    a = np.array([(gain+1) + (gain-1)*cosine + slope,
                  -2*((gain-1) + (gain+1)*cosine),
                  (gain+1) + (gain-1)*cosine - slope])
    return signal.lfilter(b, a, stereo, axis=0).astype(F32)


def active_level(x):
    block = 2048
    if len(x) < block:
        return 0
    mono = x[:len(x)//block*block].mean(axis=1).reshape(-1, block)
    rms = np.sqrt(np.mean(mono*mono, axis=1))
    active = rms[rms > max(.000001, rms.max() * .15)]
    return float(np.percentile(active, 72)) if len(active) else 0


def phrase_controls(lead_roles, name, bar_length, frames):
    # Only written phrases come forward, and only one instrument takes centre.
    active = np.array([part == name for part in lead_roles], dtype=F32)
    if not active.any():
        return None
    transitions = np.arange(len(active)+1) * bar_length
    x, y = [0.0], [active[0]]
    for i in range(1, len(active)):
        if active[i] != active[i-1]:
            x.extend((transitions[i] - min(SR*.045, .1*bar_length), transitions[i] + SR*.028))
            y.extend((active[i-1], active[i]))
    return np.interp(np.arange(frames), x, y).astype(F32)


def render_cue(sampler, source, roles, cue, info, target, output):
    total = int(info['frames'])
    tail = round(1.2*SR)
    samples = total + (tail if info['loop'] else 0)
    score = read_score(source)
    beat = 60.0 / info['bpm'] * SR
    def at(tick):
        return max(0, round(tick * beat / score.ppq))
    dry = np.zeros((total, 2), F32)
    space = np.zeros(total, F32)
    leads, style = roles['lead'], roles['style']
    for track in score.tracks:
        if not track.notes:
            continue
        name = track.name
        volume, pan, hp, lp, send = SEATS[name]
        take = (physical_take(track, samples, at) if name in ('gayageum','geomungo','buk','janggu')
                else sampler.take(track, samples, at))
        take = filter_audio(filter_audio(take, hp, 'highpass'), lp, 'lowpass')
        level = active_level(take)
        if level < 1e-7:
            continue
        take *= 1 / level
        mid = .5*(take[:,0]+take[:,1])
        sides = .5*(take[:,0]-take[:,1])
        if name in ('rhythmguitar','leadguitar'):
            # Cabinet and gently driven pickup density stay in the middle register.
            drive = 1.1 if name == 'rhythmguitar' else .76
            mid = .61*mid + .54*np.tanh(mid*drive)
        if name in ('bass','pickbass'):
            mid = .80*mid + .27*np.tanh(mid*1.4)
        if name == 'drums' and style in ('atmosphere','stinger'):
            volume -= 4
        if name in ('coil','glass','kkwaenggwari') and style == 'drive':
            volume -= 1.5
        scale = coef(volume)
        mid *= scale
        sides *= scale*(.34 if name in ('drums','piano','strings','pad') else .13)
        if info['loop']:
            mid[:tail] += mid[total:total+tail]
            sides[:tail] += sides[total:total+tail]
        mid, sides = mid[:total], sides[:total]
        foreground = phrase_controls(leads, name, beat*4, total)
        if foreground is None:
            seat = pan
        else:
            mid *= 1 + .72*foreground
            seat = pan*(1-.78*foreground)
        dry[:,0] += mid*np.sqrt(1-seat) + sides
        dry[:,1] += mid*np.sqrt(1+seat) - sides
        space += mid*send
    # A single shared small room, with a short, spectrally damped tail. The
    # same stage/return serves all patches; the bass sends very little to it.
    space = filter_audio(filter_audio(space, 240, 'highpass'), 6000, 'lowpass')
    for ch, taps in enumerate((((.012,.064),(.027,.040),(.044,.032),(.078,.023),(.111,.013)),
                               ((.017,.065),(.033,.046),(.054,.028),(.083,.021),(.123,.012)))):
        for seconds, level in taps:
            shift = round(seconds*SR)
            dry[shift:,ch] += space[:-shift]*level
            if info['loop']:
                dry[:shift,ch] += space[-shift:]*level
    # Keep the electric bass and drum fundamentals centred, while letting the
    # upper room and players span two speakers. Fast cues get a modest low
    # shelf for the drum shell and the bass fundamental on small systems.
    side = (dry[:,0] - dry[:,1]) * .5
    width = .78 if style in ('drive', 'groove') else .62
    air = filter_audio(side, 205, 'highpass') * width
    dry[:,0] += air
    dry[:,1] -= air
    weight = {'drive': 2.8, 'groove': 2.1, 'precision': 1.8,
              'atmosphere': .55, 'stinger': .9}.get(style, 1.2)
    dry = low_shelf(dry, 172, weight)
    if not info['loop']:
        length = min(total, round(.13*SR))
        dry[-length:] *= np.linspace(1,0,length,dtype=F32)[:,None]
    # Use only a few ms of boundary correction to remove the tiny nonperiodic
    # remainder left by filters/patch releases, without softening beat one.
    else:
        n = 220
        step = dry[0] - dry[-1]
        dry[-n:] += np.linspace(0,1,n,dtype=F32)[:,None]*step

    dry *= coef(target - loudness(dry))
    # A common linked lookahead limiter. Kick and melody keep their relative
    # attacks; no independent pumping of stereo instruments.
    window = round(.003*SR)
    length = len(dry)
    padded = np.pad(np.max(np.abs(dry),axis=1), (0, (-length) % window), mode='edge')
    peaks = padded.reshape(-1,window).max(axis=1)
    gain = np.minimum(1, .775 / np.maximum(peaks,1e-6))
    anticipate = np.minimum(gain, np.roll(gain,-1))
    if not info['loop']:
        anticipate[-1] = gain[-1]
    target_gain = anticipate.copy()
    release = math.exp(-window/(SR*.065))
    prev = 1.0
    for repeat in range(2 if info['loop'] else 1):
        for i,g in enumerate(target_gain):
            prev = min(float(g), prev*release + (1-release))
            gain[i] = prev
    dry *= np.interp(np.arange(length), np.arange(len(gain))*window,gain).astype(F32)[:,None]
    peak = float(np.max(np.abs(dry)))
    if peak > .80:
        dry *= .80 / peak
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_name('.' + output.stem + '.writing.ogg')
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-fflags','+bitexact','-f','f32le','-ar',str(SR),
                    '-ac','2','-i','pipe:0','-c:a','libvorbis','-q:a','6','-flags:a','+bitexact','-fflags','+bitexact','-map_metadata','-1',
                    str(temporary)],input=dry.astype('<f4',copy=False).tobytes(),check=True)
    temporary.replace(output)
    return loudness(dry), float(np.max(np.abs(dry)))


def loudness(samples):
    # ITU-R BS.1770 K-weighting at 44.1 kHz, with overlapping 400 ms gates.
    pre = signal.lfilter([1.5308412,-2.650980,1.1690791],[1.,-1.6636551,.7125954],samples,axis=0)
    k = signal.lfilter([1,-2,1],[1.,-1.9891697,.9891990],pre,axis=0)
    energy = np.r_[0.,np.cumsum(np.sum(k*k,axis=1))]
    a=np.arange(0,len(samples)-17640+1,4410)
    blocks = (energy[a+17640]-energy[a])/17640 if len(a) else energy[-1:]/max(1,len(samples))
    usable=blocks[blocks>10**((-70+.691)/10)]
    if not len(usable):
        return -70.
    gated=usable[usable>usable.mean()*.1]
    return float(-.691+10*np.log10(np.mean(gated)))


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--midi',required=True,type=Path)
    parser.add_argument('--roles',required=True,type=Path)
    parser.add_argument('--output',required=True,type=Path)
    parser.add_argument('--soundfont',type=Path,default=Path('/usr/share/sounds/sf2/TimGM6mb.sf2'))
    parser.add_argument('--targets',type=Path,default=Path(__file__).with_name('field_loudness.json'))
    parser.add_argument('cues',nargs='*')
    args=parser.parse_args()
    catalog=json.loads((ROOT/'catalog.json').read_text())
    levels=json.loads(args.targets.read_text())
    for cue in args.cues or levels:
        sampler=DrySampler(args.soundfont)
        try:
            roles=json.loads((args.roles/f'{cue}.json').read_text())
            lufs,peak=render_cue(sampler,args.midi/f'{cue}.mid',roles,cue,catalog[cue],levels[cue],args.output/f'{cue}.ogg')
            print(f'{cue}: {catalog[cue]["frames"]} frames; {lufs:.2f} LUFS; PCM peak {peak:.3f}',flush=True)
        finally:
            sampler.close()


if __name__=='__main__':
    main()
