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
