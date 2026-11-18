# P2-A와 M3.5 시험 방 — 기본 구성

> v0.31 안내: 현재 실행 씬은 [건축형 지역 맵](DISTRICT_MAPS_KO.md)으로 교체됐습니다. 아래의 단순 방 도면·난도별 간격은 기초 시험 코드와 후속 기믹 설계 자료입니다. 현재 실제 지형은 [지역 배치도](DistrictMapPreview.html)를 확인하세요.

v0.30에서는 [다른 네 층](OTHER_FLOORS_RHYTHM_KO.md)의 고유 기믹과8개 Room을 같은 시험 씬에 추가했다. 아래는 생산구에서 시작한 공통 갈고리와 두 가지 Room 유형의 설명이다.

`Assets/Scenes/TraversalRhythmPractice.unity`를 열고 Play한다. 현재는 생산구의 위험 설비가 활성화된 지역 맵이 나오며, 오른쪽 위 도움말에서 안전 바닥과 장치 난도를 바꿀 수 있다. 기존 Main 씬은 아직 기존 스토리 컨트롤러를 사용한다. 시험 씬은 기존 저장·재화·메인 보스를 변경하지 않는다.

[12개 난관·리듬 표·M1 잠정 허용치·복구 규칙](M35_RHYTHM_KO.md) / [타일 배치 검토 화면](TraversalPreview.html)

## 이번 구현 범위

- 고정·일회용·움직이는 앵커 세 종류와 일반 벽/천장 직선 견인.
- 마우스 월드 조준, 근접 앵커 보정, 후보 강조, 사거리, 직선 중심과 두 나선 LineRenderer.
- 고정 속도·고정 방향 돌진, CircleCast와 실제 캡슐 Cast 사전 검사, 이동 중 위험 레이어 무적.
- 도착 후 관성, 공중 점프1회·대시 준비 회복, 0.3초 Zip Strike 준비 값. 실제 블레이드 추가 타격은 P4 범위다.
- 안전 바닥의 Flow Room과 짧은 Gauntlet Room, 세 난도 ScriptableObject, 체크포인트 재시도·앵커 복구.

예측·체인 자동 연결·반사·에너지 앵커, 보스 약점, 저스트 회피와 무기·입자·레이저는 후속 단계다. 연속 클릭으로 이어 가는 것과 한 번 입력으로 자동 체인 돌진하는 것은 구분한다. 이번에는 전자만 구현했다. M3.5의 나머지 설계는 필요한 기믹이 완성된 뒤 제작한다.

## 코드·붙일 GameObject·컴포넌트

| 스크립트 | 위치·필수 구성 | 인스펙터 |
| --- | --- | --- |
| `GrappleRules` | 일반 C# 수학 규칙. 붙이지 않음 | 거리·고정 스텝·추후 Zip Strike 배율 |
| `GrappleData` | ScriptableObject | `Resources/Traversal/DefaultGrapple.asset` |
| `GrappleAnchorData` | ScriptableObject | DefaultAnchor, OneUseAnchorData. 반경·색·움직임 수치 |
| `GrappleAnchor` | 추상 베이스. 직접 붙이지 않음 | 레지스트리·사용 예약·소모·강조·복구 |
| `FixedGrappleAnchor` | FixedAnchor Prefab 루트 | Data=DefaultAnchor |
| `OneUseGrappleAnchor` | OneUseAnchor Prefab 루트 | Data=OneUseAnchorData |
| `MovingGrappleAnchor` | MovingAnchor Prefab 루트 | Data와 Clock Owner=플레이어 PlayerInputReader |
| `PlayerGrapple` | P1 Player 루트. PlayerStateMachine·입력·모드·Rigidbody2D·캡슐 필요 | Data=DefaultGrapple. 연습 씬이 자동 구성 |
| `TraversalDifficultyData` | ScriptableObject | Easy/Normal/Hard |
| `TraversalLayout` | 순수 저작 좌표·리듬 데이터. 붙이지 않음 | 검토·자동 검사·시험 방이 공유 |
| `TraversalRoom` | SafeFlowRoom/GauntletRoom Prefab 루트 | Precision, Difficulty, Clock Owner. Build가 같은 타일 크기로 지형 생성 |
| `TraversalPracticeHazard` | 생성된 가시 GameObject | Trigger BoxCollider2D, Layer10 TraversalHazard |
| `TraversalRhythmPractice` | 전용 씬 루트 | 시험 방·카메라·플레이어·재시도 관리 |
| `Editor/GrappleRoomSmokeChecks` | Editor 메뉴. 붙이지 않음 | 안전 흐름 방 Play에서 PLUGIN/P2/안전 흐름 방 앵커 실물 검사 |

모든 물리 판정은 Rigidbody2D/Collider2D/Physics2D다. 앵커 자체는 콜라이더가 없어도 된다. 등록된 중심점을 조준 후보로 쓰고, 벽/천장은 Layer8 GroundSolid로 탐색한다. 가시는 Layer10 Trigger이므로 돌진을 물리적으로 막지 않는다. 태그는 필요 없다.

## Prefab 구성

`Assets/Resources/Traversal/`에 다음 다섯 실제 Prefab 에셋이 있다.

```text
FixedAnchor           OneUseAnchor          MovingAnchor
  Transform             Transform             Transform
  FixedGrappleAnchor     OneUseGrappleAnchor     MovingGrappleAnchor

SafeFlowRoom          GauntletRoom
  Transform             Transform
  TraversalRoom          TraversalRoom
    Precision=false       Precision=true
    Difficulty=Normal     Difficulty=Normal
```

앵커는 실행 때 LineRenderer 원형 표식을 만든다. 고정은 파랑, 일회용은 노랑, 이동은 연두다. 선택 후보는 흰색으로 굵어진다. 룸 생성기는 앵커 Prefab을 복제하고 각 난도의 이동용 데이터를 따로 복제하므로 다른 방의 값을 덮어쓰지 않는다. Runtime 색/형상은 시험용이며 최종 도트 아트가 아니다.

Room Prefab은 스케일1, 회전0으로 사용한다. 크기를 Transform Scale로 바꾸면1타일 기준과 플레이어 허용치가 어긋난다. `Build(profile, inputReader)`로 생성하며, 시험 씬에서는 기존 방을 비활성화·제거하고 새 프로필로 다시 생성한다. 현재 세 난도는 Unity Prefab Variant 파일 세 개가 아니라 **같은 Room Prefab+세 데이터 에셋**이다.

다른 씬에서는 룸을 복제한 뒤 Build를 한 번 호출하고, 룸 로컬 Checkpoint를 월드 좌표로 변환해 플레이어를 배치한다. 런타임 도중 같은 인스턴스를 Build로 두 번 재구성하지 않는다.

## 기본 인스펙터 값

| 값 | 기본값 |
| --- | --- |
| Range / Speed | 14타일 / 72타일·초 |
| Launch Delay / Arrival Tolerance / Skin | 0.02초 / 0.06타일 / 0.03타일 |
| Retained Velocity | 0.22. 최대15.84타일·초 |
| 도착 관성 입력 유지 | PlayerStateMachine에서0.18초 |
| Arrival Attack Window | 0.3초 |
| Assist Radius / Circle Radius | 1.5타일 / 0.34타일 |
| Allow Dash Cancel / Allow Mode Cancel | 둘 다false |
| Solids / Hazards | 1<<8 / 1<<10 |
| 중심선 폭 / 나선 반경 | 0.045 / 0.07타일 |
| 앵커 표식 반경 | 0.38타일 |
| 난도 중간 앵커 수 | 3 / 4 / 5 |
| 수평 간격 | 6 / 7 / 8타일 |
| 이동 진폭·주기 | 0.5·4초 / 1·3.5초 / 1.5·3초 |

고정 속도이므로 가까운 점은 더 빨리 도착한다. 모든 거리에서 같은0.2초가 되도록 속도를 바꾸지는 않는다. 실제 이동은 물리 틱으로 반올림되므로50Hz/60Hz에서 시간을 따로 측정해야 한다.

## 돌진·충돌·도착 처리

1. 마우스가 가까운 등록 앵커를 가리키면 반경1.5타일 안에서 보정한다. 몸체부터 앵커까지 벽이 있으면 선택하지 않는다.
2. 앵커가 없으면 클릭 방향에서 처음 맞는 벽/천장을 선택한다. 캡슐의 표면 여유만큼 목표 중심을 띄운다.
3. 입력을 수락한 순간 출발점·도착점·방향을 고정한다. 발사 준비 뒤 GrappleDash로 전환하며 중력과 일반 이동 입력을 적용하지 않는다.
4. 매 틱 다음 이동량을 CircleCast와 Rigidbody2D.Cast로 검사한다. 가려지면 돌진을 취소하고 자유 낙하로 돌아간다. 판정 버퍼가 가득 찬 상황도 안전하게 취소한다.
5. 도착은 실제 물리 위치를 기준으로 확인한다. P4는 공격을 수락했을 때 TryConsumeZipStrike로 보너스를 한 번 소비한다. 성공 시 준비 창을 열고 점프·대시를 회복한다. 실패/취소에는 도착 보상을 주지 않는다.

P1의 외부 상태 속도가 물리 틱 끝의 이전 캐시로 덮이지 않도록 `SetExternalVelocity`를 연결했다. 피격·사망·재배치는 예약을 해제한다. `FinishExternal`은 Hurt/Dead를 되살릴 수 없다. 모드 전환은 즉시 되지만 기본 설정에서는 이미 발사된 돌진을 끊지 않는다.

움직이는 앵커는 **붙잡힌 짧은 시간만 이동을 멈춘다**. 그래서 방향 고정과 도착 성공을 함께 유지한다. 도착 시점의 미래 위치를 계산하는 예측 앵커는 이번 타입과 별도로 구현할 예정이다. MovingAnchor의 Orbit을 켜면 원형 궤도, 아니면 Moving Axis 방향 왕복이다.

## 앵커별 테스트

| 종류 | 테스트 방법 | 기대 결과 |
| --- | --- | --- |
| 고정 | 같은 고리를 여러 번 사용, 벽 뒤 고리 조준 | 반복 가능. 벽 뒤 후보는 보정되지 않음 |
| 일회용 | 정상 도착, 중간 벽 차단, 체크포인트 재시도 | 도착에만 소모. 차단은 미소모. 재시도하면 복원 |
| 이동 | 왕복·원형 변경, 끝 위치에서 발사, 발사 직후 관찰 | 허용 범위 안에서 선택. 예약 동안 목표 고정. 끝나면 움직임 재개 |

## 무적·위험·재시도

시험 가시는 `TraversalPracticeHazard`에서 위험 마스크와 GrappleDash 상태를 함께 확인한다. 일반 대시·낙하·발사 준비에는 이 무적이 없다. 적의 일반 Hurt 호출까지 무시하는 전역 무적으로 구현하지 않았다. 전체 HP·보스 패턴 무적·저스트 회피는 P7 범위다.

위험과 접촉하거나 아래로 떨어지면 방 입구로 즉시 재배치하고 일회용 앵커·이동 위상·선택/예약·도착 공격 창을 복원한다. 방/난도 전환도 같은 정리를 수행한다. 제어상 대기 시간을 두지 않았지만 사망→다시 조작5초 이내라는 실제 시간은 Unity에서 측정해야 한다.

## 테스트 체크리스트

- [ ] 신 Input System 설치 후 시험 씬을 열고 세 앵커가 각각 다른 색으로 보이는지 확인한다.
- [ ] 안전 방에서 바닥으로 떨어져도 계속 이동하고, 같은 종류를 위험 없는 환경에서 먼저 경험한다.
- [ ] 앵커·벽·천장으로 직선 이동하고, 관통·곡선 추적·진자 움직임이 없는지 확인한다.
- [ ] 두꺼운 벽·모서리에 돌진해 정지/낙하하고 무적이 벽 통과를 허용하지 않는지 확인한다.
- [ ] 일회용 도착/차단/재시도 세 경우를 확인한다.
- [ ] 움직이는 앵커의 극값·예약 중 고정·재시도 위상 초기화를 확인한다.
- [ ] 도착 뒤 공중 점프1회·대시·관성·0.3초 공격 준비 표시를 확인한다.
- [ ] 가시를 돌진으로 통과하고, 일반 점프/대시로 닿으면 재시도되는지 확인한다.
- [ ] 모드 변경·Esc·Alt+Tab·피격·사망·방 전환 중 예약이 남지 않는지 확인한다.
- [ ] 짧은 난관을 앵커선과 천장선 두 방법으로 각각 통과한다.
- [ ] 쉬움/보통/어려움에서 다음 앵커와 위험이 화면에 보이는지 확인한다.
- [ ] 실물 검사 메뉴를 실행하고30/60/144fps에서 직접 조작해 재도전 시간을 잰다.

## 흔한 버그

- 직선 속도가 적용되지 않음: 다른 스크립트가 매 FixedUpdate에 body.linearVelocity를 다시 쓰는지 확인한다. 외부 상태에서는 SetExternalVelocity를 사용한다.
- 앵커에 도착해도 회복 안 됨: 전방 캡슐 경로가 막혀 취소된 것인지 확인한다. 앵커 중심은 벽 안에 넣지 않는다.
- 이동 고리가 멈춰 있음: Clock Owner와 Input Ready·Pause·포커스, 활성 예약 여부를 확인한다.
- 일회용이 영원히 사라짐: 룸 재시도에서 ResetForRetry가 호출되는지 확인한다.
- 가시가 돌진도 막음: Trigger 여부와 Layer8/10을 확인한다. 위험과 차단 벽의 레이어를 섞지 않는다.
- 원래 메인 화면이 같이 뜸: 시험 씬을 단독으로 로드하고, 부트스트랩 제외 씬 이름을 변경하지 않는다.
- 공격 준비가 보여도 베기가 안 됨: 실제 Zip Strike 타격은 P4 연결 전이다.

이 환경에서는 Unity Editor가 없어 위 네이티브 검사·입력·물리·FPS를 실행하지 못했다. 자동 검사는 데이터·거리·수학 규칙·컨트롤러 보조 타입 확인이다.
