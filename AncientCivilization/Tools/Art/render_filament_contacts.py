"""필라멘트 칼의 접점 원화. 위에서부터 벽, 적, 콤보 마무리. Pillow 필요."""
from math import cos, radians, sin
from pathlib import Path

from PIL import Image, ImageDraw

W, H, COUNT, AA = 144, 112, 8, 3
X, Y = 43, 57
INK = (80, 43, 38)
GOLD = (255, 161, 41)
LIGHT = (255, 244, 188)
HOT = (255, 255, 242)


def frame(kind, index):
    canvas = Image.new('RGBA', (W*AA, H*AA))
    pen = ImageDraw.Draw(canvas)

    def poly(points, color, alpha=1.):
        if alpha > 0:
            pen.polygon([(round((X+x)*AA), round((Y-y)*AA)) for x, y in points],
                        fill=(*color, round(255*min(1.,alpha))))

    def sliver(x, y, angle, length, width, alpha, glass):
        c, s = cos(angle), sin(angle)
        def point(u, v):
            return x+c*u-s*v, y+s*u+c*v
        shell = [point(-length*.72, 0), point(0, width),
                 point(length*.28, width*.25), point(length*.34, -width*.22), point(0, -width*.68)]
        poly(shell, INK, alpha)
        poly([point(-length*.65, 0), point(0, width*.62),
              point(length*.29, width*.18), point(0, -width*.32)], GOLD, alpha)
        poly([point(-length*.26, 0), point(0, width*.58),
              point(length*.29, width*.18)], LIGHT if glass else HOT, alpha)

    t = index / (COUNT-1)
    # 첫 프레임의 국소 절단선은 즉시 보인다. 금속은 표면 바깥쪽에서만 튄다.
    if kind == 0:
        angles = (-50, -24, 8, 33, 61)
        radii = (30, 57, 41, 52, 25)
    elif kind == 1:
        angles = (-65, -32, -9, 22, 57, 135, -151)
        radii = (40, 70, 40, 76, 44, 27, 21)
    else:
        angles = (-77, -44, -20, -3, 26, 44, 66, 132, -144)
        radii = (42, 66, 85, 47, 89, 56, 42, 32, 28)
    for i,(degrees,radius) in enumerate(zip(angles,radii)):
        age = max(0.,t-.035*(i%3))
        a = radians(degrees)
        travel = (.06 + .93*(1-(1-age)**2)) * radius
        x, y = travel*cos(a), travel*sin(a)-5*age*age
        length = (13+3*(i%4))*(1.1-age*.68)*(1.18 if kind==2 else 1)
        width = (1.7+i%3*.66)*(1-.50*age)*(1.18 if kind==2 else 1)
        sliver(x,y,a+.22*age*(i%3-1),length,width,1-age**1.55,kind>0)

    # 작은 파편의 반사면이 모였다 풀리는 역광. 세 타째만 두 갈래 균열이 벌어진다.
    if kind and index<5:
        u=1-t*1.55
        L=(35 if kind==2 else 24)*u
        poly([(-L*.81,-L*.40),(-3,-3),(2,-L*.64),(3,-3),
              (L,.59*L),(5,3),(-2,L*.57),(-3,3)],INK,u)
        poly([(-L*.70,-L*.33),(-1,-1),(2,-L*.37),(3,-1),
              (L,.56*L),(2,2),(-2,L*.39),(-3,1)],HOT,u)
    if kind==2 and index<5:
        for sign in (-1,1):
            x=8+t*39
            poly([(x-9,sign*(11+12*t)),(x+8,sign*(18+12*t)),
                  (x+13,sign*(18+12*t)),(x+4,sign*(16+12*t))],LIGHT,(1-t*1.5)*.87)
    return canvas.resize((W,H),Image.Resampling.LANCZOS)


if __name__=='__main__':
    atlas = Image.new('RGBA',(W*COUNT,H*3))
    for kind in range(3):
        for i in range(COUNT):
            atlas.paste(frame(kind,i),(W*i,H*kind))
    atlas.save(Path(__file__).resolve().parents[2]/'Assets/Resources/Art/FilamentContacts.png', optimize=True)
