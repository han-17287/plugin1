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
