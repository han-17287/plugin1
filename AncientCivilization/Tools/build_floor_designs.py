from pathlib import Path
import json,copy,argparse
parser=argparse.ArgumentParser();parser.add_argument('export',type=Path);args=parser.parse_args()
root=Path(__file__).resolve().parents[1];base=json.loads((root/'Design/TraversalDesigns.json').read_text());export=json.loads(args.export.read_text())
configs=[
('L',1,'생활부','도둑이 지나간 길을 따라 골목·진료소·폐차장·감시 외벽을 지나 조직의 격납고에 도착한다. 차량 추격은 넣지 않는다.','탐조등의 지속 노출과 움직이는 골목 앵커',['골목 감시선','폐차 지붕 사잇길','가려진 운송 도관','창고의 원격 잠금','폐차장 위험 회수','진료소 예비 전력','옥상 반사등','불법 배전선','꺼진 골목등','진료소 귀환문','경비 수색선','숨겨진 응급 통로'],['주민 대피','기록 단서','차량 투척 연습','감시 돌파','격납고 진입'],['L09 안전 점등 시연','L01 짧은 감시선','L03 폐차 우회','L01 보통 감시선','L11 수색 압박'],['L02 폐차 지붕','L05 회수 도전','L12 응급 통로'],'본편 탐조등에 걸렸을 때의 로봇 소환은 기존 시스템과 후속 통합 대상이다. 새 시험 방은 지속 노출 시 재시도로 감시 규칙만 시험한다.'),
('A',2,'행정구','수송 기록과 감사를 대조해 코어 반출이 단순 절도가 아니었음을 확인한다. 위층 명령의 흔적을 모으되 최상층의 목적을 확정하지 않는다.','열림 예고가 있는 보안 셔터와 대기 발판',['접수 보안 셔터','폐기 기록 틈','꺾인 감사 게이트','서고 원격 스위치','봉인함 회수','수광부 전력 예산','반사 검증실','인증선 절단 순서','소등된 원본고','증인 귀환문','자동 폐기 압박','숨은 감사 사본실'],['접수','차량 기록','감사 차단','원본 대조','증인 확보'],['A09 원본고 시연','A01 셔터 대기','A03 상부 우회','A06 전력 계산','A11 폐기 압박'],['A02 폐기 기록 틈','A07 검증실','A12 감사 사본실'],'셔터가 플레이어와 겹치면 닫힘을 유예한다. 벽을 뚫고 돌진하는 해법은 허용하지 않으며 천장 경로는 열어 둔다.'),
('R',3,'상층 거주구','정원과 거주 심사 기록에서 하층의 배정과 실종 기록이 연결됨을 확인한다. 승진 권한은 엘리베이터 출입을 열 뿐 자동 이동을 만들지 않는다.','보호 발판의 상승 환기류와 세로 이동 앵커',['정원 환기구','외벽 바람 회랑','가려진 공용 도관','전시실 원격 잠금','외벽 블레이드 회수','공용 전력 분배','온실 반사경','계약선 절단','꺼진 조경등','정비원 귀환문','풍압 안전 점검','주소 없는 온실'],['조경 입구','공용 설비','계약 기록','주소 검문','샛길 접근'],['R09 조경등 시연','R01 환기류 상승','R03 공용 도관','R06 전력 분배','R01 상부 회랑'],['R02 바람 회랑','R07 반사경','R12 주소 없는 온실'],'바람은 일반 이동의 수직 속도에만 적용한다. 갈고리 돌진·일반 대시·피격·사망 상태를 휘게 하거나 덮어쓰지 않는다.'),
('T',4,'최상층','승진이 제한된 최상층은 샛길로 불법 진입한다. 조커 가면 저격수와는 같은 처지만 확인한다. 친구라는 사실은 공개하지 않는다. 저격수 뒤 비서실장1페이즈·생존 퇴장, 빈 회장실·노트·마지막 추격 순서를 유지한다.','일정 시간 활성화되는 앵커와 일회용 연속선',['직할 시간 앵커','미승인 우회선','삭제 도관','회장실 원격 봉인','조준선 속 회수','회수 장치 전력','감시 반사경','직할 인증선','꺼진 승인등','정비자 샛문','회수 출발 압박','숨은 직할 기록실'],['불법 진입','감시 우회','삭제 기록','저격수 전방','비서실장 전방'],['T09 승인등 시연','T01 시간 앵커','T03 삭제 도관','T01 전투 전 연속선','T11 짧은 출발 압박'],['T02 미승인 우회선','T05 회수 도전','T12 직할 기록실'],'전투 자체와 마지막 트럭 추격은 리듬 비율에서 제외한다. 고대 문명은2회차이므로 새 플레이 구역을 만들지 않는다.')]
rooms=[];floors=[]
for prefix,floor,name,story,gimmick,titles,roles,required,optional,note in configs:
 info=copy.deepcopy(export['floors'][floor]);info.update(story=story,gimmick=gimmick,required=required,optional=optional,note=note);floors.append(info)
 for index,r in enumerate(base):
  r=copy.deepcopy(r);r['baseId']=r['id'];r['id']=prefix+f'{index+1:02}';r['floor']=floor;r['title']=titles[index]
  r['purpose']=name+'의 '+roles[min(index//3,4)]+'에서 '+r['purpose']
  if index<2:
   proto=next(p for p in export['prototypes'] if p['floor']==floor and p['precision'] and p['tier']==('Normal' if index==0 else 'Hard'))
   for key in ['width','height','points','blocks','hazards']:r[key]=proto[key]
   r['verification']='actual floor prototype data; native unmeasured';r['dependency']=['층별 시험 방 구현','본편 무기·보상·보스 통합은 후속 단계']
   r['notes']=gimmick+'. '+note
   r['solutions']=[{'1':'탐조등이 움직이는 틈을 보고 표시 앵커를 이어 빠르게 통과한다.','2':'셔터 열림을 대기 발판에서 확인하고 표시 앵커로 통과한다.','3':'보호 발판의 환기류로 올라가 표시 앵커를 이어 탄다.','4':'다음 시간 앵커가 켜지는 것을 보고 일회용·이동 앵커를 연속 연결한다.'}[str(floor)],'닫힌 천장 아래의 고정 표면을 연속 견인해 우회한 뒤 출구로 내려온다.']
  else:
   r['verification']='floor design adaptation; dependent mechanics/runtime pending';r['dependency'].append('층별 개별 Room 구현 대기')
   r['notes']=gimmick+'와 조합하는 설계안. 좌표는 '+r['baseId']+'의 검토용 기본 도면이며 실제 층별 Room이 아니다. '+note
  rooms.append(r)
(root/'Design/FloorTraversalDesigns.json').write_text(json.dumps(dict(floors=floors,rooms=rooms),ensure_ascii=False,indent=2)+'\n')
def grid(r):
 g=[[' ']*r['width'] for _ in range(15)]
 for x in range(r['width']):g[14][x]='#'
 for a,b,low,high in r['blocks']:
  for y in range(int(low),int(high)+1):
   for x in range(int(a),int(b)+1):g[y][x]='#'
 for a,b,y in r['hazards']:
  for x in range(int(a),int(b)+1):g[int(y)][x]='^'
 for x,y,c in r['points']:g[int(y)][int(x)]=c
 return '\n'.join(''.join(row).rstrip() for row in g[::-1])
text='''# 다른 네 층의 흐름과 난관 — v0.30.0

생산구에 이어 생활부·행정구·상층 거주구·최상층을 확장한다. 단위는 **1타일=1유닛**이며, 리듬은 각 층의 다섯 구간마다 흐름75%·필수 난관25%다. 호흡 공간과 선택 분기·보스·추격은 비율에 넣지 않는다. 각 구간의 필수 난관은 하나다.

실제 추가는 각 층의 안전 방과 난관 방, 총8개 Room Prefab이다. 기존 생산구 두 방을 합쳐 **10개 Room×세 난도=30개 조합**을 같은 시험 씬에서 선택한다. 새 본편 전체 맵을 교체하지 않았다. 나머지 설계 방과 무기·보상은 구현 대기다.

`Assets/Scenes/TraversalRhythmPractice.unity`를 Play하고 화면의 층 버튼을 누른다. [지형 검토 HTML](TraversalPreview.html)에서도 층·방·난도를 고를 수 있다. [기본 M1 허용치·생산구12설계](M35_RHYTHM_KO.md), [기본 앵커/Room 구성](P2_TRAVERSAL_KO.md)을 함께 본다. M1은 계산 기준이며 실제 조작 성공률·5초 재시도·FPS는 미측정이다.

'''
for info in floors:
 f=info['id'];name=info['name'];prefix=configs[f-1][0]
 text+=f"## {name}\n\n{info['story']}\n\n고유 기믹: **{info['gimmick']}**. {info['note']}\n\n"
 text+='| 구간 | 흐름 두 묶음 | 필수 난관 | 호흡 | 선택 입구 | 체크포인트 |\n| --- | --- | --- | --- | --- | --- |\n'
 for i,s in enumerate(info['stages']):
  flow,challenge,rest=s['flow'],s['challenge'],s['rest'];half=flow//2
  text+=f"| {s['name']} | 0~{half}, {half}~{flow} ({flow}타일) | {flow}~{flow+challenge}: {info['required'][i]} | {flow+challenge}~{flow+challenge+rest} | x{half}, {info['optional'][i%3]} | 시작x3, 난관 전x{flow-4}, 보급x{flow+challenge+4} |\n"
 flow=sum(s['flow'] for s in info['stages']);challenge=sum(s['challenge'] for s in info['stages']);rest=sum(s['rest'] for s in info['stages']);text+=f'\n필수 흐름{flow}·난관{challenge}·호흡{rest}타일. 흐름/난관75/25이며, 이 길이를 실제 플레이 시간으로 환산하지 않는다.\n\n'
 text+='### 먼저 시험하는 두 배치\n\n안전 방은 아래와 같은 앵커선을 사용하면서 바닥을 전부 채운다. 난관 방만 위험 구간을 열고, 같은 조작을 연속 요구한다. `A` 고정, `a` 일회용, `M` 이동, `C` 체크포인트, `E` 출구, `^` 위험, `#` 지형이다. 위부터y14, 왼쪽부터x0, 문자1개=1타일이다. 소수 좌표의 발판은 포함된 타일에 표시하며 정확한 높이는 수치와 HTML을 따른다. 셔터·탐조등·바람·시간 상태는 ASCII 위에 겹치는 동적 기믹이며 검토 HTML에서 별도 표시한다.\n\n'
 for r in [r for r in rooms if r['floor']==f][:2]:
  text+=f"**{r['id']} {r['title']}** / {'보통' if r['id'].endswith('01') else '어려움·선택'} / 폭{r['width']}타일\n\n```text\n{grid(r)}\n```\n\n해법1: {r['solutions'][0]} 해법2: {r['solutions'][1]}\n\n체크포인트(3,1.1)에서 즉시 재시도하고 앵커·기믹 위상을 복원한다. 안전 방은 실패 즉사 없이 먼저 보여 준다. 본편의 희귀 보상/지름길은 아직 지급하지 않는다.\n\n"
 text+='### 12개 난관의 층별 번안\n\n| ID·이름 | 기본 유형·도면 | 이번 상태 |\n| --- | --- | --- |\n'
 for r in [r for r in rooms if r['floor']==f]:text+=f"| {r['id']} {r['title']} | {r['baseId']} {r['kind']} | {'실제 시험 방·데이터 변형' if r['id'].endswith(('01','02')) else '설계. P3~P6 또는 개별 Room 대기'} |\n"
 text+='\n기본 유형의 두 해법·실패 비용·자원 복구는 생산구 설계의 같은 C번호를 따른다. 층별 실제 지형으로 옮길 때 표의 도면을 그대로 완성 방으로 간주하지 않고, 시야·필요 능력·사거리·복구 상태를 다시 검사한다.\n\n'
text+='''## 구현 구성·인스펙터

| 파일/에셋 | 위치·컴포넌트 | 값/역할 |
| --- | --- | --- |
| `FloorTraversalLayout` | 순수 C# 데이터 |5층×5구간 리듬, 층별 앵커 높이·좌표, 시간/겹침 규칙 |
| `FloorTraversalProfile` | ScriptableObject | Production/Living/Administration/Residence/Top.asset. 바닥색·위험색·기믹 수치 |
| `TraversalFloorMechanism` | 시험 방 생성 시 기믹 오브젝트에 부착 | 같은 방의 Clock Owner=PlayerInputReader, Profile=층 데이터, Dangerous=난관 여부 |
| `LivingFlowRoom/ChallengeRoom` | Transform+TraversalRoom | Floor Profile=Living |
| `AdministrationFlowRoom/ChallengeRoom` | Transform+TraversalRoom | Floor Profile=Administration |
| `ResidenceFlowRoom/ChallengeRoom` | Transform+TraversalRoom | Floor Profile=Residence |
| `TopFlowRoom/ChallengeRoom` | Transform+TraversalRoom | Floor Profile=Top |

구조물·광선 폭·앵커 이동 데이터 변경 후에는 방/난도를 다시 선택해 재생성한다. 모든 Prefab과 데이터는 `Assets/Resources/Traversal`에 있다. 스케일1·회전0을 유지하고, 데이터로 생성한다. 물리는 Collider2D/Rigidbody2D/Physics2D만 사용한다. 탐조등·바람 표시는 SpriteRenderer 시험 표현이며 URP Light2D 최종 조명이 아니다.

### 생활부

탐조등 너비1.4타일·높이8타일, 수평 진폭±2.5타일, 주기4초다. 캡슐과 광선이 계속 겹치면0.7초 뒤 난관을 재시도한다. 그 전에는 주황색으로 경고한다. 안전 방에서는 경고만 표시한다. 실제 갈고리 돌진 중에는 이 시험 위험의 노출이 누적되지 않는다. 본편의 엄폐·시야 차단·로봇 소환과 공격은 기존/후속 전투 시스템의 연결 대상이다.

### 행정구

셔터는 폭0.75타일·높이8타일이다. 주기3초 중1.4초 열리고, 닫히기 전0.35초는 주황색이다. 직전에는 폭5타일·높이2타일의 안전 대기 발판이 있다. 플레이어 캡슐이 겹치면 닫힘을 유예한다. 닫힌 천장은y14라 상부 우회가 가능하다. 닫힌 셔터를 관통시키지 않는다.

### 상층 거주구

환기류는 폭3타일·유효 높이 약9.4타일, 보호 발판은 폭3타일·높이1.6타일이다. 일반 이동에 상승 속도12타일/초를 요청하고, 플레이어 이동 소유자가 물리 틱 끝에서 적용한다. 일반 대시·갈고리 돌진·피격·사망에는 적용하지 않는다. 움직이는 앵커는 수직 왕복으로 바뀐다. 공중에서 천장 경로를 택하면 바람의 타이밍을 생략할 수 있다.

### 최상층

일부 이동 앵커는3.6초 주기 중2.4초 활성화된다. 흐린 표식은 비활성 상태라 선택되지 않는다. 고정 천장/고정 앵커는 계속 사용 가능하다. 붙잡힌 앵커의 시간은 잠시 멈춰 비행 도중 표적이 꺼지지 않는다. 체크포인트는 일회용 소모·이동 위상·시간 위상을 모두 복원한다. 시간 앵커는 비서실장 보스의 구현이나2페이즈를 뜻하지 않는다.

## 난이도와 공정성

- 각 층의 쉬움/보통/어려움은 중간 앵커3/4/5개, 간격6/7/8타일, 진폭0.5/1/1.5타일이다. 조준 보정1.5타일은 유지한다.
- 모든 층/난도의 이동 극값과 최종 고정 앵커까지 사거리14 중2타일 이상의 여유, 캡슐 천장 여유를 검사한다. 위아래 이동은 점프가 아니라 갈고리 연결로 검사한다.
- 시험 방 카메라 중심y7·세로16타일로, 바닥과 닫힌 천장y14를 함께 보여 준다. 다음 목표의 수평 시야는4:3 이상 PC 비율 기준으로 검사한다.
- 필수 난관 앞 체크포인트, 각 방의 두 해법, 버튼/추락 재시도, 일회용과 기믹 위상 초기화를 유지한다.
- 고대 문명은2회차 구역이므로 포함하지 않는다. 생활부를 차량 추격으로 바꾸지 않는다. 최상층 저격수의 친구 정체도 공개하지 않는다.

## 테스트 체크리스트

- [ ] 시험 씬에서5층 버튼과 안전/난관, 세 난도가 모두 바뀌는지 확인한다.
- [ ] 각 층의 쉬움 안전 방에서 새 기믹을 먼저 경험한다.
- [ ] 생활부: 광선의 폭과 탐지가 일치하고,0.7초 전에 벗어나면 경보가 해제되는지 확인한다.
- [ ] 행정구: 열림/경고/닫힘과 대기 발판, 몸 안에서 닫히지 않는 처리를 확인한다.
- [ ] 거주구: 보호 발판과 수직 앵커를 확인하고, 돌진 중에는 바람이 직선을 휘지 않는지 확인한다.
- [ ] 최상층: 비활성 앵커는 선택되지 않고, 예약 중에는 사라지지 않는지 확인한다.
- [ ] 각 층에서 앵커선과 천장 우회선을 모두 통과한다.
- [ ] Esc/Alt+Tab에서 기믹 시간과 노출이 정지하고 재시도/층 전환에서 복원되는지 확인한다.
- [ ] 실제30/60/144fps에서 조작감·모서리·성공률·5초 재도전을 측정한다.

## 흔한 버그

- 색만 바뀌고 기믹이 없음: 새 층의 Room Prefab과 Floor Profile 참조, Build 호출을 확인한다.
- 시간 앵커가 꺼져도 잡힘: SetOperational과 Available 검사를 우회하지 않는다.
- 셔터가 몸을 밀어냄: 닫기 전 ShutterOverlap 유예를 유지한다. Player 캡슐 크기를 바꾸면 여유도 갱신한다.
- 바람에 돌진이 휨: 외력을 Rigidbody에 직접 계속 더하지 말고 상태를 확인하는 ApplyEnvironmentalLift를 사용한다.
- 이전 층 광선이 남음: 방 전환 때 기존 방을 즉시 비활성화하고 예약을 먼저 해제한다.

자동 검사는 수학·데이터 계약·실제 기믹 스크립트의 제한된 서비스 더블 검사다. 이 환경에서는 Unity Editor의 실물 물리·조명·입력·FPS·완주를 실행하지 못했다.
'''
(root/'OTHER_FLOORS_RHYTHM_KO.md').write_text(text)
print('PASS: four floor specifications,20 section rhythms,48 adapted designs and eight prototype diagrams authored')
