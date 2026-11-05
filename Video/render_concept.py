#!/usr/bin/env python3
"""Render the still-based concept film. Artwork is never modified by this script.

FFmpeg moves the five original images in video space. NumPy synthesizes only
time-varying light/particle VIDEO layers and original AUDIO, never still artwork.
Requires Python 3, NumPy, SciPy, FFmpeg and FFprobe.
"""
from __future__ import annotations

import concurrent.futures
import hashlib
import json
import math
from pathlib import Path
import subprocess
import wave

import numpy as np
from scipy.signal import butter, sosfilt

ROOT = Path(__file__).resolve().parent
WORK = ROOT.parent / "work" / "video"
WORK.mkdir(parents=True, exist_ok=True)
FPS, DURATION = 30, 12.0
W, H = 1920, 1080
VW, VH = 960, 540


def run(args):
    subprocess.run(args, check=True, stdout=subprocess.DEVNULL)


def camera_clip(index, frames, zoom, x, y):
    out = WORK / f"camera{index:02d}.mp4"
    vf = (f"scale=3840:-2:flags=lanczos,"
          f"zoompan=z='{zoom}':x='{x}':y='{y}':d={frames}:s={W}x{H}:fps={FPS},"
          "setsar=1,format=yuv420p")
    run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-threads", "2",
         "-i", str(ROOT / "Stills" / f"shot{index:02d}.png"), "-vf", vf,
         "-frames:v", str(frames), "-c:v", "libx264", "-preset", "fast",
         "-crf", "18", "-threads", "2", str(out)])
    print(f"Camera clip {index} ready", flush=True)
    return out


def glow(frame, x, y, radius, color, gain=1.0, hard=False):
    radius = max(1.0, radius)
    extent = int(radius * (1.15 if hard else 3.0)) + 1
    x0, x1 = max(0, int(x)-extent), min(VW, int(x)+extent+1)
    y0, y1 = max(0, int(y)-extent), min(VH, int(y)+extent+1)
    if x1 <= x0 or y1 <= y0:
        return
    yy, xx = np.ogrid[y0:y1, x0:x1]
    d = ((xx-x)**2 + (yy-y)**2) / radius**2
    a = np.maximum(0, 1-d) if hard else np.exp(-d * 1.7)
    frame[y0:y1, x0:x1] += a[..., None] * np.asarray(color) * gain


def line(frame, p0, p1, width, color, gain=1.0):
    x0, y0 = p0
    x1, y1 = p1
    r = max(2, int(width * 4))
    ax, bx = max(0, int(min(x0, x1))-r), min(VW, int(max(x0, x1))+r+1)
    ay, by = max(0, int(min(y0, y1))-r), min(VH, int(max(y0, y1))+r+1)
    if bx <= ax or by <= ay:
        return
    yy, xx = np.ogrid[ay:by, ax:bx]
    dx, dy = x1-x0, y1-y0
    u = np.clip(((xx-x0)*dx + (yy-y0)*dy)/(dx*dx+dy*dy+1e-6), 0, 1)
    d = ((xx-(x0+u*dx))**2 + (yy-(y0+u*dy))**2) / width**2
    a = np.exp(-d * 1.7)
    frame[ay:by, ax:bx] += a[..., None] * np.asarray(color) * gain


def ring(frame, x, y, radius, color, gain=1.0):
    extent = int(radius+20)
    ax, bx = max(0, int(x)-extent), min(VW, int(x)+extent+1)
    ay, by = max(0, int(y)-extent), min(VH, int(y)+extent+1)
    if bx <= ax or by <= ay:
        return
    yy, xx = np.ogrid[ay:by, ax:bx]
    r = np.sqrt((xx-x)**2+(yy-y)**2)
    a = np.exp(-((r-radius)/3.1)**2)
    frame[ay:by, ax:bx] += a[..., None] * np.asarray(color) * gain


def effects():
    out = WORK / "light_effects.mp4"
    command = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-f", "rawvideo",
               "-pixel_format", "rgb24", "-video_size", f"{VW}x{VH}", "-framerate", str(FPS),
               "-i", "-", "-an", "-c:v", "libx264", "-preset", "fast", "-crf", "17",
               "-pix_fmt", "yuv420p", "-threads", "2", str(out)]
    proc = subprocess.Popen(command, stdin=subprocess.PIPE)
    rng = np.random.default_rng(1787)
    dust = rng.uniform([0, 0, 0.0, 0.25], [VW, VH, 30.0, 0.9], (32, 4))
    sparks = rng.uniform([0, -math.pi, 120, 0.35], [0.15, math.pi, 570, 1.2], (85, 4))
    debris = rng.uniform([0, -math.pi, 80, 0.65], [0.5, math.pi, 440, 2.2], (65, 4))
    for n in range(int(FPS*DURATION)):
        t = n / FPS
        f = np.zeros((VH, VW, 3), dtype=np.float32)
        # Floating cyan particles supply actual temporal movement to the corridor.
        for px, py, speed, strength in dust:
            x = (px + 5*math.sin(t*.5+px)) % VW
            y = (py - speed*t) % VH
            glow(f, x, y, .8, (15, 80, 100), strength*.3)
        if t < 2.4:
            phase = max(0, (t-.45)/1.8)
            strength = .04 + .12*phase + .04*math.sin(t*16)**2
            glow(f, 612, 386, 45, (20, 180, 230), strength)
            if t > 2.03:
                u = min(1, (t-2.03)/.27)
                start, end = (731, 404), (731+290*u, 404+82*u)
                line(f, start, end, 9, (245, 127, 15), u*.75)
                line(f, start, end, 2.2, (255, 238, 137), u)
                glow(f, *start, 27, (255, 190, 65), .5*u)
        # A very short luminous cut connects arm activation to deployed blade.
        if 2.30 < t < 2.54:
            a = max(0, 1-abs(t-2.40)/.13)
            f += np.array([70, 100, 115], np.float32) * a*.75
        if 2.4 <= t < 4.5:
            pulse = .06+.035*math.sin(t*12)**2
            glow(f, 286, 287, 26, (20, 160, 220), pulse)
            # Traveling ignition glint along the long graphite-core blade.
            u = min(1, (t-2.4)/.8)
            if u < 1:
                glow(f, 350+550*u, 300+155*u, 24, (255, 190, 60), .35)
        if 6.8 <= t < 8.5:
            local = t-6.8
            # Slash sweeps across the robot in ten frames, then fades.
            if local < .48:
                u = np.clip(local/.28, 0, 1)
                p0 = (210, 428)
                p1 = (210+720*u, 428-300*u)
                a = min(1, local/.06)*max(0, 1-local/.5)
                line(f, p0, p1, 10, (255, 156, 25), a*.85)
                line(f, p0, p1, 2.2, (255, 244, 180), a)
                glow(f, 765, 151, 46, (255, 187, 55), a*.55)
            for delay, angle, speed, life in sparks:
                age = local-.12-delay
                if 0 < age < life:
                    vx, vy = math.cos(angle)*speed, math.sin(angle)*speed
                    px, py = 765+vx*age, 155+vy*age+100*age*age
                    tail = .015
                    line(f, (px-vx*tail, py-vy*tail), (px, py), 1.1,
                         (255, 155, 40), (1-age/life)*.85)
        if 8.5 <= t:
            age = t-8.5
            x, y = 684, 239
            flash = math.exp(-age*5.5)
            glow(f, x, y, 95+age*130, (255, 130, 30), flash*.7)
            glow(f, x, y, 35+age*90, (145, 210, 255), math.exp(-age*10)*.8)
            if age < 1.2:
                ring(f, x, y, 20+age*390, (255, 165, 65), math.exp(-age*2)*.32)
            for delay, angle, speed, life in debris:
                q = age-delay
                if 0 < q < life:
                    vx, vy = math.cos(angle)*speed, math.sin(angle)*speed
                    px, py = x+vx*q, y+vy*q+105*q*q
                    line(f, (px-vx*.025, py-vy*.025), (px, py), 1.3,
                         (255, 133, 30), (1-q/life)*.75)
                    glow(f, px, py, 2.5, (255, 155, 60), (1-q/life)*.2)
            f += np.array([32, 22, 10], np.float32)*flash
        proc.stdin.write(np.clip(f, 0, 255).astype(np.uint8).tobytes())
    proc.stdin.close()
    if proc.wait() != 0:
        raise RuntimeError("Video effects encoding failed")
    print("Animated light and particle layer ready", flush=True)
    return out


def audio():
    sr = 48000
    t = np.arange(round(sr*DURATION))/sr
    rng = np.random.default_rng(17287)
    stereo = np.zeros((len(t), 2), np.float64)
    bed = (.008*np.sin(2*np.pi*55*t) + .003*np.sin(2*np.pi*82.5*t))
    stereo += bed[:, None]

    def add(start, signal, pan=0.0):
        j = round(start*sr)
        count = min(len(signal), len(t)-j)
        left = math.sqrt((1-pan)*.5)
        right = math.sqrt((1+pan)*.5)
        stereo[j:j+count, 0] += signal[:count]*left
        stereo[j:j+count, 1] += signal[:count]*right

    q = np.arange(round(2.28*sr))/sr
    phase = 2*np.pi*(70*q+110*q*q)
    charge = (.05*np.sin(phase)+.025*np.sin(phase*2.007))*(q/2.28)**1.8
    charge *= np.minimum(1, q/.12)*np.minimum(1, (2.28-q)/.04)
    add(.05, charge, -.2)

    def noise(duration, cutoff, high=False):
        n = rng.normal(0, 1, round(duration*sr))
        return sosfilt(butter(3, cutoff, fs=sr, btype="highpass" if high else "lowpass", output="sos"), n)

    for start, duration, level, pan in [(2.18,.48,.24,.2), (6.72,.4,.31,.2)]:
        q = np.arange(round(duration*sr))/sr
        shape = np.sin(np.pi*q/duration)**1.8
        add(start, noise(duration, 4200)*shape*level, pan)
    for start in (4.95, 5.75):
        q=np.arange(round(.24*sr))/sr
        foot=(.04*np.sin(2*np.pi*90*q)+noise(.24, 650)*.06)*np.exp(-q*25)
        add(start, foot, -.25)
    q=np.arange(round(1.15*sr))/sr
    metallic = sum(np.sin(2*np.pi*h*q)*a for h,a in [(530,.13),(1131,.1),(2093,.045),(3499,.025)])
    hit = metallic*np.exp(-q*8)+noise(1.15, 6000)*np.exp(-q*23)*.25
    add(6.98, hit, .48)
    q=np.arange(round(3.5*sr))/sr
    blast=noise(3.5, 1000)*np.exp(-q*1.6)*.48
    blast+=noise(3.5, 6000)*np.exp(-q*5)*.14
    blast+=.24*np.sin(2*np.pi*(52*q-3*q*q))*np.exp(-q*2.0)
    add(8.5, blast, .32)
    # Short quiet reflections, then a graceful fade to silence.
    for delay, strength in [(.105,.15),(.23,.09)]:
        k=round(sr*delay)
        stereo[k:] += stereo[:-k].copy()*strength
    fade=np.minimum(1,t/.25)*np.minimum(1,(DURATION-t)/.65)
    stereo *= fade[:,None]
    peak=np.max(np.abs(stereo))
    stereo *= min(1.0,.88/max(peak,1e-6))
    path=WORK/"original_synth_sound.wav"
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(sr)
        wav.writeframes((np.clip(stereo,-1,1)*32767).astype("<i2").tobytes())
    print("Original synthesized sound ready", flush=True)
    return path


def main():
    # Each shot changes angle; only the camera and composited effects move.
    configs = [
        (1,72,"1.015+0.045*on/71", "iw/2-iw/zoom/2+90*on/71", "ih/2-ih/zoom/2+30*on/71"),
        (2,63,"1.025+0.025*on/62", "iw/2-iw/zoom/2+28*on/62", "ih/2-ih/zoom/2"),
        (3,72,"1.025-0.025*on/71", "0", "ih/2-ih/zoom/2"),
        (4,48,"1.012+0.04*on/47", "iw/2-iw/zoom/2+12*sin(on*2.3)*exp(-on/9)", "ih/2-ih/zoom/2+7*sin(on*2.1)*exp(-on/9)"),
        (5,105,"1.04-0.03*on/104", "iw/2-iw/zoom/2+14*sin(on*2.2)*exp(-on/14)", "ih/2-ih/zoom/2+8*sin(on*2.5)*exp(-on/14)"),
    ]
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        futures=[pool.submit(camera_clip, *c) for c in configs]
        clips=[f.result() for f in futures]
    listing=WORK/"camera_concat.txt"
    listing.write_text("".join(f"file '{p.as_posix()}'\n" for p in clips))
    base=WORK/"camera_edit.mp4"
    run(["ffmpeg","-hide_banner","-loglevel","error","-y","-f","concat","-safe","0",
         "-i",str(listing),"-c","copy",str(base)])
    fx=effects()
    sound=audio()
    output=ROOT/"Officer_Blade_Concept.mp4"
    graph=("[0:v]format=gbrp[base];[1:v]scale=1920:1080:flags=bilinear,format=gbrp[fx];"
           "[base][fx]blend=all_mode=screen:shortest=1,format=yuv420p,"
           "fade=t=in:st=0:d=0.28,fade=t=out:st=11.55:d=0.45[v]")
    run(["ffmpeg","-hide_banner","-loglevel","error","-y","-i",str(base),"-i",str(fx),
         "-i",str(sound),"-filter_complex_threads","2","-filter_complex",graph,
         "-map","[v]","-map","2:a:0","-t","12","-c:v","libx264","-preset","slow",
         "-crf","19","-threads","3","-pix_fmt","yuv420p","-c:a","aac","-b:a","192k",
         "-movflags","+faststart","-metadata","title=Officer Blade - Cinematic Concept Animatic",
         "-metadata","comment=Generated still artwork, camera animation and composited video effects. Not gameplay.",
         str(output)])
    probe=json.loads(subprocess.check_output(["ffprobe","-v","error","-show_format","-show_streams","-of","json",str(output)]))
    (ROOT/"technical_metadata.json").write_text(json.dumps(probe,indent=2)+"\n")
    for i,seconds in enumerate((1.4,3.2,7.15,9.0),1):
        run(["ffmpeg","-hide_banner","-loglevel","error","-y","-ss",str(seconds),"-i",str(output),
             "-frames:v","1",str(ROOT/"Preview"/f"frame{i:02d}.png")])
    print(f"Complete: {output} ({output.stat().st_size:,} bytes)",flush=True)


if __name__ == "__main__":
    main()
