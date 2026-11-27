"""시계침의 금속 압착 자국과 시계 축 해방을 위한 투명 이펙트 원화. Pillow 필요."""
from math import cos, radians, sin
from pathlib import Path
from PIL import Image, ImageDraw

WIDTH, HEIGHT, CELL, S = 128, 80, 8, 4
BRASS = (227, 164, 83)
GOLD = (252, 208, 135)
MILK = (255, 245, 207)
STEEL = (181, 199, 201)
UMBRA = (75, 49, 45)


def image():
    canvas = Image.new('RGBA', (WIDTH*S, HEIGHT*S))
    return canvas, ImageDraw.Draw(canvas)


def points(points):
    return [(round(x*S), round(y*S)) for x, y in points]


def poly(draw, coords, color, alpha):
    draw.polygon(points(coords), fill=(*color, round(255*alpha)))


def arc(draw, bbox, start, end, color, alpha, width):
    draw.arc(tuple(round(x*S) for x in bbox), start, end,
             fill=(*color, round(255*alpha)), width=max(1,round(width*S)))


def splinter(draw, x, y, dx, dy, length, width, color, alpha):
    shaft = [(x+dx*length, y+dy*length), (x-dy*width, y+dx*width),
             (x-dx*length*.48, y-dy*length*.48), (x+dy*width, y-dx*width)]
    poly(draw, shaft, UMBRA, alpha)
    poly(draw, [shaft[0], (x,y), shaft[3]], color, alpha)
    poly(draw, [shaft[0], shaft[1], (x,y)], GOLD, alpha*.86)


def stamp(index):
    canvas,draw=image()
    t=index/(CELL-1)
    x,y=64,68
    fade=(1-t*.90)**.90
    width=8+52*(1-(1-t)**2)
    depth=2+5*(1-(1-t)**2)
    eye=(x-width,y-depth,x+width,y+depth)
    # 타격면의 먼지 띠에 틈을 남겨 바닥과 새 위험 표식이 보인다.
    for start,end in [(184,234),(248,270),(290,342),(1,26),(40,72),(99,153),(163,179)]:
        arc(draw,eye,start,end,UMBRA,fade*.92,3)
        arc(draw,eye,start,end,BRASS,fade,1.65)
    if t<.60:
        poly(draw,[(x-width*.86,y),(x-8,y-2),(x-3,y-4),(x,y-15*(1-t/.62)),
                   (x+3,y-4),(x+9,y-2),(x+width*.86,y),(x+7,y+1.8),
                   (x,y+3),(x-8,y+1)],MILK,1-t/.68)
    # 강한 두 가닥과 짧은 세 가닥만 사용해 이웃 피격 구역까지 번지지 않게 한다.
    for dx,dy,reach,h,metal in [(-.79,-.61,46,34,STEEL),(.66,-.75,39,41,MILK),
                                  (-.40,-.92,24,31,GOLD),(.95,-.31,54,17,GOLD),
                                  (-.97,-.24,44,13,BRASS)]:
        spread=.12+.88*t**.76
        lift=h*spread*(1-.48*t)
        sx,sy=x+dx*reach*spread,y-2-lift
        splinter(draw,sx,sy,dx,dy,(7+abs(dy)*2)*(1-.76*t),1.95*(1-.63*t),metal,fade)
    return canvas.resize((WIDTH,HEIGHT),Image.Resampling.LANCZOS)


def crown(index):
    canvas,draw=image()
    t=index/(CELL-1)
    x,y=64,40
    r=9+22*(1-(1-t)**2)
    alpha=1-.93*t
    # 잠금쇠 세 쌍이 벌어진다. 대칭을 유지해 실제 합벡터 방향을 누설하지 않는다.
    for sector in range(6):
        start=sector*60+9-10*t
        arc(draw,(x-r,y-r,x+r,y+r),start,start+34,UMBRA,alpha,3.5-1.15*t)
        arc(draw,(x-r,y-r,x+r,y+r),start,start+34,BRASS,alpha,2.05-.95*t)
        angle=radians(start+17)
        dx,dy=cos(angle),sin(angle)
        splinter(draw,x+dx*(r+2),y+dy*(r+2),dx,dy,3.1-.9*t,1.8-t,STEEL,alpha)
    if t<.63:
        a=1-t/.65
        for sector in range(3):
            s=sector*120+19+t*46
            rad=r*.65
            arc(draw,(x-rad,y-rad,x+rad,y+rad),s,s+67,GOLD,a,1.7)
        ray=7*(1-t/.64)
        poly(draw,[(x-ray,y),(x-2,y-2),(x,y-ray),(x+2,y-2),(x+ray,y),
                   (x+2,y+2),(x,y+ray),(x-2,y+2)],MILK,a)
    return canvas.resize((WIDTH,HEIGHT),Image.Resampling.LANCZOS)


if __name__ == '__main__':
    sheet=Image.new('RGBA',(WIDTH*CELL,HEIGHT*2))
    for row,paint in enumerate((stamp,crown)):
        for col in range(CELL):
            sheet.paste(paint(col),(WIDTH*col,HEIGHT*row))
    path=Path(__file__).resolve().parents[2]/'Assets/Resources/Art/ClockStrikeFX.png'
    sheet.save(path,optimize=True)
