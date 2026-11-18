"""Review the canonical authored geometry; this is a source diagram, not a Unity capture."""
from pathlib import Path
import argparse
import json
import subprocess

TEMPLATE = r'''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>PLUGIN · 지역 맵 배치도</title>
<style>*{box-sizing:border-box}body{margin:0;background:#0c151c;color:#dce9e9;font:15px system-ui}main{max-width:1440px;margin:auto;padding:30px}h1{font-size:30px;font-weight:600;margin:10px 0}p{line-height:1.7;color:#98b0b6}small{color:#89a3ab}nav{display:flex;gap:10px;flex-wrap:wrap;margin:26px 0}button,select{font:inherit;padding:10px 15px;border:1px solid #47616c;background:#192c36;color:#edf7f5;border-radius:4px}button[aria-pressed=true]{border-color:#7be2d2;color:#7be2d2}svg{display:block;width:100%;height:auto;border:1px solid #39505c;background:#10212c}aside{display:flex;gap:22px;flex-wrap:wrap;padding:20px 0;color:#b5cccc}.cards{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}.card{padding:18px;background:#172a32;border-top:2px solid #4e7478}.card h3{font-size:16px;margin:0 0 8px}.card p{font-size:14px;margin:0}a{color:#86ded5}@media(max-width:650px){main{padding:16px}.cards{grid-template-columns:1fr}h1{font-size:23px}}</style>
<main><small>PLUGIN / ARCHITECTURAL ROUTES / v0.31</small><h1 id="title"></h1><p id="subtitle"></p><p>Unity 코드가 사용하는 실제 타일 좌표를 그린 배치도입니다. 배경 원화·조명·캐릭터가 렌더링된 Unity 실행 화면은 아닙니다.</p>
<nav><select id="floor" aria-label="층 선택"></select><select id="framing" aria-label="배치도 범위"><option value="all">전체 경로</option><option value="entry">진입 구간</option><option value="challenge">중앙 난관</option><option value="exit">출구 구간</option></select><button id="guide" aria-pressed="true">앵커 연결선</button><button id="safe" aria-pressed="false">안전 바닥 보기</button></nav>
<svg id="map" role="img" aria-label="실제 지형과 앵커 배치도"></svg><aside><span>청록 원: 고정</span><span>금색 원: 일회용</span><span>녹색 원: 이동</span><span>초록 발판: 재시작 지점</span><span>붉은 띠: 설비 아래 위험 구역</span></aside><div class="cards" id="cards"></div><p id="stats"></p><p><a href="DISTRICT_MAPS_KO.md">실행 경로·설계 의도·Unity 확인 항목</a> · 기존 스토리와 보스는 메인 순찰에 유지됩니다.</p></main>
<script>const maps=__MAPS__;let floor=0,guide=true,safe=false;const $=id=>document.getElementById(id);maps.forEach((m,i)=>$('floor').add(new Option(m.title,i)));const svgNS='http://www.w3.org/2000/svg';
function draw(){const m=maps[floor],svg=$('map');svg.replaceChildren();$('title').textContent=m.title;$('subtitle').textContent=m.description;
let start=0,width=m.width;const frame=$('framing').value;if(frame==='entry'){width=36;}if(frame==='challenge'){start=m.gapLeft-12;width=42;}if(frame==='exit'){start=m.width-36;width=36;}
svg.setAttribute('viewBox',`${start-2} -2 ${width+4} ${m.ceiling+10}`);
function add(tag,attrs,parent=svg){const n=document.createElementNS(svgNS,tag);Object.entries(attrs).forEach(([k,v])=>n.setAttribute(k,v));parent.append(n);return n;}
function rect(x,y,w,h,color,extra={}){add('rect',{x,y:m.ceiling-y,width:w,height:h,fill:color,...extra});}
function text(x,y,t,color='#aac3c8',size=.8){const n=add('text',{x,y:m.ceiling-y,fill:color,'font-size':size,'font-family':'system-ui'});n.textContent=t;}
for(let x=0;x<=m.width;x+=2)add('line',{x1:x,x2:x,y1:0,y2:m.ceiling+5,stroke:'#25404b','stroke-width':.025});for(let y=0;y<=m.ceiling;y+=2)add('line',{x1:0,x2:m.width,y1:y,y2:y,stroke:'#25404b','stroke-width':.025});
for(let i=0;i<8;i++){let x=1+i*14;rect(x,m.ceiling-2,11,m.ceiling-2,'#142b35');for(let y=5;y<m.ceiling-3;y+=3.5)for(let n=0;n<3;n++)rect(x+1+n*3.2,y,1.8,1.4,(i+n)%3?'#21414a':'#594a30');}
rect(0,m.ceiling+1,m.width,1,'#50646c');text(1,m.ceiling+1.5,'연속 천장 / 와이어 가능','#a2bcc4',.75);
m.decks.forEach(d=>{const h=d.bridge?.65:d.top+5;rect(d.left,d.top,d.right-d.left,h,'#344b57',{'stroke':'#86a4a7','stroke-width':.07});rect(d.left,d.top,d.right-d.left,.14,'#a7c1be');if(d.bridge)for(let x=d.left+.5;x<d.right;x+=3)rect(x,d.top-.7,.13,d.top+4.3,'#28414c');text(d.left+.3,d.top-1.1,d.name,'#a8bec4',.65);});
rect(m.gapLeft,-2,m.gapRight-m.gapLeft,2,safe?'#365a5e':'#722e26');text(m.gapLeft,-4.9,safe?'실패 후 걸어서 복구 가능한 안전 배수로':'한 곳의 필수 난관 / 아래는 위험 설비','#d1a486',.7);
if(guide)add('polyline',{points:m.hooks.map(h=>`${h.x},${m.ceiling-h.y}`).join(' '),fill:'none',stroke:'#62c9d1','stroke-width':.1,'stroke-dasharray':'.4 .4',opacity:.8});
m.hooks.forEach(h=>{const c=['#64d8ed','#e9b65d','#79e2a5'][h.kind];add('line',{x1:h.x,x2:h.x,y1:0,y2:m.ceiling-h.y,stroke:'#38515a','stroke-width':.07});if(h.kind===2)rect(h.x-1,h.y+1,2,.1,'#79e2a5');add('circle',{cx:h.x,cy:m.ceiling-h.y,r:.42,fill:'#0d1c26',stroke:c,'stroke-width':.12});});
const rescue=m.Recovery;add('circle',{cx:rescue.x,cy:m.ceiling-rescue.y,r:.4,fill:'#28564c',stroke:'#72d3bb','stroke-width':.1});text(rescue.x+.8,rescue.y,'복구 앵커','#72d3bb',.65);
m.stops.forEach((s,i)=>{rect(s.x-1.2,s.y-1,2.4,.15,'#65dda1');text(s.x-1,s.y+1.6,`C${i+1} ${s.name}`,'#8ce2b2',.75);});rect(m.width-4,4,2.5,4,'#2c7f7b');text(m.width-7,5.3,'연결문','#a1ece2');
if(floor===1){rect(m.mechanismX-.7,8,1.4,8,'#e6b84a',{opacity:.25});text(m.mechanismX-3,9,'감시 탐조등','#e6b84a');}if(floor===2){rect(m.mechanismX-.375,8,.75,8,'#e19c5e',{opacity:.6});text(m.mechanismX-2,8.8,'보안 셔터','#e6b84a');}if(floor===3){rect(m.mechanismX-1.5,11,3,9.4,'#65cbe6',{opacity:.2});text(m.mechanismX-2,12,'상승 환기류','#65cbe6');}if(floor===4)text(m.mechanismX-6,20,'외벽 접속 장치 / 시간 창','#e6b84a');
for(let x=0;x<=m.width;x+=10)text(x,-6.4,String(x),'#78949e',.65);
$('cards').replaceChildren();m.stops.forEach((s,i)=>{const card=document.createElement('article');card.className='card';const h=document.createElement('h3');h.textContent=`0${i+1} / ${s.name}`;const p=document.createElement('p');p.textContent=[`안전한 진입. 바닥길에서 조작을 확인하고 건물 위 정비로로 올라갑니다.`,`첫 난관 전에 재시작 지점을 확보합니다. ${m.gapRight-m.gapLeft}타일의 설비 구역은 앵커와 천장으로 통과합니다.`,`난관을 지나 넓은 보행로로 내려옵니다. 연결문 앞에서는 다음 구역을 선택할 수 있습니다.`][i];card.append(h,p);$('cards').append(card);});
$('stats').textContent=`길이 ${m.width}타일 · 닫힌 천장 ${m.ceiling}타일 · 보행면 ${m.decks.length}개 · 주 경로 앵커 ${m.hooks.length}개 + 상시 복구 앵커 · 재시작 지점 3곳`;}
$('floor').onchange=e=>{floor=+e.target.value;draw();};$('framing').onchange=draw;$('guide').onclick=()=>{guide=!guide;$('guide').setAttribute('aria-pressed',guide);draw();};$('safe').onclick=()=>{safe=!safe;$('safe').setAttribute('aria-pressed',safe);draw();};draw();</script></html>'''

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet',required=True)
    args=parser.parse_args()
    root=Path(__file__).resolve().parents[1]
    result=subprocess.run([str(Path(args.dotnet).resolve()),'run','--project',str(root/'Tools/DistrictRouteChecks.csproj'),'-c','Release','--no-build','--','--export'],check=True,text=True,capture_output=True)
    maps=json.loads(result.stdout)
    output=root/'DistrictMapPreview.html'
    output.write_text(TEMPLATE.replace('__MAPS__',json.dumps(maps,ensure_ascii=False)),encoding='utf-8')
    print('PASS: canonical five-district source diagram ->',output)
if __name__=='__main__':main()
