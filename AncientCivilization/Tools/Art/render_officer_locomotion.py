"""기존 제복, 부츠, 팔 원화로 본편의 보행 시트와 좌우 손 소켓을 함께 굽는다(Pillow)."""
import argparse
from math import cos, sin, pi, sqrt, atan2, radians, degrees
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops

PROJECT = Path(__file__).resolve().parents[2]
CELL = 256
TAU = 2 * pi
source = Image.open(PROJECT / "Assets/Resources/Traversal/BluePlugOfficer.png").convert("RGBA")
idle = source.crop((0, 0, CELL, CELL))
coat_mask = Image.new("L", idle.size)
draw = ImageDraw.Draw(coat_mask)
# 원화의 갈라진 코트 자락을 보존하고 그 틈으로 새 다리가 보이게 한다.
draw.polygon([(0,0),(255,0),(255,211),(132,211),(131,139),(122,151),
              (114,201),(112,214),(100,210),(84,209),(82,214),(0,214)], fill=255)
coat = idle.copy()
coat.putalpha(ImageChops.multiply(idle.getchannel("A"), coat_mask))
boot = idle.crop((112, 219, 152, 248))
# 앞쪽 부츠의 발목과 황동 띠까지 포함하는 영역이다.
limbs = []
for i, (w,h) in enumerate([(36,16),(36,16),(36,16),(44,16),(24,20),(24,20)]):
    x, y = 512 + (i%4)*64, 1024 + (i//4)*64
    limbs.append(source.crop((x,y,x+w,y+h)))

def paint_segment(image, outline, start, end, half_width):
    """원화의 두 관절 중심을 기준으로 부품을 수평 텍스처로 펴낸다."""
    mask=Image.new("L",image.size)
    ImageDraw.Draw(mask).polygon(outline,fill=255)
    art=image.copy(); art.putalpha(ImageChops.multiply(image.getchannel("A"),mask))
    dx,dy=end[0]-start[0],end[1]-start[1]; length=sqrt(dx*dx+dy*dy)
    c,s=dx/length,dy/length
    matrix=(c,-s,start[0]-4*c+half_width*s,s,c,start[1]-4*s-half_width*c)
    return art.transform((round(length)+8,2*half_width),Image.Transform.AFFINE,matrix,Image.Resampling.BILINEAR)

uniform=Image.open(PROJECT/"Assets/Resources/Art/Hero.png").convert("RGBA").crop((0,0,384,512)).resize((192,256),Image.Resampling.LANCZOS)
limbs[2]=paint_segment(uniform,[(62,69),(64,59),(69,43),(73,40),(81,40),(90,47),(88,58),(83,74),(74,84),(65,87),(58,82)],(78,47),(64,83),14)
limbs[3]=paint_segment(uniform,[(54,121),(52,116),(57,108),(57,98),(57,86),(57,80),(61,77),(70,77),(74,82),(73,91),(65,108),(65,118),(63,125),(55,127)],(65,84),(58,121),12)
# 금속 하우징의 윤곽은 보존하고 비장착 상태에 그려진 흡입광의 채도만 낮춘다.
pixels=limbs[3].load()
for y in range(limbs[3].height):
    for x in range(limbs[3].width):
        r,g,b,a=pixels[x,y]
        if g>r*1.3 and b>r*1.25:
            l=(2*g+b+r)//4; pixels[x,y]=(int(l*.69),int(l*.77),int(l*.80),a)

run=source.crop((768,0,1024,256))
thigh=paint_segment(run,[(87,126),(99,126),(107,141),(126,163),(130,177),(119,186),(107,180),(97,164),(92,151),(86,147)],(95,133),(119,175),17)
shin=paint_segment(run,[(103,178),(108,170),(120,167),(129,173),(134,194),(137,204),(133,214),(136,224),(127,232),(119,225),(112,211),(106,207),(103,197)],(118,174),(130,227),18)
boot_points=[(x-10,y-8) for y in range(boot.height) for x in range(boot.width) if boot.getpixel((x,y))[3]>125]

def mix(a,b,t):
    return a+(b-a)*t

def smooth(t):
    t=max(0,min(1,t))
    return t*t*(3-2*t)

def warp_coat(hip_x, hip_y, lean, breeze, flutter, crouch):
    # 골반 솔기를 기준점으로 두고 제모·옷깃·옷자락은 서로 다른 호를 따라 기운다.
    def p(x,y):
        upper=max(0, (110-y)/96)
        lower=max(0, (y-110)/106)
        dx=lean*upper-breeze*lower**1.45 + flutter*lower**2
        dy=-.15*breeze*lower**2+upper*crouch
        return (x+hip_x-110+dx,y+hip_y-110+dy)
    # 출력 스캔라인마다 역변환해 메시 가장자리의 정수 반올림 이음새를 없앤다.
    samples=[p(0,y) for y in range(217)]
    strips=[]; src=0
    for a in range(max(0,int(samples[0][1])+1),min(CELL,int(samples[-1][1]))):
        edges=[]
        for dest in (a,a+1):
            while src<215 and samples[src+1][1]<dest:src+=1
            x0,y0=samples[src];x1,y1=samples[src+1]
            ratio=(dest-y0)/max(.001,y1-y0)
            edges.append((mix(x0,x1,ratio),src+ratio))
        (dx0,y0),(dx1,y1)=edges
        strips.append(((0,a,CELL,a+1),(-dx0,y0,-dx1,y1,CELL-dx1,y1,CELL-dx0,y0)))
    return coat.transform((CELL,CELL),Image.Transform.MESH,strips,Image.Resampling.BILINEAR),p

def segment(canvas, art, start, end, thickness=1):
    # 그려진 관절을 축으로 변환하고 마지막 베이크에서만 픽셀 단위로 반올림한다.
    dx,dy=end[0]-start[0],end[1]-start[1]
    sx=sqrt(dx*dx+dy*dy)/(art.width-8); sy=thickness
    angle=atan2(dy,dx);c,s=cos(angle),sin(angle)
    a,b,d,e=c/sx,s/sx,-s/sy,c/sy
    matrix=(a,b,4-a*start[0]-b*start[1],d,e,art.height/2-d*start[0]-e*start[1])
    layer=art.transform(canvas.size,Image.Transform.AFFINE,matrix,Image.Resampling.BILINEAR)
    canvas.alpha_composite(layer)

def stamp(canvas, image, anchor, target, angle=0, scale=1):
    c,s=cos(angle)/scale,sin(angle)/scale
    matrix=(c,s,anchor[0]-c*target[0]-s*target[1],-s,c,anchor[1]+s*target[0]-c*target[1])
    canvas.alpha_composite(image.transform(canvas.size, Image.Transform.AFFINE,matrix,Image.Resampling.BILINEAR))

def joint(start,end,a,b,sign):
    dx,dy=end[0]-start[0],end[1]-start[1];l=max(.1,sqrt(dx*dx+dy*dy))
    projection=min(l,(l*l+a*a-b*b)/(2*l)); height=sqrt(max(0,a*a-projection*projection))
    return start[0]+(dx*projection+sign*dy*height)/l,start[1]+(dy*projection-sign*dx*height)/l

def sculpt(draw,a,b,outline,cloth,highlight,dark,width0,width1):
    dx,dy=b[0]-a[0],b[1]-a[1];length=max(1,sqrt(dx*dx+dy*dy))
    nx,ny=dy/length,-dx/length
    def point(t,width):return (a[0]+t*dx+nx*width,a[1]+t*dy+ny*width)
    draw.polygon([point(0,-width0*.83),point(.18,-width0),point(.70,-width1*1.10),
        point(1,-width1),point(1,width1),point(.86,width1*1.12),point(.2,width0),point(0,width0*.82)],fill=cloth,outline=outline,width=2)
    draw.polygon([point(.12,-width0*.77),point(.24,-width0*.74),point(.77,-width1*.51),point(.91,-width1*.69)],fill=highlight)
    draw.polygon([point(.38,width0*.77),point(.54,width0*.73),point(.86,width1*.72),point(.93,width1*.30)],fill=dark)

def leg(canvas, hip, ankle, angle, far):
    knee=joint(hip,ankle,58,60,1)
    paint=ImageDraw.Draw(canvas)
    c=('#12131A','#303037','#48484D','#25252D') if far else ('#11121A','#403F45','#56565C','#34343C')
    sculpt(paint,hip,knee,*c,11,9)
    sculpt(paint,knee,ankle,*c,9,6)
    if not far:
        segment(canvas,thigh,hip,knee,.80)
        segment(canvas,shin,knee,ankle,.77)
    stamp(canvas,boot,(10,8),ankle,angle,.83 if far else .91)

def arm(canvas, shoulder, elbow, grip, near):
    segment(canvas,limbs[2 if near else 0],shoulder,elbow,.87 if near else .77)
    segment(canvas,limbs[3 if near else 1],elbow,grip,.82 if near else .72)
    fist=limbs[5 if near else 4]
    stamp(canvas,fist,(fist.width/2,fist.height/2),grip,0,.57 if near else .67)

def step_foot(p):
    if p<.50: return 47-94*p/.50,0,radians(-8+17*p/.50)
    t=(p-.50)/.50
    return -47+94*smooth(t),47*sin(pi*t)**1.32,radians(10+47*sin(pi*t)-18*t)

def draw_frame(kind,p,side=0):
    # 위가 y=0이다. 모든 발은 지지선 y=247을 사용한다.
    phase=p*TAU;lift=0;breeze=2;flutter=sin(phase)*1.5;lean=0
    hip_x=126;hip_y=111;chest=0
    ankles=[(110,230,0),(131,230,0)]
    if kind in ('run','brake'):
        t=p if kind=='run' else (0,.5,.25,.75)[side]
        stride=1 if kind=='run' else 1-smooth(p)
        fl,fr=step_foot((t+.5)%1),step_foot(t%1)
        ankles=[(mix(110,124+fl[0],stride),230-fl[1]*stride,fl[2]*stride),
                (mix(131,126+fr[0],stride),230-fr[1]*stride,fr[2]*stride)]
        lean=16*stride;hip_x+=4*stride; hip_y+=stride*(6+4*cos(t*TAU*2))
        breeze=39*stride+2;flutter=sin(t*TAU-.8)*4*stride
        counter=cos(t*TAU)*stride
        chest=2*stride
    elif kind=='air':
        # 상승 때 무릎을 치고, 정점에서 모았다가, 접지에 앞서 밑창을 펼친다.
        q=p;lean=mix(12,6,q);breeze=22+15*sin(q*pi)
        hip_y=110+10*sin(q*pi);chest=3*sin(q*pi)
        ankles=[(mix(90,101,q),mix(176,221,q)-12*sin(q*pi),radians(26*(1-q))),
                (mix(155,149,q),mix(190,224,q)-16*sin(q*pi),radians(-12+24*q))]
        counter=.44-.82*q;flutter=-3+5*q
    elif kind=='land':
        bend=sin(pi*(.10+.90*p))*(1-p)
        hip_y=111+26*bend;lean=13*bend;breeze=2+15*bend;chest=2*bend
        ankles=[(109-2*bend,230,0),(132+4*bend,230,0)]
        counter=-.4*bend;flutter=3*bend
    else:
        hip_y=111+1.4*sin(phase-.3);lean=1.4*sin(phase);chest=1.4*sin(phase+.45)
        counter=.035*sin(phase-.4)
    corrected=[]
    for index,(x,y,angle) in enumerate(ankles):
        sole=max(bx*sin(angle)+by*cos(angle) for bx,by in boot_points)*(.83 if index==0 else .91)
        corrected.append((x,247-sole + y-230,angle))
    ankles=corrected
    body,pos=warp_coat(hip_x,hip_y,lean,breeze,flutter,chest)
    canvas=Image.new('RGBA',(CELL,CELL))
    farr,nearr=ankles
    leg(canvas,(hip_x+4,hip_y+8),farr[:2],farr[2],True)
    leg(canvas,(hip_x-3,hip_y+8),nearr[:2],nearr[2],False)
    far=pos(122,64);near=pos(96,65)
    # 팔은 앞다리와 반대로 움직인다. 팔꿈치가 먼저 나가고 손이 따라온다.
    running = kind in ('run','brake')
    rate = (1 if kind=='run' else 1-smooth(p)) if running else (1 if kind=='air' else 0)
    f_grip=(far[0]+7+counter*27+rate*9,far[1]+53-rate*21)
    n_grip=(near[0]-2-counter*28+rate*9,near[1]+58-rate*20)
    def elbow(s,g):return joint(s,g,28,32,-1)
    arm(canvas,far,elbow(far,f_grip),f_grip,False)
    # 원화의 매듭을 고정하고 스카프 앞뒤 두 면은 몸보다 늦게 따라오게 한다.
    sx,sy=pos(100,48);tail=20+breeze*.68
    d=ImageDraw.Draw(canvas);dy=2+flutter
    d.polygon([(sx,sy),(sx-8,sy+7),(sx-tail*.58,sy+9+dy),(sx-tail,sy+19+dy),
               (sx-tail+4,sy+9+dy),(sx-tail*.57,sy+3+dy),(sx-9,sy+2)],fill='#873D34')
    d.line([(sx-8,sy+3),(sx-tail*.55,sy+4+dy),(sx-tail+4,sy+11+dy)],fill='#A95446',width=2)
    canvas.alpha_composite(body)
    arm(canvas,near,elbow(near,n_grip),n_grip,True)
    # 장착 중인 블레이드와 케이블에 같은 소켓 좌표를 전달한다.
    return canvas,tuple(round(v) for v in (*f_grip,*n_grip))

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('--output',type=Path,default=PROJECT/'Assets/Resources/Traversal/OfficerFieldGait.png')
    args=ap.parse_args()
    specs=([('idle',i/8,0) for i in range(8)]+[('run',i/16,0) for i in range(16)]+
           [('air',i/7,0) for i in range(8)]+[('land',i/7,0) for i in range(8)]+
           [('brake',i/3,j) for j in range(4) for i in range(4)])
    atlas=Image.new('RGBA',(8*CELL,7*CELL))
    grips=[]
    for i,spec in enumerate(specs):
        image,coordinates=draw_frame(*spec)
        atlas.alpha_composite(image,((i%8)*CELL,(i//8)*CELL));grips.extend(coordinates)
    atlas.save(args.output,optimize=True)
    args.output.with_name('OfficerFieldSockets.bytes').write_bytes(bytes(grips))
if __name__ == '__main__': main()
