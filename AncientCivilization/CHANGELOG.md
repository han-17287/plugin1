# v0.13.0 — 조커 가면 저격수 4페이즈

- 저격수 외형을 모자에 반쯤 가린 조커 가면, 가벼운 복장, 긴 저격소총으로 교체. 6포즈 골반 피벗과 기존 다른 보스 피벗 보존.
- 2초 조준·5발 후 재장전, 멀리서 수류탄 연속 투척, 실제 지면 파괴/입자 생성/약 6초 복구.
- 2페이즈 푸른 에너지 분신 1명, 3페이즈 분신 총 3명과 압축 포탑 2개. 조종 분신 제압 후 E로 포탑 탈취, 좌클릭으로 압축 발사.
- 4페이즈 무적 충전, 포탑 2개 파괴와 입자 수집 후 자동 레이저 충돌. Space·좌클릭 각각의 새 누름으로 힘겨루기. 실패 시 파이널 수집 재도전, 성공 시 대치 종료.
- 비서실장 설정 확정: 1회차는 1페이즈만 싸운 뒤 살아서 퇴장. 대사 동안 공격·침 정리, 죽음 처리 없음. 2회차에 재등장.
- 저격수 역시 전투 승리 뒤 살아서 대화하며, 같은 처지만 확인한다. 과거 관계 공개는 2회차로 유지.
- 검사 결과는 VALIDATION.txt 기준. 실제 Unity Play/충돌/난도/FPS/빌드는 아직 미검증. 총 10시간 작업이나 9시간 플레이 완료를 뜻하지 않는 중간본.

# v0.12.1 — 현장 동선과 전투 안내 보완

- 20개 현장별 발판 데이터. 이동 현장 16곳의 높낮이/발판 폭/지름길 분리, 보호 현장 4곳의 연속 지면 유지. 기록·적·운송 벨트 좌표는 실제 발판 데이터와 함께 이동.
- 비서실장 일반 몸 접촉 보호 규칙 연결, 침의 비 예고 색 수정, 제압 전 노트 접근 안내 수정.
- 독립 규칙 3211개 통과. 지도 간격과 머리 위 여유는 데이터 검사이며 실제 Unity 이동 완주 검증을 대신하지 않음.

# v0.12.0 — 로고 도입과 시계 최종보스 (중간본)

- 메인 메뉴 앞 검은 화면: Hwasan Games → HG → 메뉴. 실행 중 한 번; Space/Esc 건너뛰기. 사용자 제공 시안을 참고한 정리 텍스처이며 첨부 원본 파일의 바이트 복사본은 아님.
- 7개 주요 사건에서 근거 두 개를 읽은 뒤 자료 대조 결론 선택. 오답은 진행 손실 없이 재확인.
- 구역 보스별 돌진/물리 투척/곡사/낙하물/도탄과 보스 6명 총 36포즈 추가.
- 저격수 이후 비서실장 1페이즈: 3초 힘 합성 창, 세 번째 세 방향, 순간이동 광역 3회와 바로 아래 안전지대, 파괴·입자화 가능한 침의 비.
- 비서실장 제압을 별도 저장. 재도전 때 저격수 재전투 없음. 2페이즈 미구현; 기존 추격 세이브에 재전투 강제 없음.
- 독립 규칙 2310개, 에셋/구문/스포일러 검사, 보조 타입 검사 통과. Unity 실기 미검증. 총 10시간 완료본 또는 9시간 플레이 완성본이 아닌 중간본.

# v0.11.1 — Unity 6000.6 실행 차단 API 수정

- GetInstanceID를 텍스처 참조 키로 대체. 더 이상 숫자 ID 형식에 의존하지 않음.
- 부팅 중복 검사 FindFirstObjectByType → FindAnyObjectByType.
- Esc 종료 입력이 같은 프레임에 다른 메뉴로 이어지지 않도록 준비 프레임 확인.
- 두 폐기 API의 재유입을 정적 검사에서 차단. 실제 Unity 6 실행은 미검증.

# v0.11.0 — 백명과 도시 수사 확장

- 백명/백 경위 호칭, 주민 25명과 상태별 대화·인물별 외형·실제 청취 대화 기록. 구조 대상 4명의 걷기 16프레임과 발 기준 정렬.
- 층당 4개의 주요 사건과 별도 현장 20개, 목표 60개와 확보 자료 60개.
- 기록/증언, 봉인 파괴, 주민 동행, 입자 레이저 복구. 현장별 지형·기믹·작업장/진료소/보관소/정원/관제실 배경.
- 새 게임의 조사→보스→권한 순서, 현장 중단·재도전·저장 재개, 파괴된 패널의 입자 재채취로 자원 소실 막힘 방지.
- 현장별 메시·조명·동적 기믹 생략, F10 CSV에 현장 ID 포함.
- **설정 변경:** 1회차 최상층은 저격수와 같은 처지만 확인. 친구 인지 대사/기록 삭제. 친구 공개는 2회차로 이동.
- 조커 복선은 다섯 주민 대화에 한 번씩 배치. 늦게 처음 만난 주민도 단서를 생략하지 않고 이후 대사로 전환.
- 회장/특수부대 트럭 탑승의 간략한 도트 동작. 고대 문명 플레이 구역 미제작.
- 실제 Unity Play/빌드 및 9시간 분량/90% 완성은 미검증·미달. COMPLETION_KO.md에 구현과 남은 범위를 공개.

이하 v0.10.0 이전 설명은 당시 설정과 작업 기록이다. 저격수 관계는 위의 최신 설정을 따른다.

# v0.10.0 — 프레임 처리 정리와 도시 하강 사건

- 층별 정적 장식 메시 묶기, 시야/층별 조명 생략과 빈 조명 슬롯 재사용.
- 투척물/타격 링/적 베기 궤적 풀, 와이어 길이별 재사용 좌표 버퍼와 삼각함수 점화식.
- HUD 문자열·영웅 스프라이트·투척물 이미지 캐시, 지면 검사 재사용, 효과음 미리 준비, 빗줄기 고정 경계.
- F8 60/120/144/240/무제한 목표, F9 프레임 시간/P95/GC/묶음/풀 표시, F10 최근 표본별 설정을 포함한 CSV.
- 보스 공격 후 금색 빈틈과 피해 배율, 피해 숫자·정밀 파괴·조준 완료 안내, 전투 구역 카메라.
- 보스 직전 저장/재도전, 이동 지형 위 강제 장소 이동과 탑승 정리, 트럭 경비 밀림 위치 수정.
- 높은 이동 경로 보급품 10개, Tab 순찰 기록, 서로 다른 선택 수사 3개와 중복 보상 방지.
- 회장실 계획/코어 안전 경고, 최상층→최하층 마지막 추격, 지붕 승차 후 옛 동료 지원 사격·코어 폭주·추락 종료.
- 도중 종료한 추격/연출은 조작 가능한 구간으로 재개. 고대 문명 맵은 생성하지 않음.
- 긴 노란 8프레임 블레이드·DNA 와이어·즉시 레이저·보관한 3D 시안 원본 유지. 레이저의 벽 뒤 끝점 피해 차단.
- 실제 Unity Play/빌드 및 기기 FPS는 미검증. 9시간은 완성판 목표이며 현재 분량이 아님.

# v0.9.1 — 무기 아트와 실제 표시 일치

- 긴 노란 칼날과 검은 심재, 원본 시트의 이중 나선 와이어 표시.
- 새 8프레임 베기, 휘두르는 순간의 피해/반동 판정, 짧은 다음 공격 입력 버퍼.
- 원본의 긴 칼끝을 보존하는 실루엣 분리와 발/크기 기준 정렬.
- 빔 레이어 강화와 발사 자세, 미사용 아군 입자탄 경로 및 레이저 입자 흩뿌림 제거.
- 고그래픽 블레이드/와이어 시안 원본을 ArtReferences에 별도 보관.
- 403개 순수 C# 검사 통과. Unity 실제 Play/빌드 미실행.

# v0.9.0 — 도시 다섯 구역 연결

- 고대 문명은 2회차로 유보하고 생성하지 않음. 도시 0~4층만 구성.
- 행정구 셔터, 거주구 상승류, 최상층 붕괴 발판과 층별 기본/숙련 경로.
- 구역 범죄자와 패턴 보스, 저격수 전투 후 옛 친구 확인 대화.
- 구역 해결 → 다음 층 권한 → 엘리베이터 선택 이동. 최상층은 거주구 외벽 샛길로만 침입.
- 기존 저장 이관, 현재 층 저장/복원, 보스 구역 경계와 전투 정리.
- 순수 규칙·정적 맵 검사 394개 통과. 검증용 API 이름 치환 복사본으로 Unity 2021 참조 타입 검사 통과. 실제 Unity 6 Play/빌드 미실행.

# v0.8.7 — 층별 와이어 경로

- 기존 하층과 상층 모두 이동 구간 구성. 하층에 공장 옥상 기본/숙련 경로 추가.
- 움직이는 크레인 앵커를 실제 견인·스윙 목표로 추적, 정/역방향 운송 벨트.
- 도로/오토바이 진행 공간 확보, 층별 기록 분리와 재도전.
- 미구현 층의 기믹은 FLOOR_DESIGN_KO.md에 설계 상태로 명시.
- 순수 규칙·정적 맵 검사 224개 통과. Unity Play/빌드는 미실행.

# v0.8.6 — 상층 정비 통로 점프맵

- 약 120유닛 구간에 넓은 기본 발판과 높은 숙련 지름길, 연속 천장 앵커 구성.
- 재사용 공중 반동 부유체 6개, 투척물 파괴/드론 타격 반동, 가로 운동량 유지.
- 피해 없는 추락 회수, 중간 회수 지점, 재도전과 완주 시간·최고 기록.
- 상층 카메라/천장 확장과 경계, 엘리베이터 왕복 유지.
- 규칙·정적 맵 검사 187개 통과. Unity Play/빌드는 미실행.

# v0.8.5 — 적 대상 와이어 충돌

- 좌클릭 적 조준, 명중 피해, 움직이는 적을 향한 견인과 선분 충돌 검사.
- 일반 적 밀림과 충돌 피해, 보스 피해·밀림 저항 및 패턴 유지.
- 해당 대상만 견인·충돌 접촉 피해 보호. 다른 적과 투사체·공격은 기존 판정 유지.
- 벽/사거리/대상 사망/취소 처리, 적 조준 색상, 지형 스윙 유지.
- 규칙 검사 118개 통과. Unity Play/빌드는 미실행.

# v0.8.4 — 봉인된 운송 기록의 복선

- 순찰 점검 예외, 공장 배정 표지, 수령처가 가려진 운송장, 별도 이송 승인, 엘리베이터 관제에 C-00 연결.
- 통신관은 기록 사본을 남기며 주인공과 함께 의문을 가짐. 초반에 흑막의 정체를 공개하지 않음.
- STORY_KO.md에 현재 구현 대사와 후반 회수 계획을 구분해 기록.

# v0.8.3 — 승진 권한과 엘리베이터 왕복

- 승진 후 자동 이동 없이 상층 출입 권한만 해금.
- 양쪽 층 터미널에서 E로 목적 층 선택, 현재 층/잠긴 층 비활성화, Esc 취소.
- 상승·하강 전환 안내, 최초 상층 도착 대화는 한 번만 표시.
- 하층 귀환 후에도 상층 출입 권한 유지. 도착 층 체크포인트 저장 및 복원.
- 두 구역 사이 직접 우회 차단, 층별 카메라 범위, 입력 차단과 오토바이 주차 처리.
- 규칙 검사 104개와 문법·에셋 검사 통과. Unity Play/빌드는 미실행.

# v0.8.2 — 이중 나선 입자 와이어와 전구 검

- 중앙 직선 코어와 반대 위상으로 회전하는 두 푸른 입자 나선. 기존 2D 카메라에 깊이가 있는 와이어를 표시.
- 넓고 뾰족한 노란 칼날·검은 심재·밝은 날·발광층으로 블레이드 변경.
- 성인 체형의 흰색/검은색 도트와 푸른빛 고대 기계팔. 얼굴을 가리고, 오토바이 본체 이미지는 유지.
- 우클릭만 흩어진 입자 수집. 좌클릭은 블레이드 또는 준비 후 레이저. intact 투사체의 우클릭 자동 캡처 제거.
- 한국어 한 글자씩 대화, Neo둥근모 글꼴과 라이선스 포함.
- 순찰소·정비소·작업장·코어 생산소·주거 골목·검문소로 이어지는 초반 맵과 연결 경사로.
- 순수 C# 회귀 검사 94개 통과. Unity Play/빌드 검증은 미실행.

# v0.8 — 순찰 퀘스트 / 노란 블레이드 / 조준 레이저 / 상층 진입
- Ctrl 달리기 복구, 대시와 Alt/C·R/V/Q 전투 바인딩 제거.
- 공격 모드 LMB 짧게: 노란 필라멘트 블레이드·물체 파괴·입자 수집. 유지: 조준 준비. 0.85초 후 놓기: 저장 입자 레이저.
- 준비 중 이른 해제·모드 전환·대화·포커스 상실은 취소. 지형이 조준선·레이저·블레이드를 차단.
- 재사용 가능한 실제 입자 조각과 단일 저장량 HUD, 충전량별 레이저 피해·폭·효과.
- 순찰·사슬·탑승·블레이드 훈련 선행 퀘스트, 추격 구간 일반 적 3명, 움직이는 코어 화물 트럭과 투척·레이저 억제.
- 코어 회수 후 체력 240 미니 보스, 검거와 절도 동기에 대한 의문, 시민 포인트 1,000점과 상층 허가.
- 승강기 E 이용 및 위 도시 연결 구역·체크포인트·진행 저장.
- 주인공 전 자세를 귀여운 도트 비율과 청록빛 유물 사슬 장치로 리디자인. 노란 블레이드와 3인 대화 초상화.
- 하단 대화창·타자식 표시·Space/Enter 진행·대화 중 게임 정지·종료 입력 보호.
- 별도 스토리 저장 키로 이전 v0.7 코어·보스·체크포인트 보존. 기존 업그레이드 유지.
- 순수 C# 전투·퀘스트 규칙 67개 검사 실행 통과. 리소스·구문 검사 통과. Unity 실행·빌드 검증은 미실행.

# v0.7 기반 — 주차 오토바이 탑승 / 코어 도난 스토리
- Ctrl 차량 소환·입자 비용·제작 해금 제거. 달리기를 R로 이동.
- 시작·체크포인트 근처에 차량 3대 배치. E로 지상 탑승 / 하차·주차.
- 같은 실행 중 차량별 위치·방향·체력 유지. 파괴 차량 재탑승 방지, 사망 시 원래 배치 복구.
- 기존 스키드·제동·자폭·차량 충돌·엔진음 유지. 소환 조립 연출 제거.
- 수직 계층 도시·포인트 거주권·아래층 치안경찰·위층 도둑의 아래층 생산 코어 절도 설정 정리.
- 도둑은 트럭을 훔친 것이 아니라 트럭을 이용해 에너지 코어를 훔친다. 시작 안내·메인 화면 반영.
- 움직이는 트럭 추격전은 이번 변경의 구현 범위가 아니며 STORY_KO.md에 후속 범위 명시.

# v0.7 기반 — 중장비 사슬팔 / 휠 전환 / 오토바이 스키드 / 렌더링 정리
- 팔·손 윤곽 없는 비대칭 사슬 윈치와 노출 스프로킷·장갑판·앵커 헤드로 캐릭터 전 자세 리디자인.
- 실제 케이블을 재사용 금속 링크 사슬로 교체하고 넓어진 장치의 이미지 분할·기준점 갱신.
- 휠 위: 공격 / 아래: 와이어 즉시 선택. 같은 방향 연속 휠 입력 시 모드 유지. Shift 전환 제거.
- 두 모드 공통 우클릭 캡처. 와이어 좌클릭 견인 / 가운데 클릭 스윙. 공격 좌클릭 저장 에너지 발사.
- 지상 오토바이 역방향 입력 시 관성 제동·차체 쏠림·기울기·바퀴 헛돎·타이어 자국·먼지 연출.
- 144FPS 목표 및 HUD 실측 FPS 표시. 실제 Unity 성능 측정은 미실행.
- 빗줄기 72개를 한 메시로 통합, 화면 밖 발광·숨겨진 효과·Light 후처리 감소.
- 투사체별 매 프레임 적 목록 배열 생성 제거, 조준 후보 시야 판정 축소, 선 좌표 일괄 업로드.

# v0.7 기반 — 손 모양 없는 발사 / 캡처 모듈
- 기계팔 끝의 주먹·손가락·손바닥·기계 장갑을 원통형 발사구로 교체.
- 집게·갈고리 없이 건메탈 하우징 / 티타늄 테두리 / 청록색 안쪽 발사구로 구성.
- 기본·이동·공격·와이어·흡수·점프·탑승 / 타이틀 전 자세 적용.
- 기존 기계 어깨·피스톤·팔꿈치·전완과 게임 조작·판정 유지.

# v0.7 기반 — 완전 기계식 로봇 팔
- 장식형 은백색·금색 의수를 어깨부터 손끝까지 기계인 로봇 팔로 재설계.
- 어깨 서보·유압 피스톤·팔꿈치 힌지·전완 기계 장치·손목 와이어 발사기·금속 관절 손가락.
- 건메탈·티타늄·검은 관절·청록색 상태등으로 재질 통일.
- 펼친 기계 손의 폭에 맞게 Hero 6/7 프레임 분할 경계와 피벗을 조정하고 투명 여백 검증 갱신.
- 기본·이동·공격·와이어·흡수·점프·탑승 / 타이틀 캐릭터 적용. 기존 조작과 전투 수치 유지.

# v0.7 기반 — 고대 기술 의수 리디자인
- Hero / Actions / Motorcycle 캐릭터 팔을 은백색 장갑판·금색 테두리·흑연색 관절·청록색 코어 디자인으로 교체.
- 기본·이동·전투·점프·흡수·탑승 및 메인 화면의 의수 디자인 통일.
- 기존 CharacterRim 셰이더에 청록색 코어·채널만 반응하는 충전 발광 / 동작 맥동 추가.
- 캐릭터·탑승자별 MaterialPropertyBlock으로 충전과 와이어·캡처·발사 상태 전달. 전투·이동 수치 유지.

# v0.7 기반 — 메인 화면 추가
- 도시 배경·캐릭터·입자 연출을 사용하는 반응형 타이틀 화면 추가.
- 시작 / 저장 이어하기 / 현재 세션 재개 / 새 게임 확인 / 조작 안내 / 종료 제공.
- K/Esc 업그레이드 메뉴에서 저장 후 메인 화면 복귀.
- 메뉴 중 게임 시간·물리·적·투사체 정지, 메뉴 클릭이 게임 조작으로 이어지지 않도록 입력 보호.
- 미시작 상태로 종료할 때 저장 덮어쓰기 방지, 기존 체크포인트·설정 보존.

# v0.7 기반 — 입자 와이어 / 실물 투사체 외형 개선
- 와이어: 48점 중심선·외피·이중 꼬임, 재사용 입자 32개, 손목 링과 육각 래치.
- 발사 잔물결·스윙 처짐, F6 Light에서 꼬임 생략 및 입자 16개.
- 적 투사체: 긴 레이저 궤적 대신 명암·테두리가 있는 금속탄 / 회전 원반 / 중량탄 / 금속 파편.
- 금속탄의 진행 방향 정렬, 원반·투척물 회전, 지형 충돌 시 금속색 파편.
- 적 탄의 속도·피해·중력·캡처와 플레이어 충전 발사는 기존 동작 유지.

# v0.7 기반 — 맵 전체 밀폐 천장으로 수정
- 분리된 천장 6개를 제거하고 맵 전체를 덮는 단일 연속 천장으로 교체.
- 아래면 높이 10, 가로 -40~160, 두께 32의 충돌 지형으로 위쪽을 빈틈없이 차단.
- 천장 아래면 전체 와이어 견인·스윙 및 적 6명 배치 유지.

# v0.7 기반 — 적 밀도 감소 / 천장 와이어
- 일반 적 11명 → 6명. 구간별 배치를 분산하고 캡처 가능한 원거리 적과 마지막 보스 유지.
- 지형 충돌이 있는 천장 6개와 아래면 연결 노드 추가. 천장 전체 아래면에서 직선 견인·스윙 가능.
- 천장 사이의 개방 구간, 기존 사거리·시야·자동 해제 규칙 유지.

# v0.7 기반 — 와이어 / 캡처 공격 조작 변경
- Shift: 와이어 / 공격 모드 토글, 근접 3연타는 V로 이동.
- 와이어 모드: 좌클릭 직선 견인, 우클릭 스윙. Tab 모드 선택 제거.
- 공격 모드: 우클릭 유지로 적 투사체 캡처, 좌클릭으로 누적 에너지를 조준 발사.
- 캡처 위력에 따라 에너지 누적, 최대 3단계 및 별도 100% 충전율. 3회 캡처로 풀충전되지 않음.
- 누적량에 따라 발사 피해·크기·궤적·피격 범위와 저장 고리 확대. HUD와 도움말 갱신.

# v0.7 — Particle Motorcycle

- Add modular chassis, animated wheels, an aligned rider and an alternate damaged chassis.
- Add phased assembly, suspension-style visual motion, headlight/exhaust effects and speed-driven engine audio.
- Add progressive acceleration, coasting, reverse braking and an S/Down brake.
- Add a mount-clearance check and a vehicle-sized collision box, restored to the hero capsule on dismount/death.
- Add low-health sparks, velocity-aware explosion launch, visual component debris and a bike health/speed HUD.
- Preserve the existing unlock, 30-particle construction cost and 80 health.

This is an arcade vehicle, not a wheel-physics simulator. Unity compilation, collisions, rider alignment, audio and handling remain unverified here.

# v0.6 — Aimed Wire Controls

- Bind left mouse to pointer-aimed wire, Shift to melee and Alt/C to dodge.
- Add Tab switching between automatic swing and direct zip-pull, preserving the mode preference.
- Limit snapping to the cursor neighbourhood and add direction-unrestricted fixed-surface targets in zip mode.
- Add a pixel reticle with target/cooldown feedback, mode-specific shape and a menu pointer.
- Restore the native cursor outside the Game view, on focus loss, disable and teardown.

Unity input handling, cursor behaviour, wire physics and rendering remain unverified here.

# v0.5 — Impact and Motion

- Add dedicated anticipation/contact, heavy-strike, jump and hurt sprites with stable actor pivots.
- Apply melee damage on contact timing and add a short next-attack input buffer.
- Add short global hit emphasis, impact rings, landing feedback and pooled afterimages/impact pixels.
- Add readable evade/absorb windup indicators, cooldown meters and the nearest-core locator.
- Add a consistent HUD/button theme and trailing health feedback.
- Separate camera shake from its follow position and align to actual display pixels.
- Refine synthesized cues and add quiet original ambience with an F7 mute preference.

Unity C#/shader compilation, rendering, input responsiveness and combat remain unverified here.

# v0.4 — Neon Sentinels

- Add a dedicated high-resolution collision-aligned rooftop texture, closer adaptive camera and 1080p defaults.
- Add additive local lights, character rim shading, subtle screen glow/grade, distant rain, haze and foreground cables.
- Add an F6 Enhanced/Light presentation option with supported-shader fallbacks.
- Replace shared enemy art with six distinct silhouettes and a dedicated thrown scrap sprite.
- Add telegraphed lancer lunges, gunner three-round bursts, ballistic scrap throws, flying drone fans and directional shield guards.
- Add guard breaks with combo finishers, particle bursts and motorcycle explosions; add vulnerable recovery windows.

Unity C#/shader compilation, rendered appearance and combat remain unverified here.

# v0.3 — Particle Wire

- Add an actual maximum-length DistanceJoint2D tether with automatic reeling and immediate forward/upward launch.
- Automatically release on the rising side of the arc; preserve aerial momentum and count distinct-anchor chains.
- Allow elevated fixed rooftop surfaces as well as preset anchors, with live target highlighting and line-of-sight checks.
- Add a bright cable core, translucent sheath, energy pulses, rotating hook and latch/release particles.
- Clean up the joint on dash, jump, explosion, death and scene teardown.

Unity import, compilation and physical movement remain unverified in this cloud environment.

# v0.2 — City of the Warden

- Replace placeholder pixel grids with shipped city, hero and world PNG artwork.
- Add animated hero poses, detailed repeating rooftops, parallax, trails, impact effects and synthesized cues.
- Add a three-hit combo, directional particle burst, invulnerable dash and buffered/coyote-time jumps.
- Add four upgrades with core gates and separate technology-fragment currency.
- Add persistent core/upgrades/checkpoint progress, healing beacons and guarded save reset.
- Add a telegraphed, two-phase Sanctum Warden encounter and final gate sequence.
- Preserve full-speed absorption and immediate automatic wire acceleration.

This is source-only. Unity compilation, Play Mode and builds remain unverified.
