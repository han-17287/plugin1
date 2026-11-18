"""Read-only checks of the three shipped melee atlases and measured runtime metadata."""
import math
import re
from PIL import Image


def validate_blade_atlas(root, check, png_info):
    source = (root / 'Assets/Scripts/AncientBladeAtlas.cs').read_text()
    def block(name):
        return source.split(name+'={', 1)[1].split('};', 1)[0]
    rects = [tuple(map(int, item)) for item in re.findall(r'new Rect\((\d+),(\d+),(\d+),(\d+)\)', block('Rectangles'))]
    def points(name):
        return [tuple(float(value.rstrip('f')) for value in item) for item in re.findall(r'new Vector2\((-?[0-9.]+f?),(-?[0-9.]+f?)\)', block(name))]
    pivots, tips = points('Pivots'), points('Tips')
    sheets = re.findall(r'"([A-Za-z]+)"', block('Sheets'))
    ppu = [float(v.rstrip('f')) for v in block('Ppu').split(',')]
    check(len(rects)==len(pivots)==len(tips)==48, 'three blade cuts each have sixteen measured crops, foot pivots and tip points')
    check(len(sheets)==len(ppu)==3 and all(p>0 for p in ppu), 'three blade sheets have explicit positive world scales')
    grid = int(re.search(r'GridSize=(\d+)', source)[1])
    signatures=[]
    for step, name in enumerate(sheets):
        path = root / ('Assets/Resources/Art/'+name+'.png')
        info=png_info(path)
        check(info[3]==6, name+' retains its transparent RGBA source')
        meta=path.with_suffix('.png.meta').read_text()
        check(all(setting in meta for setting in ('isReadable: 1','filterMode: 0','textureCompression: 0')), name+' uses readable point-filtered uncompressed import')
        image=Image.open(path);pixels=image.load();width,height=image.size
        def native(x,y):
            sx=min(x//grid*grid+grid//2,width-1)
            bottom=height-1-y
            sy=height-1-min(bottom//grid*grid+grid//2,height-1)
            return pixels[sx,sy]
        for frame in range(16):
            index=step*16+frame;x,y,w,h=rects[index];px,py=pivots[index];tx,ty=tips[index]
            cx0=round(frame%4*width/4);cx1=round((frame%4+1)*width/4)
            cy0=round(frame//4*height/4);cy1=round((frame//4+1)*height/4)
            check(0<=x<x+w<=width and 0<=y<y+h<=height and cx0<x+w*.5<cx1 and cy0<y+h*.5<cy1,
                  name+' measured crop stays in the texture and follows frame order '+str(frame))
            others=rects[step*16:step*16+16]
            check(all(other==frame or min(x+w,ox+ow)<=max(x,ox) or min(y+h,oy+oh)<=max(y,oy)
                      for other,(ox,oy,ow,oh) in enumerate(others)), name+' crop has no overlap with another pose '+str(frame))
            check(0<px<1 and 0<=py<1 and all(math.isfinite(v) for v in (tx,ty)), name+' foot pivot and blade tip are finite '+str(frame))
            shape=[];gold=white=black=cyan=0
            for yy in range(y,y+h):
                for xx in range(x,x+w):
                    r,g,b,a=native(xx,yy)
                    if a<110:continue
                    shape.append((xx-x,yy-y))
                    gold += r>180 and g>135 and b<110
                    white += min(r,g,b)>165
                    black += max(r,g,b)<95
                    cyan += b>130 and g>100 and b>r+25
            check(len(shape)>3000, name+' has an opaque actor '+str(frame))
            check(gold>100 and white>100 and black>100 and cyan>15, name+' retains yellow blade, white/black uniform and blue arm '+str(frame))
            pending=set(shape);main=set()
            while pending:
                component={pending.pop()};queue=list(component)
                for xx,yy in queue:
                    for dx,dy in ((-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)):
                        point=(xx+dx,yy+dy)
                        if point in pending:pending.remove(point);component.add(point);queue.append(point)
                if len(component)>len(main):main=component
            check(len(main)>len(shape)*.95, name+' blade and body form one complete actor '+str(frame))
            check(all(0<xx<w-1 and 0<yy<h-1 for xx,yy in main), name+' keeps clear blade and boot gutters '+str(frame))
            tipx=x+px*w+tx*ppu[step];tipy=y+(1-py)*h-ty*ppu[step]
            check(x<=tipx<x+w and y<=tipy<y+h, name+' measured blade tip stays inside its pose '+str(frame))
            check(any(native(xx,yy)[3]>=110 and native(xx,yy)[0]>170 and native(xx,yy)[1]>120 and native(xx,yy)[0]>native(xx,yy)[2]*1.2
                      for yy in range(max(y,round(tipy)-4),min(y+h,round(tipy)+5))
                      for xx in range(max(x,round(tipx)-4),min(x+w,round(tipx)+5))), name+' tip metadata touches the yellow blade '+str(frame))
            signatures.append(frozenset(main))
    check(len(set(signatures))==48, 'all forty-eight blade frames have distinct measured silhouettes')
