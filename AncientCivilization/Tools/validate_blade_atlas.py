"""Read-only checks of the three shipped melee atlases and measured runtime metadata."""
import math
import re
from PIL import Image
from pack_field_blade import layout


def validate_blade_atlas(root, check, png_info):
    source = (root / 'Assets/Scripts/AncientBladeAtlas.cs').read_text()
    def block(name):
        return source.split(name+'={', 1)[1].split('};', 1)[0]
    rects = [tuple(map(int, item)) for item in re.findall(r'new Rect\((\d+),(\d+),(\d+),(\d+)\)', block('Rectangles'))]
    def points(name):
        return [tuple(float(value.rstrip('f')) for value in item) for item in re.findall(r'new Vector2\((-?[0-9.]+f?),(-?[0-9.]+f?)\)', block(name))]
    pivots, tips = points('Pivots'), points('Tips')
    hilts, reels = points('Hilts'), points('Reels')
    check(len(hilts) == len(reels) == 48, 'each melee frame has a measured wrist and cable reel')
    sheets = re.findall(r'"([A-Za-z]+)"', block('Sheets'))
    ppu = [float(v.rstrip('f')) for v in block('Ppu').split(',')]
    check(len(rects)==len(pivots)==len(tips)==48, 'three blade cuts each have sixteen measured crops, foot pivots and tip points')
    check(len(sheets)==len(ppu)==3 and all(p>0 for p in ppu), 'three blade sheets have explicit positive world scales')
    grid = int(re.search(r'GridSize=(\d+)', source)[1])
    field_path = root / 'Assets/Resources/Art/FieldBladeDetail.png'
    field = Image.open(field_path).convert('RGBA')
    width = int(re.search(r'FieldSheetWidth\s*=\s*(\d+)', source)[1])
    regions = list(layout(rects, width))
    check(field.size == (width, max(y+h for _, y, _, h in regions)+2) and max(field.size) <= 2048,
          'full-density field melee sheet fits its unscaled Unity import limit')
    field_meta = field_path.with_suffix('.png.meta').read_text()
    check(all(setting in field_meta for setting in ('isReadable: 0', 'filterMode: 0', 'textureCompression: 0', 'enableMipMap: 0')),
          'field melee sheet uses point pixels, no compression, mips or retained CPU copy')
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
            fx, fy, fw, fh = regions[index]
            packed = field.crop((fx,fy,fx+fw,fy+fh))
            check(packed.tobytes() == image.crop((x,y,x+w,y+h)).convert('RGBA').tobytes(),
                  f'{name} frame {frame} field art retains every RGBA pixel from the authored PNG')
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
            for socket, label in ((hilts[index], 'hilt'), (reels[index], 'reel')):
                gx = round(x + px*w + socket[0]*ppu[step])
                gy = round(y + (1-py)*h - socket[1]*ppu[step])
                visible = [native(xx,yy) for yy in range(max(y,gy-2),min(y+h,gy+3))
                           for xx in range(max(x,gx-2),min(x+w,gx+3)) if native(xx,yy)[3] > 128]
                check(bool(visible), f'{name} frame {frame} {label} lands on opaque actor pixels')
                if label == 'hilt':
                    check(any(g>85 and g>r*1.15 and b>r*1.15 for r,g,b,a in visible),
                          f'{name} frame {frame} hilt meets the drawn mechanical wrist')
            signatures.append(frozenset(main))
    check(len(set(signatures))==48, 'all forty-eight blade frames have distinct measured silhouettes')
