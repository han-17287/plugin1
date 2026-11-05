#!/usr/bin/env python3
"""Native-60fps concept animatic, using unchanged image-generator artwork.

Only video compositing, procedural video masks/lights and synthesized audio are
created here. No source image is retouched. This is not a rigged character film.
Requires Python3, NumPy, SciPy, FFmpeg and FFprobe. Run from any directory.
"""
from __future__ import annotations

import argparse
import concurrent.futures
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import subprocess
import sys

import numpy as np

ROOT=Path(__file__).resolve().parent
WORK=ROOT.parent/"work"/"video60"
WORK.mkdir(parents=True,exist_ok=True)
FPS,DURATION=60,12.0
W,H=1920,1080
VW,VH=960,540
EMITTER=(.383,.590)
TIP=(.970,.898)

spec=importlib.util.spec_from_file_location("original_concept",ROOT/"render_concept.py")
original=importlib.util.module_from_spec(spec)
sys.dont_write_bytecode=True
spec.loader.exec_module(original)
original.WORK=WORK


def run(args):
    subprocess.run(args,check=True,stdout=subprocess.DEVNULL)


def smooth(x):
    x=np.clip(x,0,1)
    return x*x*(3-2*x)


def window(t,start,end):
    return float(smooth((t-start)/(end-start)))


def point_in_first_camera(x,y,t):
    u=window(t,0,4.5)
    z=1.012+.035*u
    # Same source-space camera used for both matched arm images and the mask.
    return ((z*(x-.5)+.5-36*u*z/5760)*VW,
            (z*(y-.5)+.5-12*u*z/(5760*941/1671))*VH)


def source(index):
    corrected=ROOT/"Stills60"/f"shot{index:02d}.png"
    return corrected if corrected.exists() else ROOT/"Stills"/f"shot{index:02d}.png"


def camera_clip(index,frames,zoom,x,y):
    path=source(index)
    if index<=2 and not (ROOT/"Stills60"/f"shot{index:02d}.png").exists():
        raise RuntimeError("Both corrected matched arm images must be present in Video/Stills60")
    out=WORK/f"camera{index:02d}.mp4"
    stamp=WORK/f"camera{index:02d}.json"
    identity={"sha256":hashlib.sha256(path.read_bytes()).hexdigest(),"frames":frames,
              "zoom":zoom,"x":x,"y":y,"fps":FPS}
    if out.exists() and stamp.exists() and json.loads(stamp.read_text())==identity:
        return out
    vf=(f"scale=5760:-2:flags=lanczos,zoompan=z='{zoom}':x='{x}':y='{y}':"
        f"d={frames}:s={W}x{H}:fps={FPS},setsar=1,format=yuv420p")
    run(["ffmpeg","-hide_banner","-loglevel","error","-y","-threads","2",
         "-i",str(path),"-vf",vf,"-frames:v",str(frames),"-c:v","libx264",
         "-preset","fast","-crf","17","-threads","2",str(out)])
    stamp.write_text(json.dumps(identity,indent=2)+"\n")
    print(f"Native60 camera {index} ready",flush=True)
    return out


def encoder(path,pixel_format="rgb24",codec="libx264"):
    args=["ffmpeg","-hide_banner","-loglevel","error","-y","-f","rawvideo",
          "-pixel_format",pixel_format,"-video_size",f"{VW}x{VH}","-framerate",str(FPS),
          "-i","-","-an","-c:v",codec]
    if codec=="libx264":args += ["-preset","fast","-crf","16","-pix_fmt","yuv420p","-threads","2"]
    else:args += ["-pix_fmt","gray","-threads","2"]
    return subprocess.Popen(args+[str(path)],stdin=subprocess.PIPE)


def finish(proc,message):
    proc.stdin.close()
    if proc.wait()!=0:raise RuntimeError(message)


def deployment_mask():
    """Moving video mask reveals the new blade from its emitter to its tip.

    The closed-arm background and officer remain unchanged throughout the shot.
    Only a feathered polygon around the blade uses the deployed source image.
    """
    path=WORK/"deployment_mask.mkv"
    proc=encoder(path,"gray","ffv1")
    yy,xx=np.mgrid[:VH,:VW].astype(np.float32)
    for n in range(round(4.5*FPS)):
        t=n/FPS
        p=1.08*window(t,1.72,2.72)
        x0,y0=point_in_first_camera(*EMITTER,t)
        x1,y1=point_in_first_camera(*TIP,t)
        dx,dy=x1-x0,y1-y0
        axis=((xx-x0)*dx+(yy-y0)*dy)/(dx*dx+dy*dy)
        polygon=[(585/1672,500/941),(1330/1672,683/941),(1643/1672,857/941),
                 (1450/1672,855/941),(580/1672,593/941)]
        vertices=[point_in_first_camera(x,y,t) for x,y in polygon]
        distance=np.full_like(xx,10000)
        for a,b in zip(vertices,vertices[1:]+vertices[:1]):
            ex,ey=b[0]-a[0],b[1]-a[1]
            signed=(ex*(yy-a[1])-ey*(xx-a[0]))/math.hypot(ex,ey)
            distance=np.minimum(distance,signed)
        band=np.clip((distance+3)/6,0,1)
        front=np.clip((p-axis+.025)/.05,0,1)*window(t,1.70,1.78)
        value=band*front
        proc.stdin.write(np.rint(value*255).astype(np.uint8).tobytes())
    finish(proc,"Deployment video mask encoding failed")
    print("Spatial blade-extension mask ready",flush=True)
    return path


def effects():
    path=WORK/"native60_lights.mp4"
    proc=encoder(path)
    rng=np.random.default_rng(1728760)
    dust=rng.uniform([0,0,4,.20],[VW,VH,19,.65],(26,4))
    sparks=rng.uniform([0,-math.pi,100,.30],[.14,math.pi,470,.9],(72,4))
    debris=rng.uniform([0,-math.pi,85,.65],[.4,math.pi,370,1.8],(56,4))
    for n in range(round(FPS*DURATION)):
        t=n/FPS
        f=np.zeros((VH,VW,3),np.float32)
        for px,py,speed,strength in dust:
            original.glow(f,(px+3*math.sin(t*.4+px))%VW,(py-speed*t)%VH,.7,
                          (15,80,105),strength*.2)
        if t<4.5:
            # A stable emitter activates; the moving ignition front follows the
            # spatially revealed blade instead of appearing from a different arm.
            x0,y0=point_in_first_camera(*EMITTER,t)
            cx,cy=point_in_first_camera(.310,.540,t)
            energy=window(t,.35,1.6)
            original.glow(f,cx,cy,22,(10,135,195),.045+.10*energy)
            if 1.68<t<3.1:
                p=window(t,1.72,2.72)
                tx,ty=point_in_first_camera(*TIP,t)
                end=(x0+(tx-x0)*p,y0+(ty-y0)*p)
                strength=window(t,1.65,1.82)*(1-window(t,2.72,3.1))
                original.line(f,(x0,y0),end,4.6,(255,156,20),strength*.34)
                original.line(f,(x0,y0),end,.85,(255,242,150),strength*.6)
                original.glow(f,*end,13,(255,178,35),strength*.45)
                original.glow(f,x0,y0,22,(255,170,50),strength*.22)
            # Brief gold wash motivates the end of the physical extension.
            original.glow(f,x0,y0,39,(255,155,40),math.exp(-((t-2.68)/.14)**2)*.2)
        # Motivated cuts: light sweeps are brief rather than a long dissolve of
        # unrelated poses. Camera changes still remain honest scene-angle cuts.
        if 4.43<t<4.57:
            a=math.exp(-((t-4.50)/.038)**2)
            f += np.array([13,23,28],np.float32)*a
        if 6.83<t<7.23:
            local=t-6.83
            p=window(local,0,.24)
            a=window(local,0,.045)*(1-window(local,.21,.40))
            p0=(185,428);p1=(185+735*p,428-306*p)
            original.line(f,p0,p1,8.5,(255,151,25),a*.70)
            original.line(f,p0,p1,1.4,(255,245,185),a*.9)
            original.glow(f,765,153,33,(255,185,45),a*.35)
        if 6.9<=t<8.5:
            local=t-6.9
            for delay,angle,speed,life in sparks:
                age=local-.08-delay
                if 0<age<life:
                    vx,vy=math.cos(angle)*speed,math.sin(angle)*speed
                    x,y=765+vx*age,155+vy*age+100*age*age
                    original.line(f,(x-vx*.012,y-vy*.012),(x,y),.9,
                                  (255,158,45),(1-age/life)*.65)
        if t>=8.5:
            age=t-8.5;x,y=684,239
            original.glow(f,x,y,90+age*120,(255,137,35),math.exp(-age*5)*.65)
            original.glow(f,x,y,28+age*80,(165,220,255),math.exp(-age*11)*.65)
            if age<1.1:original.ring(f,x,y,18+age*350,(255,173,75),math.exp(-age*2.4)*.25)
            for delay,angle,speed,life in debris:
                q=age-delay
                if 0<q<life:
                    vx,vy=math.cos(angle)*speed,math.sin(angle)*speed
                    xq,yq=x+vx*q,y+vy*q+105*q*q
                    original.line(f,(xq-vx*.018,yq-vy*.018),(xq,yq),1,
                                  (255,137,35),(1-q/life)*.65)
            f += np.array([26,19,10],np.float32)*math.exp(-age*6)
        proc.stdin.write(np.clip(f,0,255).astype(np.uint8).tobytes())
    finish(proc,"Native60 light effects encoding failed")
    print("Native60 light/particle motion ready",flush=True)
    return path


def configs():
    ease=lambda end:f"(on/{end})*(on/{end})*(3-2*(on/{end}))"
    e=ease(269)
    first=(270,f"1.012+.035*({e})",f"iw/2-iw/zoom/2+36*({e})",f"ih/2-ih/zoom/2+12*({e})")
    e3=ease(143);e4=ease(95);e5=ease(209)
    return [
        (1,*first),(2,*first),
        (3,144,f"1.035-.018*({e3})",f"iw/2-iw/zoom/2-22*({e3})","ih/2-ih/zoom/2"),
        (4,96,f"1.015+.024*({e4})",f"iw/2-iw/zoom/2+8*sin(2*PI*4.2*on/{FPS})*exp(-on/{FPS}/.28)",
         f"ih/2-ih/zoom/2+5*sin(2*PI*5.1*on/{FPS})*exp(-on/{FPS}/.26)"),
        (5,210,f"1.037-.023*({e5})",f"iw/2-iw/zoom/2+10*sin(2*PI*4.6*on/{FPS})*exp(-on/{FPS}/.40)",
         f"ih/2-ih/zoom/2+6*sin(2*PI*5.3*on/{FPS})*exp(-on/{FPS}/.36)"),
    ]


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--prepare",action="store_true",help="Prepare closed-arm and combat camera clips, mask, sound and effects while the deployed still is generated")
    parser.add_argument("--reuse-effects",action="store_true",help="Reuse already rendered native60 effects/audio; always rebuild the blade mask")
    args=parser.parse_args()
    tasks=[c for c in configs() if not args.prepare or c[0]!=2]
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        fs=[pool.submit(camera_clip,*c) for c in tasks]
        clips={c[0]:f.result() for c,f in zip(tasks,fs)}
    mask=deployment_mask()
    fx=WORK/"native60_lights.mp4" if args.reuse_effects else effects()
    sound=WORK/"original_synth_sound.wav" if args.reuse_effects else original.audio()
    if args.prepare:
        print("Preparation complete; waiting for corrected deployed frame",flush=True)
        return
    deployed=WORK/"arm_deployment.mp4"
    graph=("[0:v]format=gbrp[closed];[1:v]format=gbrp[open];"
           f"[2:v]scale={W}:{H}:flags=bilinear:in_range=full:out_range=full,format=gbrp[mask];"
           "[closed][open][mask]maskedmerge,format=yuv420p[v]")
    run(["ffmpeg","-hide_banner","-loglevel","error","-y","-i",str(clips[1]),
         "-i",str(clips[2]),"-i",str(mask),"-filter_complex_threads","2","-filter_complex",graph,
         "-map","[v]","-frames:v","270","-c:v","libx264","-preset","fast","-crf","17","-threads","2",str(deployed)])
    listing=WORK/"camera_concat.txt"
    listing.write_text("".join(f"file '{p.as_posix()}'\n" for p in [deployed,clips[3],clips[4],clips[5]]))
    base=WORK/"camera_edit.mp4"
    run(["ffmpeg","-hide_banner","-loglevel","error","-y","-f","concat","-safe","0",
         "-i",str(listing),"-c","copy",str(base)])
    output=ROOT/"Officer_Blade_Concept_60fps.mp4"
    graph=(f"[0:v]format=gbrp[base];[1:v]scale={W}:{H}:flags=bilinear,format=gbrp[fx];"
           "[base][fx]blend=all_mode=screen:shortest=1,format=yuv420p,"
           "fade=t=in:st=0:d=0.24,fade=t=out:st=11.55:d=0.45[v]")
    run(["ffmpeg","-hide_banner","-loglevel","error","-y","-i",str(base),"-i",str(fx),
         "-i",str(sound),"-filter_complex_threads","2","-filter_complex",graph,"-map","[v]","-map","2:a:0",
         "-t","12","-r",str(FPS),"-fps_mode","cfr","-c:v","libx264","-preset","slow","-crf","19",
         "-threads","3","-pix_fmt","yuv420p","-c:a","aac","-b:a","192k","-movflags","+faststart",
         "-metadata","title=Officer Blade - Native60 Concept Animatic",
         "-metadata","comment=Corrected matched arm stills; native60 camera, spatial blade reveal and composited effects. Not gameplay or continuous rigged character animation.",str(output)])
    probe=json.loads(subprocess.check_output(["ffprobe","-v","error","-show_format","-show_streams","-of","json",str(output)]))
    (ROOT/"technical_metadata_60fps.json").write_text(json.dumps(probe,indent=2)+"\n")
    previews=ROOT/"Preview60";previews.mkdir(exist_ok=True)
    for i,second in enumerate((.8,1.95,2.30,2.60,3.3,7.1,9.0),1):
        run(["ffmpeg","-hide_banner","-loglevel","error","-y","-ss",str(second),"-i",str(output),
             "-frames:v","1",str(previews/f"frame{i:02d}.png")])
    print(f"Complete: {output} ({output.stat().st_size:,} bytes)",flush=True)


if __name__=="__main__":main()
