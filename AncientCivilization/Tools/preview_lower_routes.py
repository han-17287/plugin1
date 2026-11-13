"""Make a local, interactive map-data review from LowerRouteExport JSON.

This draws exported collision geometry and dialogue markers. It is not a
Unity screenshot, playable build, physics simulation, or playtime estimate.
"""
import argparse
import json
from pathlib import Path


HTML = r'''<!doctype html>
<html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>PLUGIN · 하층 지도 데이터 검토</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#08131e;color:#d5e6ee;font:15px system-ui,sans-serif}
header{padding:22px 28px;border-bottom:1px solid #284151;background:#101f2c}
h1{font-size:23px;margin:0 0 8px}p{margin:6px 0;line-height:1.6}.muted{color:#8eacbd;font-size:13px}
nav{padding:14px 28px;display:flex;gap:10px;flex-wrap:wrap;align-items:center}
button,select{color:#d5e6ee;background:#162d3f;border:1px solid #37647d;border-radius:5px;padding:9px 12px;font:inherit;cursor:pointer}
button:hover,button[aria-pressed=true]{background:#235675}select{min-width:260px}
main{display:grid;grid-template-columns:minmax(0,1fr) 345px;gap:20px;padding:0 28px 28px}
.map{border:1px solid #284151;border-radius:8px;overflow:hidden;background:#0c1a28}
canvas{display:block;width:100%;height:480px;touch-action:none;cursor:grab}
.bar{padding:12px 16px;background:#102433;display:flex;flex-wrap:wrap;gap:16px;font-size:13px}
.dot{display:inline-block;width:10px;height:10px;border-radius:50%;margin-right:6px}
aside{background:#102433;padding:20px;border:1px solid #284151;border-radius:8px;max-height:560px;overflow:auto}
aside h2{font-size:19px;margin-top:0}.line{padding:10px 0;border-top:1px solid #284151}.line b{display:block;font-size:12px;color:#69cdeb;margin-bottom:3px}
#stops{display:flex;gap:8px;padding:14px 16px;flex-wrap:wrap}#stops button{font-size:13px}
#hint{padding:0 16px 14px}footer{padding:0 28px 28px;font-size:13px;color:#8eacbd}
@media(max-width:950px){main{grid-template-columns:1fr}aside{max-height:none}canvas{height:360px}}
</style>
<header><h1>PLUGIN / 최하층·하층 지도 데이터 검토</h1>
<p>연속 이동로 · 필수 현장 48곳 · 선택 현장 24곳 · 단차 · 정비 통로 · 지붕 지름길</p>
<p class="muted">실제 C# 배치와 대사에서 추출한 검토 도면입니다. Unity 실행 화면과 완주 시간 측정 결과가 아닙니다.</p></header>
<nav><select id="chapter" aria-label="사건 경로"></select><span id="sectors" style="display:flex;gap:8px;flex-wrap:wrap"></span>
<button id="fit">전체 구간</button><button id="previous">이전 블록</button><button id="next">다음 블록</button></nav>
<main><section class="map"><canvas id="map" aria-label="현재 경로의 발판과 현장 위치" tabindex="0"></canvas>
<div class="bar"><span><i class="dot" style="background:#f7bc62"></i>필수 현장 / 미해결 통제선</span><span><i class="dot" style="background:#76b5fb"></i>선택 현장</span><span><i class="dot" style="background:#57d7b2"></i>구간 인계 지점</span></div>
<div id="stops"></div><p id="hint" class="muted">드래그로 이동 · 휠로 확대 · 현장 버튼으로 대사 확인 · 좌우 방향키로 이동</p></section>
<aside id="detail"></aside></main><footer>녹색 작은 표식은 착지하면 저장되는 안전 발판입니다. 천장에 와이어 가능. 하역 단차·낮은 배관로·지붕 길과 상부 정비 발판을 이용합니다. 주민 호송 길은 안전한 평지를 유지합니다. 필수 행동 현장은 해결 즉시 열리며, 증언 현장은 대화로 진행합니다.</footer>
<script>
const data=__DATA__,canvas=document.querySelector('canvas'),ctx=canvas.getContext('2d');
const chapter=document.querySelector('#chapter'),sectors=document.querySelector('#sectors'),detail=document.querySelector('#detail');
let caseIndex=0,sectorIndex=0,selected=-1,centre=0,span=832,drag=null;
const cases=data.cases,current=()=>cases[caseIndex].sectors[sectorIndex];
const escape=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const actions={Talk:'현장 증언',Blade:'블레이드로 고정판 제거',Relay:'입자 수집과 수광부 레이저',Ambush:'경비 제압',Wire:'상부 와이어 접속',Throw:'물체를 와이어로 투척'};
for(const c of cases){const o=document.createElement('option');o.value=c.id;o.textContent=(c.floor?'하층 생활부':'최하층 생산구')+' / '+c.title;chapter.append(o)}
function showStop(s){selected=s.id;detail.innerHTML='<p class="muted">'+(s.optional?'선택 현장 · 진행을 막지 않음':'필수 현장 · 해결 후 통제선 개방')+'</p><h2>'+escape(s.title)+'</h2><p>'+escape(actions[s.action])+'</p><p class="muted">'+escape(s.speaker)+' · X '+s.x.toFixed(1)+'</p>'+s.lines.map((line,i)=>'<div class="line"><b>'+escape(s.voices[i])+'</b>'+escape(line)+'</div>').join('');draw()}
function load(){
 sectors.replaceChildren();for(let i=0;i<3;i++){const b=document.createElement('button');b.textContent=cases[caseIndex].sectors[i].name;b.setAttribute('aria-pressed',String(i===sectorIndex));b.onclick=()=>{sectorIndex=i;load()};sectors.append(b)}
 const route=current();centre=route.left+route.width/2;span=route.width+20;selected=-1;
 const stops=document.querySelector('#stops');stops.replaceChildren();for(const stop of route.stops){const b=document.createElement('button');b.textContent=(stop.optional?'선택 · ':'')+stop.title;b.onclick=()=>{centre=stop.x;span=65;showStop(stop)};stops.append(b)}
 detail.innerHTML='<h2>'+escape(route.name)+'</h2><p>'+escape(cases[caseIndex].title)+'</p><p>'+(route.jump?'점프 구간 · 완만한 높이 변화와 중간 착지대':'구조물 이동 구간 · 단차와 하부 통로 / 상부 지름길')+'</p><p>208 단위 블록 4개, 합계 '+route.width+' 단위입니다. 첫 두 현장은 필수이며, 세 번째는 선택입니다. 마지막 블록에서 사건의 주요 단서를 확인합니다.</p><p class="muted">현장 버튼을 선택하면 위치와 대사를 함께 볼 수 있습니다. 가로축과 세로축은 서로 다른 축척입니다.</p>';draw()
}
function draw(){
 const box=canvas.getBoundingClientRect(),dpr=window.devicePixelRatio||1;canvas.width=Math.round(box.width*dpr);canvas.height=Math.round(box.height*dpr);ctx.setTransform(dpr,0,0,dpr,0,0);
 const w=box.width,h=box.height,pad=32,route=current(),scale=(w-2*pad)/span,ys=(h-100)/23;
 const X=x=>w/2+(x-centre)*scale,Y=y=>h-65-y*ys;ctx.clearRect(0,0,w,h);ctx.font='12px system-ui';
 for(const c of route.ceilings){ctx.fillStyle='#193547';ctx.fillRect(X(c.x),Y(20),(c.right-c.x)*scale,Y(c.y)-Y(20));ctx.strokeStyle='#43b3c7';ctx.beginPath();ctx.moveTo(X(c.x),Y(c.y));ctx.lineTo(X(c.right),Y(c.y));ctx.stroke()}
 ctx.strokeStyle='#1a3040';for(let y=-2;y<=18;y+=2){ctx.beginPath();ctx.moveTo(0,Y(y));ctx.lineTo(w,Y(y));ctx.stroke();ctx.fillStyle='#628395';ctx.fillText(String(y),5,Y(y)-4)}
 for(let block=0;block<4;block++){const x=route.left+block*208;ctx.fillStyle=block%2?'#ffffff04':'#ffffff09';ctx.fillRect(X(x),Y(18),208*scale,Y(-3)-Y(18));ctx.fillStyle='#88a9bc';ctx.fillText('블록 '+(block+1),X(x)+8,28)}
 for(const p of route.platforms){ctx.fillStyle=p.depth<.5?'#678c9e':'#2d5367';ctx.fillRect(X(p.x),Y(p.top),(p.right-p.x)*scale,p.depth*ys);ctx.fillStyle='#6ec6cf';ctx.fillRect(X(p.x),Y(p.top),(p.right-p.x)*scale,2)}
 for(const rest of route.rests){ctx.fillStyle="#6ef2a9";ctx.fillRect(X(rest.x)-3,Y(.6),6,8)}
 for(const hook of route.hooks){ctx.strokeStyle="#5de9ff";ctx.lineWidth=2;ctx.beginPath();ctx.arc(X(hook.x),Y(hook.y),4,0,Math.PI*2);ctx.stroke();ctx.lineWidth=1}
 for(const s of route.stops){
   const color=s.optional?'#76b5fb':'#f7bc62',x=X(s.x),y=Y(s.y);
   if(!s.optional){ctx.strokeStyle='#f7bc6266';ctx.setLineDash([5,5]);ctx.beginPath();ctx.moveTo(X(s.gateX),Y(0));ctx.lineTo(X(s.gateX),Y(10));ctx.stroke();ctx.setLineDash([])}
   ctx.fillStyle='#dbe9ee';ctx.fillRect(X(s.actorX)-3,Y(s.feet+1.9),6,1.9*ys);ctx.fillStyle=color;ctx.beginPath();ctx.arc(x,y,s.id===selected?7:5,0,Math.PI*2);ctx.fill();
   if(span<160&&x>=0&&x<=w){ctx.fillStyle=color;ctx.fillText(s.title,Math.min(w-180,Math.max(16,x-15)),y-18);ctx.fillStyle='#aac2cf';ctx.fillText(s.speaker,Math.max(16,X(s.actorX)-35),Y(s.feet+2.35))}
 }
 const objective=route.objective;ctx.fillStyle='#57d7b2';ctx.fillRect(X(objective.x)-5,Y(objective.y)-5,10,10);
 if(X(objective.x)>5&&X(objective.x)<w-5){ctx.fillText('주요 단서',Math.min(w-70,X(objective.x)-20),Y(objective.y)-20)}
 ctx.fillStyle='#8eacbd';ctx.fillText('X '+(centre-span/2).toFixed(0)+' → '+(centre+span/2).toFixed(0)+' / 세로 Y -3~20',16,h-16);
}
chapter.onchange=()=>{caseIndex=Number(chapter.value);sectorIndex=0;load()};document.querySelector('#fit').onclick=()=>{centre=current().left+current().width/2;span=current().width+20;draw()};
function blockStep(n){let b=Math.floor((centre-current().left)/208);b=Math.max(0,Math.min(3,b+n));centre=current().left+b*208+104;span=224;draw()}
document.querySelector('#previous').onclick=()=>blockStep(-1);document.querySelector('#next').onclick=()=>blockStep(1);
canvas.onpointerdown=e=>{drag={x:e.clientX,centre};canvas.setPointerCapture(e.pointerId)};canvas.onpointermove=e=>{if(drag){centre=drag.centre-(e.clientX-drag.x)*span/(canvas.clientWidth-64);draw()}};canvas.onpointerup=()=>drag=null;
canvas.addEventListener('wheel',e=>{e.preventDefault();span=Math.max(35,Math.min(950,span*Math.exp(e.deltaY*.001)));draw()},{passive:false});
canvas.onkeydown=e=>{if(e.key==='ArrowRight'||e.key==='ArrowLeft'){e.preventDefault();centre+=(e.key==='ArrowRight'?1:-1)*span*.15;draw()}};
new ResizeObserver(draw).observe(canvas);load();
</script></html>'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', type=Path, help='LowerRouteExport JSON')
    parser.add_argument('--output', type=Path, default=Path('lower-route-preview.html'))
    args = parser.parse_args()
    data = json.loads(args.input.read_text(encoding='utf-8'))
    if len(data['cases']) != 8 or sum(len(s['stops']) for c in data['cases'] for s in c['sectors']) != 72:
        parser.error('Expected the eight lower cases and 72 authored stops.')
    payload = json.dumps(data, ensure_ascii=False).replace('<', '\\u003c')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(HTML.replace('__DATA__', payload), encoding='utf-8')
    print(args.output.resolve())


if __name__ == '__main__':
    main()
