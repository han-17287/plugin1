"""조커의 실전 포즈 원화 생성기. 프로젝트 루트에서 python Tools/Art/render_joker_motion.py."""
from dataclasses import dataclass, replace
from functools import lru_cache
from math import cos, hypot, radians, sin
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageOps

ART = Path(__file__).resolve().parents[2] / "Assets/Resources/Art"
CELL = 96
DENSITY = 3
SAMPLE = 6
# 96×96 논리 좌표에서 발 피벗은 (34.5,93.75), 아래 좌표가 일반 사격·수류탄의 공통 원점이다.
MUZZLE = (73.55, 52.5)


def add(a, b):
    return a[0] + b[0], a[1] + b[1]


def between(a, b, fraction):
    return a[0] + (b[0] - a[0]) * fraction, a[1] + (b[1] - a[1]) * fraction


def unit(a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]
    length = max(.01, hypot(dx, dy))
    return dx / length, dy / length


def basis(a, b):
    ux, uy = unit(a, b)
    length = hypot(b[0] - a[0], b[1] - a[1])
    return lambda f, n=0: (a[0] + ux * length * f - uy * n, a[1] + uy * length * f + ux * n)


def bend(shoulder, hand, upper, forearm):
    # 두 골격 길이를 보존한 팔꿈치. 총구가 낮을 때에도 팔이 길게 늘어나지 않는다.
    dx,dy=hand[0]-shoulder[0],hand[1]-shoulder[1]
    distance=max(.01,hypot(dx,dy))
    along=min(distance*.85,max(distance*.13,(upper*upper-forearm*forearm+distance*distance)/(2*distance)))
    across=max(0,upper*upper-along*along)**.5
    return (shoulder[0]+(dx*along-dy*across)/distance,
            shoulder[1]+(dy*along+dx*across)/distance)


class Frame:
    def __init__(self):
        self.canvas = Image.new("RGBA", (CELL * SAMPLE, CELL * SAMPLE))
        self.pen = ImageDraw.Draw(self.canvas)

    @staticmethod
    def coords(points):
        return [(round(x * SAMPLE), round(y * SAMPLE)) for x, y in points]

    def fill(self, points, color):
        self.pen.polygon(self.coords(points), fill=color)

    def stroke(self, points, color, width=.55):
        self.pen.line(self.coords(points), fill=color, width=max(1, round(width * SAMPLE)), joint="curve")

    def oval(self, x, y, rx, ry, color):
        self.pen.ellipse(self.coords([(x-rx,y-ry), (x+rx,y+ry)]), fill=color)

    def panel(self, points, dark, lit):
        # 가장자리를 밝게 그리되 검은 재질 자체에는 넓은 면 명암을 남긴다.
        coords = self.coords(points)
        x0, y0 = min(x for x, _ in coords), min(y for _, y in coords)
        x1, y1 = max(x for x, _ in coords)+1, max(y for _, y in coords)+1
        mask = Image.new("L",(x1-x0,y1-y0))
        ImageDraw.Draw(mask).polygon([(x-x0,y-y0) for x,y in coords],fill=255)
        shade = Image.linear_gradient("L").transpose(Image.Transpose.FLIP_TOP_BOTTOM).resize(mask.size)
        self.canvas.paste(ImageOps.colorize(shade, dark, lit), (x0,y0),mask)

    def mount(self, tex, src_start, src_end, start, end, width):
        # 원화 조각은 길이 방향만 포즈에 맞춰 늘리고 너비와 표면의 붓질은 그대로 둔다.
        ux, uy = unit(src_start, src_end)
        tx, ty = unit(start, end)
        scale = hypot(end[0]-start[0], end[1]-start[1]) / hypot(src_end[0]-src_start[0],src_end[1]-src_start[1])
        along, across = 1/(max(scale,.01)*SAMPLE), 1/(width*SAMPLE)
        a,b = ux*tx*along + uy*ty*across, ux*ty*along - uy*tx*across
        c,d = uy*tx*along - ux*ty*across, uy*ty*along + ux*tx*across
        ox = src_start[0] - (a*start[0]+b*start[1])*SAMPLE
        oy = src_start[1] - (c*start[0]+d*start[1])*SAMPLE
        warp = tex.transform(self.canvas.size, Image.Transform.AFFINE,(a,b,ox,c,d,oy),Image.Resampling.BICUBIC)
        self.canvas.alpha_composite(warp)

    def image(self):
        return self.canvas.resize((CELL*DENSITY,CELL*DENSITY),Image.Resampling.LANCZOS)


@dataclass(frozen=True)
class Pose:
    hip: tuple = (33.5,57.5)
    shoulder: tuple = (35.7,36.1)
    feet: tuple = ((27,93),(45,93))
    knees: tuple = ((28.9,75.7),(42.8,75.9))
    tip: tuple = (73.7,61)
    pitch: float = 23
    tilt: float = 0
    shouldered: bool = False
    offhand: tuple | None = None
    hand: tuple | None = None
    elbow: tuple | None = None
    far_elbow: tuple | None = None
    grenade: bool = False
    magazine: bool = False
    knife: tuple | None = None
    sling_tip: tuple = (18,20)
    neck: tuple | None = None
    cape_tip: tuple | None = None


@lru_cache(maxsize=1)
def field_parts():
    """재킷·슬링·전면 관측기와 허리 탄창은 몸통 좌표에, 견갑과 장갑은 팔 관절에 등록한다."""
    raw = Image.open(Path(__file__).with_name("JokerFieldPlate.png")).convert("RGBA")
    red, green, blue, alpha = raw.split()
    # 배경과 실루엣이 붙는 어두운 장면에서도 외투와 탄창 표면의 명암이 구분되게 한다.
    curve = [round(255*(v/255)**.87) for v in range(256)]
    raw = Image.merge("RGBA", (red.point(curve), green.point(curve), blue.point(curve), alpha))
    # 기본 카메라에서도 가면의 패와 가슴 관측기를 분리해 읽도록, 원화 표면 좌표에 도색한다.
    pen = ImageDraw.Draw(raw)
    pen.polygon([(529,117),(540,132),(528,154),(516,136)], fill="#682635")
    pen.polygon([(528,121),(536,133),(527,149),(520,136)], fill="#b14d56")
    pen.line([(529,119),(538,132)], fill="#ddbaab", width=2)
    pen.ellipse((588,279,623,312), fill="#242731", outline="#b9a67f", width=3)
    pen.ellipse((593,283,618,307), fill="#72654c", outline="#423a35", width=3)
    pen.ellipse((598,285,614,300), fill="#ddbd80")
    pen.ellipse((599,286,606,292), fill="#ffeaab")
    # 망토의 겹쳐진 앞잎 하단 안감. 기존 붓 터치의 밝기를 보존해 가죽 장식과 재질을 구분한다.
    fabric = Image.new("L", raw.size)
    ImageDraw.Draw(fabric).polygon([(1114,1071),(1350,1032),(1421,948),(1394,1095),
        (1340,1200),(1280,1344),(1234,1431),(1165,1425),(1088,1243)], fill=255)
    fabric = ImageChops.multiply(fabric,alpha)
    lining = ImageOps.colorize(ImageOps.grayscale(raw), black="#2a1c29", white="#ca9a92")
    raw.paste(lining,(0,0),fabric)
    raw.putalpha(alpha)
    parts = {}

    def cut(name, shape, start, end):
        mask = Image.new("L", raw.size)
        ImageDraw.Draw(mask).polygon(shape, fill=255)
        masked = ImageChops.multiply(alpha, mask)
        box = masked.getbbox()
        part = raw.crop(box)
        part.putalpha(masked.crop(box))
        def local(p):
            return p[0] - box[0], p[1] - box[1]
        parts[name] = part, local(start), local(end)

    cut("cape", [(1050,280),(1838,280),(1838,1462),(1050,1462)],
        (1750,311),(1227,1444))
    cut("helmet", [(383,0),(631,0),(631,113),(585,113),(566,126),(556,154),
                   (544,167),(527,177),(506,171),(496,158),(471,154),(439,147),
                   (425,142),(414,125),(394,112),(386,86)], (459,216),(477,66))
    cut("jacket", [(355,211),(372,202),(378,188),(388,178),(381,142),(395,136),
                   (456,148),(493,160),(519,175),(545,171),(584,185),(575,211),
                   (568,222),(597,235),(632,248),(642,268),(650,295),(649,339),
                   (638,371),(637,423),(648,496),(636,522),(603,599),(522,621),
                   (429,616),(380,600),(369,565),(383,514),(356,501),(353,477),
                   (361,440),(348,426),(348,401),(366,349),(371,298),(381,261)],
        (459,216),(505,592))
    cut("belt", [(378,507),(427,508),(456,502),(484,503),(519,511),(546,523),
                 (594,517),(623,513),(649,526),(654,541),(651,588),(650,639),
                 (646,653),(590,656),(579,646),(574,622),(520,627),(510,649),
                 (506,661),(432,664),(417,651),(414,616),(403,602),(363,613),
                 (359,597),(362,564)], (505,592),(505,646))
    cut("sleeve", [(254,204),(309,198),(345,185),(362,195),(385,225),(393,251),
                   (367,270),(375,295),(367,338),(342,370),(330,397),(325,428),
                   (312,460),(301,486),(273,501),(244,486),(233,471),(242,435),
                   (245,403),(245,366),(255,343),(248,324),(248,280)],
        (342,246),(276,472))
    cut("far sleeve", [(582,236),(635,247),(659,272),(671,308),(671,361),
                       (667,382),(680,430),(694,453),(685,498),(659,497),
                       (635,476),(625,437),(626,369),(631,330),(616,303)],
        (638,267),(665,476))
    cut("vambrace", [(236,458),(273,451),(303,470),(302,506),(293,534),
                     (296,560),(307,581),(310,615),(308,655),(300,675),
                     (250,684),(237,663),(237,622),(227,593),(230,555)],
        (271,469),(276,661))
    cut("glove", [(242,655),(289,641),(305,648),(315,683),(333,714),
                  (338,747),(331,776),(322,770),(313,749),(306,751),
                  (306,778),(317,794),(317,808),(306,812),(286,798),
                  (262,778),(247,751),(245,716),(240,688)], (277,665),(281,776))
    cut("thigh", [(368,581),(496,581),(536,614),(531,668),(513,727),(501,782),
                  (477,825),(465,879),(466,913),(446,951),(428,978),(394,1000),
                  (350,996),(326,972),(321,939),(336,901),(345,877),(321,858),
                  (328,818),(338,785),(337,755),(351,703),(352,669)], (440,608),(403,982))
    cut("far thigh", [(528,573),(626,572),(656,612),(658,669),(657,699),
                      (674,765),(663,803),(659,857),(651,920),(644,961),
                      (624,985),(587,992),(535,992),(510,961),(500,928),
                      (504,865),(496,824),(496,792),(513,742),(523,700)],
        (570,610),(603,980))
    cut("shin", [(327,932),(355,924),(400,932),(439,950),(448,972),(443,1004),
                 (430,1054),(426,1083),(415,1146),(410,1205),(395,1243),
                 (387,1284),(374,1333),(372,1419),(382,1455),(391,1473),
                 (321,1490),(294,1464),(299,1429),(301,1389),(307,1317),
                 (299,1279),(290,1242),(274,1211),(281,1171),(283,1150),
                 (285,1106),(300,1063),(307,1034),(316,988)], (401,979),(342,1446))
    cut("far shin", [(519,930),(561,935),(604,930),(637,935),(657,968),(656,1010),
                     (642,1040),(625,1068),(636,1103),(629,1144),(635,1196),
                     (630,1237),(624,1270),(613,1295),(620,1376),(644,1416),
                     (646,1436),(570,1453),(510,1440),(508,1410),(518,1367),
                     (513,1311),(503,1271),(493,1236),(490,1195),(493,1163),
                     (488,1128),(495,1095),(506,1063),(505,1014),(497,974)],
        (604,975),(561,1422))
    cut("boot", [(292,1418),(348,1414),(376,1418),(387,1449),(401,1471),
                 (407,1494),(414,1511),(413,1531),(288,1534),(280,1508)],
        (341,1443),(338,1527))
    cut("far boot", [(507,1388),(549,1388),(593,1388),(606,1404),(622,1421),
                     (660,1432),(691,1436),(711,1461),(717,1495),(695,1502),
                     (498,1500),(499,1450)], (561,1414),(551,1490))
    cut("rifle", [(1024,0),(2010,0),(2010,216),(1024,216)], (1043,91),(1978,91))
    return parts


def mount_field(f,name,start,end,width):
    tex,base,tip=field_parts()[name]
    f.mount(tex,base,tip,start,end,width)


def fixed_field(f,name,at,scale,tilt=0):
    _,base,tip=field_parts()[name]
    dx=(tip[0]-base[0])*scale
    dy=(tip[1]-base[1])*scale
    angle=radians(tilt)
    end=add(at,(dx*cos(angle)-dy*sin(angle),dx*sin(angle)+dy*cos(angle)))
    mount_field(f,name,at,end,scale)


def paint_arm(f,shoulder,elbow,hand,far=False):
    mount_field(f,"far sleeve" if far else "sleeve",shoulder,elbow,.054 if far else .060)
    wrist=between(hand,elbow,min(.47,4.0/max(1,hypot(hand[0]-elbow[0],hand[1]-elbow[1]))))
    mount_field(f,"vambrace",elbow,wrist,.050 if far else .054)
    mount_field(f,"glove",wrist,hand,.054)


def arm_targets(p):
    sx,sy=p.shoulder
    theta=radians(p.pitch)
    base=(p.tip[0]-44*cos(theta),p.tip[1]-44*sin(theta))
    gun=basis(base,p.tip)
    hand=p.hand or gun(.209,3.45)
    off=p.offhand or gun(.54,1.72)
    far_el=p.far_elbow or bend((sx+3.5,sy+1),off,12.8,13.3)
    near_el=p.elbow or bend((sx-3.5,sy+1.3),hand,12.0,12.4)
    return base,hand,off,far_el,near_el


def neck_socket(p):
    return p.neck or (add(p.shoulder,(1.6,.2)) if p.shouldered else p.shoulder)


def cape_end(p, index):
    if p.cape_tip is not None:
        return p.cape_tip
    sx,sy=p.shoulder
    hx,hy=p.hip
    # 뒤쪽 어깨의 망토는 발·총구 좌표와 분리한다. 지면을 딛고 숙이면 접히고,
    # 달리는 주기에 따라 뒤로 들리며, 멈추면 허벅지 위까지 가라앉는다.
    if 20 <= index <= 25:
        lift=(2,5,4,7,9,2)[index-20]
        return (hx-16-lift*.48, hy+6-lift)
    if p.shouldered:
        return (max(5.6,hx-12.6),hy+6.4)
    if p.knife:
        return (max(5.6,hx-13.8-(sx-hx)*.21),hy+7.0)
    if 11 <= index <= 15:
        return (hx-13.9-(sx-35)*.23,hy+8.2-(sx-35)*.13)
    return (hx-13.5,hy+11.0)


def paint_field_pose(p,index):
    f=Frame()
    sx,sy=p.shoulder
    base,hand,off,far_el,near_el=arm_targets(p)

    mount_field(f,"cape",(sx-2.5,sy+.3),cape_end(p,index),.028)

    for i in (1,0):
        side="far " if i else ""
        foot=p.feet[i]
        hip=add(p.hip,(2 if i else -1.7,0))
        knee=p.knees[i]
        ankle=add(foot,(.15 if i else -.5,-6.8))
        mount_field(f,side+"thigh",hip,knee,.056 if i else .061)
        mount_field(f,side+"shin",knee,ankle,.049 if i else .054)
        mount_field(f,side+"boot",ankle,foot,.049 if i else .056)

    if p.knife:
        mount_field(f,"rifle",add(p.hip,(3.7,-1.2)),p.sling_tip,.044)
    # 뒤쪽 팔은 재킷 뒤로, 앞쪽 팔과 소총은 슬링과 가슴 관측기 앞으로 지나간다.
    paint_arm(f,(sx+3.5,sy+1),far_el,off,True)
    mount_field(f,"jacket",p.shoulder,p.hip,.058)
    # 무릎을 접어도 탄창이 찌그러지지 않게, 허리 회전에만 붙이고 자체 길이는 유지한다.
    belt_tilt=max(-18,min(18,(sx-p.hip[0])*.85))
    fixed_field(f,"belt",p.hip,.057,belt_tilt)
    if not p.knife:
        mount_field(f,"rifle",base,p.tip,.054)
        wrist=between(off,far_el,min(.42,4.0/max(1,hypot(off[0]-far_el[0],off[1]-far_el[1]))))
        mount_field(f,"glove",wrist,off,.054)
    else:
        k=basis(hand,p.knife)
        f.fill([k(.02,-.65),k(.34,-1.02),k(1),k(.44,1.2),k(.16,.74)],"#56606a")
        f.fill([k(.24,-.55),k(1),k(.43,.65)],"#c9cace")
        f.stroke([k(.20,-1.5),k(.19,1.6)],"#9b9b9d",.72)

    if p.magazine:
        x,y=hand
        f.fill([(x-1.3,y-4),(x+1.3,y-4.4),(x+2.1,y+3.22),(x-.73,y+3.87)],"#252731")
        f.stroke([(x-.65,y-3.4),(x+.45,y+2.8)],"#93949b",.63)
    paint_arm(f,(sx-3.5,sy+1.3),near_el,hand)
    if p.grenade:
        x,y=hand
        f.oval(x-.15,y-1.15,2.16,2.65,"#242630")
        f.oval(x-.32,y-1.22,1.59,2.03,"#525b5d")
        f.stroke([(x-1.52,y-1.2),(x+.98,y-1.4)],"#283337",.55)
        f.stroke([(x-.8,y-3),(x+.62,y-3.6)],"#c4b89c",.63)
    fixed_field(f,"helmet",neck_socket(p),.060,p.tilt)
    return f.image()


def layouts():
    stand=Pose()
    brace=replace(stand,hip=(23.1,71.8),shoulder=(34.8,54.8),feet=((14.8,93),(47.8,93)),
                  knees=((12.7,77.5),(44.1,72.4)),tip=MUZZLE,pitch=0,tilt=11,shouldered=True)
    reload=replace(stand,hip=(32.2,58.5),shoulder=(35.3,38.0),feet=((23.1,93),(47.0,93)),
                   knees=((24.8,75.0),(45.8,74.9)),tip=(64.3,71),pitch=47)
    grenade=replace(stand,hip=(32.5,59.7),shoulder=(35.2,39.1),feet=((21,93),(49.4,93)),
                    knees=((26.9,77.1),(47.2,76.6)),tip=(59.5,78.7),pitch=61)
    # 스윕은 허리 높이에서 중계기에 동력을 건다. 저격 때와 달리 얼굴이 개머리판에서 떨어져 있다.
    beam=replace(stand,hip=(31.2,62.7),shoulder=(35.1,41.6),feet=((11.0,93),(56.3,93)),
                 knees=((15.2,75.6),(50.7,72.4)),tip=MUZZLE,pitch=0,tilt=5,cape_tip=(15,69))
    rail=replace(stand,hip=(34.5,57.6),shoulder=(43.6,38.5),pitch=9,tip=(78.5,54),tilt=8)
    duel=replace(stand,hip=(33.5,59.2),shoulder=(39.4,40.3),feet=((18.2,93),(47.2,93)),
                 knees=((23,76),(46,71)),tilt=7,sling_tip=(17,24))
    return {
        0:stand,
        1:replace(brace,hip=(31,61.6),shoulder=(36.7,42.3),feet=((27,93),(47.8,89.8)),knees=((25.4,76.1),(44.7,71.7)),
                  tip=(74.2,57.2),pitch=13,tilt=5,shouldered=False),
        2:replace(brace,hip=(25.5,67.4),shoulder=(34.8,49.8),feet=((19.3,90.0),(47.8,93)),knees=((17.4,75.2),(44,71.9)),
                  tip=(74.2,54.5),pitch=6,tilt=9),
        3:brace,
        4:replace(brace,hip=(20.4,70.8),shoulder=(28.9,53.4),tip=add(MUZZLE,(-6,-2)),pitch=-4,tilt=7),
        5:replace(brace,hip=(22.1,71.6),shoulder=(32.5,54.5),tip=add(MUZZLE,(-2,0)),pitch=-1,tilt=9),
        6:replace(brace,hip=(32.8,59.4),shoulder=(37.2,38.9),tip=(72.9,61.1),pitch=18,tilt=4,shouldered=False),
        7:replace(reload,tip=(64.1,69.2),pitch=36,tilt=13),
        8:replace(reload,tip=(56.6,72.8),pitch=54,hand=(36.8,56),magazine=True,tilt=27),
        9:replace(reload,tip=(58.1,72.2),pitch=49,hand=(42.5,50.3),magazine=True,tilt=21),
        10:replace(reload,tip=(68.3,65.4),pitch=27,hand=(48.6,49),tilt=9),
        11:replace(grenade,hand=(29.1,48.4),elbow=(24.4,46.9),grenade=True,tilt=9),
        12:replace(grenade,hand=(25,32),elbow=(20.6,39.2),grenade=True,tilt=7),
        13:replace(grenade,hip=(31,59.5),shoulder=(32.8,38.7),hand=(15.9,29.1),elbow=(18.9,39.4),grenade=True,tilt=6),
        14:replace(grenade,hip=(42.9,61.4),shoulder=(49.8,41.9),tip=(58.7,83.2),pitch=64,
                   hand=MUZZLE,elbow=(61.9,45.2),far_elbow=(49.3,52.4),tilt=9),
        15:replace(grenade,hip=(38.4,60.5),shoulder=(44,41.1),hand=(63.7,58.2),elbow=(55.4,47.6),tilt=11),
        16:replace(beam,feet=((24.8,91.4),(56.3,93))),
        17:replace(beam,hip=(29.2,65.2),shoulder=(34.0,44.4),knees=((13.8,74.5),(51.3,72.2)),tilt=7,cape_tip=(12,67.5)),
        18:replace(beam,hip=(24.0,66.6),shoulder=(27.1,42.3),knees=((12.5,75.2),(47,70.6)),tip=(66.5,49.6),pitch=-4.1,tilt=-3,cape_tip=(7.2,61)),
        19:replace(beam,hip=(27.7,64.8),shoulder=(32.0,43.4),tip=(70.7,51.6),pitch=-1.3,tilt=4,cape_tip=(10.1,63.2)),
        20:replace(rail,feet=((10.3,90.1),(54.6,93)),knees=((24.8,71.3),(51.4,68.2))),
        21:replace(rail,hip=(36,55.6),feet=((26.5,72.5),(45.7,91.7)),knees=((22.8,66.2),(48.3,72.1))),
        22:replace(rail,hip=(34.7,58.4),feet=((22.5,93),(55,84.8)),knees=((29.1,73.3),(55.6,64.8))),
        23:replace(rail,hip=(35.7,55.8),feet=((23.6,88.1),(42.8,73.4)),knees=((29,69.5),(48.2,62.5))),
        24:replace(rail,hip=(34.1,58.0),feet=((17.7,76.3),(49.2,87.3)),knees=((23.2,62.5),(51.7,66.3)),pitch=21),
        25:replace(rail,hip=(34.6,66.5),shoulder=(43.8,46.6),feet=((18.8,93),(54,93)),knees=((17.5,77),(53.5,73)),pitch=18),
        26:replace(brace,hip=(21.0,74.1),shoulder=(32.7,58.8),pitch=-8.5,tilt=10),
        27:replace(brace,hip=(24.4,68.8),shoulder=(35.4,50.9),pitch=8.5,tilt=12),
        30:replace(duel,hand=(53.2,62.1),elbow=(44.4,54.7),offhand=(34,51),far_elbow=(30,45),knife=(58.4,54.5)),
        31:replace(duel,hip=(31.1,63.3),shoulder=(45.6,44.0),feet=((8,93),(54,93)),knees=((24.1,77),(48,70)),
                   hand=(59,71.7),elbow=(52.4,55.6),offhand=(21,59.5),far_elbow=(30.8,49),knife=(68,78.2),sling_tip=(18,28)),
        32:replace(duel,hip=(32.5,61.6),shoulder=(40,41.2),feet=((17.5,93),(46.7,93)),knees=((25,76.5),(49,72)),
                   hand=(35.8,71.9),elbow=(29.3,58.4),offhand=(50,59),far_elbow=(48,45),knife=(47.8,73),sling_tip=(17.2,27)),
        33:replace(duel,hip=(34,60.8),shoulder=(42,40.5),feet=((10.3,93),(50.5,93)),knees=((24.5,74.8),(50,68.5)),
                   hand=(55,49.8),elbow=(49.6,40),offhand=(22,58),far_elbow=(30,46.3),knife=(65.5,42.6),sling_tip=(15,24.2)),
    }


def optic(frame):
    f=Frame()
    if frame==28:
        f.panel([(28,51),(36,49),(44,54),(49,57.6),(49,60.6),(43,63),(37,69),(27,70),(23,66),(23,55)],"#1c242d","#64717b")
        f.panel([(25,55),(36,52),(42,56),(42,62),(36,65),(25,66)],"#27313a","#505e67")
        f.stroke([(25,54.5),(29,52),(35,51.5),(38,53.1)],"#a0afba",.62)
        for x in (26.6,28.7,30.8):
            f.stroke([(x,56.7),(x,63.2)],"#85919b",.61)
        f.oval(41.9,59.7,5.9,4.1,"#1b2128")
        f.oval(44.4,59.5,3.1,2.6,"#a46e78")
        f.oval(46.1,59.4,1.35,1.75,"#f5d7d2")
    else:
        f.fill([(42,59.5),(46.5,58.5),(45.1,53.3),(48.2,57.7),(52.1,54.7),(50,58.7),(55,59.4),(50.1,60.7),(52.3,65),(48.1,61.5),(46,65),(46.5,60.9)],"#f4a0a8")
        f.oval(48.3,59.5,2.85,1.7,"#ffeed9")
        f.stroke([(43,59.5),(54,59.4)],"#fff9e6",.4)
    return f.image()


TRANSITIONS = [
    (0,1),(1,2),(2,3),(3,4),(4,5),(5,6),(7,8),(8,9),(9,10),
    (13,14),(14,15),(15,11),(24,25),(25,0),(0,16),(17,18),(18,19),
    (20,21),(21,22),(22,23),(23,20),(5,7),(11,12),(12,13),(10,0),
]


def inbetween(poses,a,b):
    early,late=poses[a],poses[b]
    _,hand_a,off_a,far_a,near_a=arm_targets(early)
    _,hand_b,off_b,far_b,near_b=arm_targets(late)
    mid=lambda x,y:between(x,y,.5)
    # 두 장을 겹쳐 반투명한 두 팔을 만드는 대신, 한 벌의 원화를 새 골격에 입힌다.
    # 양손이 총에 붙은 자세끼리는 중간 원화에서도 손을 정확히 그때의 손잡이로 옮긴다.
    return replace(early,
        hip=mid(early.hip,late.hip),shoulder=mid(early.shoulder,late.shoulder),
        # 앞발 착지 후에 뒷발을 여는 순서. 양발을 지면 위로 동시에 미끄러뜨리지 않는다.
        feet=(early.feet[0],late.feet[1]) if (a,b)==(1,2) else
             (early.feet[0],(52.1,89.7)) if (a,b)==(0,16) else
             tuple(mid(x,y) for x,y in zip(early.feet,late.feet)),
        knees=tuple(mid(x,y) for x,y in zip(early.knees,late.knees)),
        tip=mid(early.tip,late.tip),pitch=(early.pitch+late.pitch)*.5,tilt=(early.tilt+late.tilt)*.5,
        hand=mid(hand_a,hand_b) if early.hand or late.hand else None,
        offhand=mid(off_a,off_b) if early.offhand or late.offhand else None,
        elbow=mid(near_a,near_b) if early.elbow or late.elbow else None,
        far_elbow=mid(far_a,far_b) if early.far_elbow or late.far_elbow else None,
        grenade=early.grenade,magazine=early.magazine or late.magazine,
        neck=mid(neck_socket(early),neck_socket(late)),
        cape_tip=mid(cape_end(early,a),cape_end(late,b)))


def articulation(poses):
    """실제 손 놓기·격발에 맞춘 추가 몸짓. 복제 사수도 같은 원화로 무기를 다룬다."""
    frames = []
    for source in (3,26,27):
        p=poses[source]
        # 조준선이 움직이지 않도록 총구·발은 고정하고, 어깨와 팔꿈치, 망토만 호흡한다.
        frames.extend([
            replace(p,hip=add(p.hip,(-.45,.65)),shoulder=add(p.shoulder,(-.8,.9)),
                    tilt=p.tilt-2,cape_tip=add(cape_end(p,source),(-2.3,-1))),
            replace(p,hip=add(p.hip,(-.9,1)),shoulder=add(p.shoulder,(-1.15,1.45)),
                    tilt=p.tilt-3,cape_tip=add(cape_end(p,source),(-3.6,-1.8)))])
    p=poses[13]
    frames.extend([
        replace(p,shoulder=(34.0,39.3),hip=(32.5,60),hand=(23,23),elbow=(19,35),
                tilt=8,cape_tip=(15.1,64.6)),
        replace(p,shoulder=(45.5,40.3),hip=(40,61),hand=(63,38),elbow=(51,35.6),
                tilt=12,cape_tip=(20.3,61)),
        replace(poses[11],shoulder=(36.8,39.2),hand=(42.1,49.7),elbow=(32.3,48.1),grenade=False,
                cape_tip=(16,66.0)),
        replace(p,shoulder=(34.1,39.2),hip=(32.4,59.9),hand=(23.9,31.4),elbow=(21.2,40.7),
                cape_tip=(16.8,64.5)),
        inbetween(poses,0,11),
        replace(poses[8],hand=(33.9,60.4),elbow=(22.8,51.5),tilt=24,cape_tip=(17,68)),
        replace(poses[9],hand=(40.5,57.1),elbow=(26.6,52),tilt=22,cape_tip=(16.1,68.5)),
        replace(poses[10],hand=(44.7,51.1),tilt=15,tip=(64.9,68.3),pitch=37),
        replace(poses[16],hip=(30.4,63.5),shoulder=(34.9,42.7),feet=((17.3,90.5),(56.3,93)),tilt=6,cape_tip=(13.6,68.4)),
        replace(poses[17],hip=(28.9,65.5),shoulder=(33.1,44.9),tilt=8,cape_tip=(11,67.0)),
        replace(poses[19],hip=(26.2,65.8),shoulder=(29.9,43.1),tip=(69.4,51.3),pitch=-1.8,
                cape_tip=(6.5,60.5)),
        replace(poses[19],hip=(28.7,64.2),shoulder=(33.1,43.7),tip=(71.3,52.0),
                cape_tip=(9.3,61.6)),
        replace(poses[0],shoulder=(35.3,36.6),hip=(33.45,57.7),tip=(73.1,61.7),tilt=-1,cape_tip=(18.4,68.8)),
        replace(poses[0],shoulder=(35.1,37.0),hip=(33.3,57.9),tip=(72.8,62.3),tilt=-2,cape_tip=(17.2,68.1)),
    ])
    for source in (26,27):
        p=poses[source]
        # 상·하향 사격은 같은 원점에서 그 방향의 견착 자세를 유지한 채 반동한다.
        frames.extend([replace(p,shoulder=add(p.shoulder,(dx,dy)),hip=add(p.hip,(dx*.42,dy*.35)),
            tip=add(p.tip,(dx,dy)),pitch=p.pitch+da,tilt=p.tilt+da,
            cape_tip=add(cape_end(p,source),(dx*.6,-.8))) for dx,dy,da in
            ((-3,-1,-2),(-6,-2,-4),(-2,0,-1))])
    return frames


def bake():
    stride=CELL*DENSITY
    poses=layouts()
    atlas=Image.new("RGBA",(stride*6,stride*6))
    for index,p in poses.items():
        atlas.alpha_composite(paint_field_pose(p,index),(index%6*stride,index//6*stride))
    for index in (28,29):
        atlas.alpha_composite(optic(index),(index%6*stride,index//6*stride))
    atlas.save(ART/"JokerRifleMotion.png",optimize=True)
    intermediate=Image.new("RGBA",(stride*5,stride*5))
    for i,(start,end) in enumerate(TRANSITIONS):
        pose=inbetween(poses,start,end)
        intermediate.alpha_composite(paint_field_pose(pose,start),(i%5*stride,i//5*stride))
    intermediate.save(ART/"JokerRifleTransitions.png",optimize=True)
    # 긴 총과 발을 모두 담은 공통 영역만 싣는다. 발 피벗 (103.5,281.25)의 원본 좌표는 그대로다.
    trim=(3,50,231,286)
    w,h=trim[2]-trim[0],trim[3]-trim[1]
    accents=Image.new("RGBA",(w*7,h*4))
    for i,p in enumerate(articulation(poses)):
        frame=paint_field_pose(p,0)
        l,t,r,b=frame.getbbox()
        if not (trim[0] < l and trim[1] < t and r < trim[2] and b < trim[3]):
            raise ValueError(f"조커 추가 몸짓 {i}이 인접 셀에 닿습니다.")
        accents.alpha_composite(frame.crop(trim),(i%7*w,i//7*h))
    accents.save(ART/"JokerRifleArticulation.png",optimize=True)


if __name__ == "__main__":
    bake()
