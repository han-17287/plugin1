"""비서실장의 기존 회중시계 원화에서 전투 포즈를 굽는다. Pillow 필요."""
from math import cos, radians, sin, pi
from pathlib import Path
import sys

from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/Resources/Art"
CELL = (160, 224)
FRAMES = 8
SHOULDER = (285, 109)
ELBOW = (326, 126)
CUFF = (341, 139)
AFFINE = Image.Transform.AFFINE
BICUBIC = Image.Resampling.BICUBIC
PADDING = 41


def masked(image, polygon):
    mask = Image.new("L", image.size)
    ImageDraw.Draw(mask).polygon(polygon, fill=255)
    out = image.copy()
    out.putalpha(ImageChops.multiply(out.getchannel("A"), mask))
    return out


def source_parts():
    raw = Image.open(ART / "Boss5.png").convert("RGBA").crop((0, 512, 512, 1024))
    # 원화에서 체인을 분리한다. 손가락과 금시계는 같은 레이어라 벌어지지 않는다.
    limb = [(279, 84), (298, 84), (310, 97), (361, 91), (376, 62), (422, 62),
            (423, 123), (374, 133), (365, 146), (342, 146), (334, 137),
            (331, 141), (326, 144), (307, 147), (279, 135)]
    arm = masked(raw, limb)
    upper = masked(arm, [(265, 72), (328, 72), (335, 148), (265, 148)])
    lower = masked(arm, [(317, 62), (425, 62), (425, 149), (324, 149)])
    body = raw.copy()
    draw = ImageDraw.Draw(body)
    draw.polygon([(288, 98), (314, 105), (354, 70), (423, 61), (423, 181),
                  (289, 181), (289, 143), (288, 136)], fill=(0, 0, 0, 0))
    # 체인의 몸통 쪽 부착점과 장식은 남긴다.
    draw.polygon([(290, 101), (294, 101), (304, 103), (299, 111), (290, 109)],
                 fill=(0, 0, 0, 0))
    # 반대쪽 장갑도 어깨 밑에서 별도 회전시킨다. 장갑에 가려졌던 코트의
    # 왼쪽 솔기는 바로 옆 검은 천으로 복원해 팔을 펼 때 구멍이 생기지 않는다.
    back_outline = [(183,116),(223,111),(226,120),(225,149),(222,159),
                    (220,167),(219,181),(216,200),(217,209),(215,230),
                    (213,237),(220,251),(221,273),(208,276),(187,268),
                    (180,236),(179,193),(179,154)]
    sleeve = masked(raw, back_outline)
    marker=sleeve.getchannel("A")
    for y in range(112,278):
        edge=231 - max(0,y-166)*.38
        for x in range(177,228):
            if marker.getpixel((x,y)) == 0:
                continue
            behind_coat = y>=169 and x>=edge
            body.putpixel((x,y),raw.getpixel((round(edge+20+(x-edge)*.32),y))
                          if behind_coat else (0,0,0,0))
    return body, upper, lower, sleeve


def rotated_point(point, pivot, angle, destination):
    a = radians(angle)
    x, y = point[0] - pivot[0], point[1] - pivot[1]
    return destination[0] + cos(a)*x - sin(a)*y, destination[1] + sin(a)*x + cos(a)*y


def turn(image, pivot, angle, destination):
    a = radians(angle)
    c, s = cos(a), sin(a)
    x, y = destination
    return image.transform((512, 558), AFFINE, (
        c, s, pivot[0]-c*x-s*y, -s, c, pivot[1]+s*x-c*y), BICUBIC)


def chain(canvas, attach, hand, slack):
    # 중력에 처진 체인이 손목의 높이를 따라가도록 2차 곡선으로 그린다.
    draw = ImageDraw.Draw(canvas)
    ctrl = ((attach[0]+hand[0])*.5, (attach[1]+hand[1])*.5 + slack)
    points = []
    for i in range(31):
        t = i/30
        points.append(tuple((1-t)**2*attach[k]+2*t*(1-t)*ctrl[k]+t*t*hand[k] for k in (0,1)))
    draw.line(points, fill=(46, 29, 27, 250), width=4)
    draw.line(points, fill=(195, 148, 76, 255), width=2)
    for p in points[1::3]:
        draw.point(p,fill=(255, 227, 171, 255))


# 위팔 각도, 아래팔 절대각, 상체의 좌우 치우침, 무릎 눌림, 체인 처짐.
NEUTRAL = (49, -28, -2, 0, 17)
READ = (36, -59, 9, 5, 14)
EXTEND = (-22, 9, 17, 11, 26)
FOLD = (58, -86, -12, 18, 11)
BRACE = (-46, -47, -16, 15, 22)
SWEEP = (18, 32, 21, 19, 32)
RAISED = (-89, -78, -12, 1, 14)
CUT = (-49, -8, 8, 12, 26)

# 각 구간의 마지막 자세를 다음 구간으로 물려준다.
KEYS = (
    [(0,NEUTRAL), (.5,(45,-32,-4,3,19)), (1,NEUTRAL)],
    [(0,NEUTRAL), (.35,(26,-35,3,2,22)), (.71,READ), (1,(32,-62,6,2,13))],
    [(0,EXTEND), (.26,(-17,12,21,14,30)), (1,NEUTRAL)],
    [(0,NEUTRAL), (.43,(45,-55,-6,6,14)), (1,FOLD)],
    [(0,FOLD), (.57,(26,-61,-12,14,18)), (1,BRACE)],
    [(0,BRACE), (.28,SWEEP), (.57,(26,29,15,15,30)), (1,NEUTRAL)],
    [(0,NEUTRAL), (.25,(33,-67,-6,10,18)), (.75,(-68,-85,-15,3,13)), (1,RAISED)],
    [(0,RAISED), (.27,CUT), (.52,(-48,-13,6,10,32)), (1,RAISED)],
)


def interpolate(keys,t):
    for (lo,a),(hi,b) in zip(keys, keys[1:]):
        if t<=hi:
            u=(t-lo)/(hi-lo)
            u=u*u*(3-2*u)
            return tuple(x+(y-x)*u for x,y in zip(a,b))
    return keys[-1][1]


def cell(parts, row, col):
    body, upper, lower, sleeve = parts
    a, b, lean, sink, slack = interpolate(KEYS[row], col/(FRAMES-1))
    dressed = Image.new("RGBA",(512,558))
    dressed.paste(body,(0,PADDING))
    free_arm_keys=((0,2),(4,9),(29,0),(0,-11),(-11,32),(32,0),
                   (0,39),(39,39))
    lo,hi=free_arm_keys[row]
    t=col/(FRAMES-1)
    free_arm=lo+(hi-lo)*t*t*(3-2*t)
    if row==5: free_arm+=28*sin(pi*t)
    if row==7: free_arm-=14*sin(pi*t)
    dressed.alpha_composite(turn(sleeve,(207,117),free_arm,(207,117+PADDING)))
    sh=(SHOULDER[0],SHOULDER[1]+PADDING)
    elbow=rotated_point(ELBOW,SHOULDER,a,sh)
    cuff=rotated_point(CUFF,ELBOW,b,elbow)
    chain(dressed,(285,176+PADDING),cuff,slack)
    dressed.alpha_composite(turn(upper,SHOULDER,a,sh))
    dressed.alpha_composite(turn(lower,ELBOW,b,elbow))
    # 발바닥은 고정한다. 목과 가슴이 골반보다 먼저 움직인다.
    warped=Image.new("RGBA",dressed.size)
    for y in range(dressed.height):
        source_y=y-PADDING
        weight=max(0,min(1,(483-source_y)/360))
        sx=round(lean*weight)
        from_y=round(y-sink*weight)
        if 0<=from_y<dressed.height:
            warped.paste(dressed.crop((0,from_y,512,from_y+1)),(sx,y))
    return warped.crop((56,0,456,560)).resize(CELL, Image.Resampling.LANCZOS)


def main():
    parts=source_parts()
    atlas=Image.new("RGBA",(CELL[0]*FRAMES,CELL[1]*len(KEYS)))
    for row in range(len(KEYS)):
        for col in range(FRAMES):
            atlas.paste(cell(parts,row,col),(col*CELL[0],row*CELL[1]))
    # 단일 팔레트로 앞뒤 프레임의 금색과 흰 장갑을 일정하게 유지한다.
    atlas.quantize(colors=256,method=Image.Quantize.FASTOCTREE,
                   dither=Image.Dither.NONE).save(
        Path(sys.argv[1]) if len(sys.argv)>1 else ART/"SecretaryGestures.png", optimize=True)


if __name__ == "__main__":
    main()
