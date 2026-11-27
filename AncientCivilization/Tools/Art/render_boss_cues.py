"""보스별 예고·발동·접점을 직접 그린 원화. 오른쪽이 발사 방향, (40, 40)이 월드 접점."""
from pathlib import Path
from math import cos, sin, pi
from PIL import Image, ImageDraw

W,H,N,SS = 112,80,8,3
# 외곽, 짙은 면, 중간 면, 빛 받는 면, 광원
INK = [
 ('#34252B','#803B28','#D36131','#FFAD58','#FFF1B5'), # 안전 걸쇠와 굵은 리벳
 ('#1E2535','#30475C','#698B91','#FFC051','#FFF4C7'), # 피스톤, 강철, 가열된 단면
 ('#192844','#284C77','#40A9C9','#95EDFF','#EDFEFF'), # 홀로그램 필름
 ('#26233C','#59416E','#9378BC','#EFBD69','#FFF6CC'), # 궤도 레일과 화물 고정구
 ('#242538','#4B405D','#9A6179','#F39A97','#FFF5DA'), # 반쪽 가면, 얇은 사선
 ('#322634','#684737','#B1854B','#F2CB79','#FFF8D1'), # 태엽 이빨
 ('#252640','#4E4074','#9367B3','#F7B6A1','#FFFAEA'), # 과부하 코어의 유리 프리즘
 ('#2B2524','#594A3C','#A48A60','#E6C477','#FFF2B8'), # 하역 장치의 육각 너트
]

class Cel:
 def __init__(self,pal,t):
  self.im=Image.new('RGBA',(W*SS,H*SS));self.d=ImageDraw.Draw(self.im)
  self.p=pal; self.t=t
 def xy(self,pts):return [(round((40+x)*SS),round((40+y)*SS)) for x,y in pts]
 def line(self,pts,c,w=1.1):
  self.d.line(self.xy(pts),fill=self.p[c],width=max(1,round(w*SS)),joint='curve')
 def poly(self,pts,c,edge=True):
  self.d.polygon(self.xy(pts),fill=self.p[c])
  if edge:self.line(pts+[pts[0]],0,.85)
 def diamond(self,x,y,a,b,c):
  self.poly([(x-a,y),(x,y-b),(x+a,y),(x,y+b)],c)
 def arc(self,x,y,r,lo,hi,c,w):
  steps=max(4,round(abs(hi-lo)*r/44))
  self.line([(x+r*cos((lo+(hi-lo)*i/steps)*pi/180),y+r*sin((lo+(hi-lo)*i/steps)*pi/180)) for i in range(steps+1)],c,w)
 def sliver(self,x,y,dx,dy,width,c):
  length=max(1,(dx*dx+dy*dy)**.5);nx,ny=-dy*width/length,dx*width/length
  self.poly([(x,y),(x+dx*.28+nx,y+dy*.28+ny),(x+dx,y+dy),(x+dx*.22-nx*.55,y+dy*.22-ny*.55)],c)
 def hoop(self,x,y,r,n,c,start=0):
  pts=[(x+r*cos(start+i*2*pi/n),y+r*sin(start+i*2*pi/n)) for i in range(n)]
  self.line(pts+pts[:1],c,1.7)
 def particles(self,angles,r,length,c):
  for i,a in enumerate(angles):
   a=a*pi/180;co,si=cos(a),sin(a);q=r*(.85+(i%3)*.09)
   self.sliver(q*co,q*si,co*length*(1-.11*(i%3)),si*length*(1-.11*(i%3)),1.25+(i%2)*.5,c if i%2 else min(4,c+1))

def captain(v,b,t):
 if b==0:
  for s in [-1,1]:
   x=-17+10*t;y=s*(14-8*t)
   v.poly([(x-5,y),(x+2,y-3*s),(x+12,y-3*s),(x+5,y+2*s),(x-4,y+5*s)],2);v.line([(x-1,y+2*s),(x+7,y-s)],4,1.3)
  v.diamond(5,0,2+3*t,2,4)
 elif b==1:
  for s in [-1,1]:
   v.sliver(-4,s*2,25+13*t,s*(6+3*t),3.2*(1-t*.7),3)
   v.poly([(-10-7*t,s*4),(-5-4*t,s*2),(-9-8*t,s*10),(-16-8*t,s*13)],2)
  v.sliver(-5,0,36+16*t,-1,3.4-2*t,4)
 else:
  v.particles([-62,-28,6,35,62,112,-118],5+17*t,12-5*t,2)
  for s in [-1,1]: v.line([(-6-7*t,s*(5+6*t)),(-1,s*(3+3*t)),(6+8*t,s*(5+6*t))],3,1.6)

def mech(v,b,t):
 if b==0:
  for s in [-1,1]:
   x=-17+9*t; y=s*(16-7*t)
   v.poly([(x-6,y),(x-2,y-s*5),(x+13,y-s*5),(x+19,y),(x+12,y+s*3),(x-4,y+s*3)],2)
   for i in range(3):v.line([(x+i*4,y),(x+2+i*4,y-3*s)],4,1.3)
 elif b==1:
  for s in [-1,1]:
   v.sliver(-7,s,23+16*t,s*(14+5*t),4.5-2*t,2)
   v.sliver(-6,s*2,40+12*t,s*(4+2*t),4.5-2*t,3)
  v.sliver(-5,0,40+18*t,0,4.5-3*t,4)
 else:
  for i,a in enumerate([-66,-34,7,45,77]):
   a*=pi/180; r=7+21*t
   x=r*cos(a);y=r*sin(a);v.poly([(x-4,y),(x-1,y-4),(x+7,y-2),(x+5,y+2),(x-2,y+3)],2);v.line([(x-1,y-3),(x+5,y-2)],3,1.2)
  for s in [-1,1]:v.arc(0,0,6+12*t,-55+s*72,55+s*72,3,2.2-t)

def enforcer(v,b,t):
 for i,s in enumerate([-1,1]):
  if b==0:
   x=-12+5*t+i*3;y=s*(14-6*t)
   v.poly([(x-5,y),(x+14,y),(x+12,y+s*3),(x-7,y+s*3)],2)
   v.line([(x-6,y+s*5),(x-6,y+s*8),(x+2,y+s*8)],3,1.3)
   v.line([(x+6,y+s*6),(x+10,y+s*6)],4)
  elif b==1:
   x=-8+i*7;y=s*3
   v.poly([(x,y),(31+15*t,y),(39+12*t,y+s*3),(x+3,y+s*3)],3)
   v.line([(8,s*(7+4*t)),(26+i*6+13*t,s*(7+4*t))],2,1.7)
  else:
   for j in range(3):
    x=-6+j*8+t*(j+2)*3; y=s*(3+j*4+9*t)
    v.poly([(x-5,y-1),(x+3+j,y-1),(x+3+j,y+2),(x-5,y+2)],2+j%2)
 if b==1:v.sliver(-4,0,49+10*t,0,1.8,4)
 if b==2:v.line([(-2-6*t,-8-7*t),(-2-6*t,-2-7*t),(4-6*t,-2-7*t)],4)

def orbital(v,b,t):
 if b==0:
  for i in range(3):
   a=120*i+t*21;v.arc(0,0,16-4*t,a,a+65,2,3)
   v.arc(0,0,19-4*t,a+3,a+18,4,1.25)
  v.diamond(0,0,3.5,3.5,3)
 elif b==1:
  for s in [-1,1]:
   v.line([(-8,s*(9+4*t)),(10,s*(9+4*t)),(20+17*t,s*2),(39+11*t,s*2)],3,2.5)
   v.sliver(-4,s*2,30+10*t,s*2,2.6,2)
  v.sliver(-2,0,46+9*t,0,1.8,4)
 else:
  for i in range(4):
   a=i*90-33+24*t;v.arc(0,0,9+13*t,a,a+53,2,3.4-t);v.arc(0,0,12+13*t,a+2,a+22,3,1.5)
  v.particles([-45,10,57,160,-130],6+10*t,8,3)

def joker(v,b,t):
 if b==0:
  for s in [-1,1]:
   x=-13+8*t;y=s*(12-6*t)
   v.poly([(x-6,y+s*3),(x+5,y-s*2),(x+16,y-s*4),(x+5,y+s*2)],2)
   v.line([(x+5,y),(x+15,y-s*3)],3)
  v.diamond(8,0,3.5,1.4,4)
 elif b==1:
  v.sliver(-6,0,53+7*t,-1,2.8-t*1.7,4)
  v.sliver(-3,1,28+8*t,6+2*t,2.5-t,3);v.sliver(-4,-1,18+12*t,-12-2*t,2,2)
  v.diamond(-5-6*t,3+4*t,3,1.7,3)
 else:
  for i,(a,bias) in enumerate([(-68,2),(-29,0),(18,1),(46,2),(141,1)]):
   aa=a*pi/180;r=8+15*t+bias
   v.sliver(r*cos(aa),r*sin(aa),cos(aa)*(12-i),sin(aa)*(12-i),1.4,2+(i%3))
  v.line([(-9-10*t,6+5*t),(5+8*t,-4-5*t)],3,1.5)

def clock(v,b,t):
 if b==0:
  for i in range(3):
   a=120*i+28*t;v.arc(0,0,14,a,a+67,3,2.4)
   for k in [9,24,39]:
    q=(a+k)*pi/180;v.line([(15*cos(q),15*sin(q)),(18*cos(q),18*sin(q))],2,2)
  v.sliver(0,0,9+3*t,-5+t,1.5,4)
 elif b==1:
  v.arc(-1,0,9+5*t,55,300,3,2.3);v.diamond(-1,0,5,4,4)
  v.poly([(1,0),(12,-4),(40+12*t,0),(11,3)],3);v.line([(5,0),(37+10*t,0)],4)
 else:
  for i in range(5):
   a=(72*i-27)*pi/180;r=8+t*13
   v.diamond(r*cos(a),r*sin(a),2.6,2.9,3);v.line([((r+3)*cos(a),(r+3)*sin(a)),((r+6)*cos(a),(r+6)*sin(a))],4)
  v.arc(0,0,5+10*t,155,272,2,2)

def convoy(v,b,t):
 if b==0:
  for i in range(3):
   ang=2*pi*i/3 + .35*t;r=10-4*t;x=r*cos(ang);y=r*sin(ang)
   v.poly([(x,y-7),(x+4,y-2),(x,y+7),(x-3,y+1)],2);v.line([(x,y-4),(x+1,y+2)],4)
 elif b==1:
  for s in [-1,1]:
   v.sliver(-4,0,30+18*t,s*(9+3*t),3,2)
   v.sliver(1,s,30+10*t,s*3,2.7,3)
  v.sliver(-5,0,47+8*t,0,3.0-t*2,4)
 else:
  for i,a in enumerate([-122,-65,-23,15,73,143]):
   q=a*pi/180;r=7+(12+i%3*3)*t;x=r*cos(q);y=r*sin(q)
   v.poly([(x-2,y-3),(x+4,y-5),(x+2,y+5),(x-3,y+2)],2+i%2);v.line([(x-1,y-2),(x+3,y-4)],4)
  v.diamond(0,0,max(1,8-7*t),max(1,9-7*t),4)

def guardian(v,b,t):
 if b==0:
  v.hoop(0,0,13-3*t,6,2);v.hoop(0,0,7-2*t,6,3)
  for s in [-1,1]:v.line([(-10,s*(17-3*t)),(-3,s*(17-3*t)),(0,s*(14-3*t))],4,1.3)
 elif b==1:
  for i in range(3):
   x=2+i*(9+t*3);v.line([(x,-9+i*2),(x+9,0),(x,9-i*2)],2+i,2.6-i*.4)
  v.sliver(-8,0,34+11*t,0,2.4,3)
 else:
  v.particles([-72,-34,4,35,86,143,-132],5+15*t,9-3*t,2)
  v.arc(0,0,6+9*t,65,218,3,2.3)

PAINTERS=(captain,mech,enforcer,orbital,joker,clock,convoy,guardian)
def render():
 atlas=Image.new('RGBA',(W*N,H*len(PAINTERS)*3))
 for style,paint in enumerate(PAINTERS):
  for beat in range(3):
   for i in range(N):
    t=i/(N-1);v=Cel(INK[style],t);paint(v,beat,t)
    im=v.im.resize((W,H),Image.Resampling.LANCZOS)
    # 외곽선과 모양은 유지하며 마지막 두세 장에서 여운을 지운다.
    fade=(1-t*.16) if t<.56 else max(.14,1-(t-.56)*1.88)
    im.putalpha(im.getchannel('A').point(lambda a:round(a*fade)))
    atlas.paste(im,(i*W,(style*3+beat)*H))
 return atlas
if __name__=='__main__':
 render().save(Path(__file__).resolve().parents[2]/'Assets/Resources/Art/BossCueAtlas.png',optimize=True)
