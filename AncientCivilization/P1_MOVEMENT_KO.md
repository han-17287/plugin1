# P1 입력·모드·이동 기반 — v0.28.0

Unity 6000.6.7f1용이다. `Assets/Scenes/PlayerMovementPractice.unity`를 열고 Play한다. 추가 생성 메뉴를 실행할 필요 없이 연습 지형과 플레이어가 생성된다. 기존 `Main.unity` 스토리는 기존 컨트롤러를 사용한다. 새 이동 기반을 실제 조작으로 확인한 뒤 P2~P7을 단계별 연결한다.

## 실행과 설정

프로젝트를 열면 Package Manager가 `com.unity.inputsystem` 1.11.2를 설치한다. 공식 패키지의 최소 Unity 버전은 2019.4다. Input System 어셈블리의 UI 참조를 위해 Unity 6용 `com.unity.ugui` 2.0.0도 선언했다. 연습 화면은 IMGUI를 사용한다. Input System과 기존 게임을 함께 실행하기 위해 Active Input Handling은 **Both**다. Unity가 재시작을 요청하면 재시작한다. 새 `PlayerInputReader`는 신 Input System만 사용한다. 입력 패키지가 없으면 명시적인 설정 오류를 내고 새 플레이어 이동을 멈춘다.

렌더 파이프라인은 이번 P1에서 전환하지 않았다. 연습 씬은 기본 SpriteRenderer로 동작한다. URP 2D·Light2D·Bloom 전환은 레이저 비주얼 단계에서 머티리얼과 함께 처리한다. 현재 전구 표시는 색 표시용 스프라이트다.

새 씬은 기본 빌드 목록에서 꺼져 있다. 연습 씬을 실행 파일로 만들 때는 Build Profiles의 씬 목록에서 이 씬을 넣고 시작 씬으로 지정한다. `Main.unity`와 새 씬을 동시에 로드하지 않는다. 연습 씬에서는 기존 게임 부트스트랩을 실행하지 않으며, 기존 저장 데이터를 읽거나 변경하지 않는다.

## 스크립트와 부착 위치

| 파일 | 붙일 위치·필수 구성 | 인스펙터·레이어 |
| --- | --- | --- |
| `PlayerMovementData.cs` | GameObject에 붙이지 않는 ScriptableObject | `Resources/PlayerCore/DefaultMovement.asset`에서 모든 이동 수치와 곡선 조절 |
| `PlayerInputBuffer.cs` | 입력 리더가 소유하는 일반 C# 객체 | 별도 컴포넌트 없음. 입력 만료·1회 소비·스크롤 재무장 처리 |
| `PlayerInputReader.cs` | Player 루트. PlayerModeController 필요 | Data에 DefaultMovement 지정. Resources의 P1Player.inputactions를 복제해 사용 |
| `PlayerModeController.cs` | 같은 Player 루트 | Initial Mode=Hook. 휠 위=Attack, 아래=Hook, Q=반전 |
| `PlayerStateMachine.cs` | Player 루트. Rigidbody2D·CapsuleCollider2D·입력·모드 컴포넌트 필요 | Data 지정. 루트 Layer=9 Player, 충돌 바닥 Layer=8 GroundSolid |
| `PlayerDebugOverlay.cs` | Player 루트 | Target·Input 지정. Bulb에는 전구 표시 SpriteRenderer 지정. Visible로 디버그 화면 켜기/끄기 |
| `PlayerMovementPractice.cs` | 연습 씬의 P1 Movement Practice 루트 | Movement Data 지정. 플레이어·카메라·단차·타일·벽·단방향 발판 자동 구성 |
| `Editor/PlayerCoreSmokeChecks.cs` | 붙이지 않는 Editor 메뉴 | Play 중 `PLUGIN/P1/연습 씬 구성과 상태 검사` 실행 |

플레이어 기본 캡슐은 폭 0.72, 높이 1.8, 수직 방향이다. Rigidbody2D의 중력은 컨트롤러가 계산하므로 Gravity Scale=0, Rotation Freeze, Interpolation=Interpolate, Collision Detection=Continuous다. 런타임에 마찰·탄성 0의 PhysicsMaterial2D를 적용한다. 태그는 필요 없다.

다른 씬에서 직접 플레이어를 구성하려면 위 루트 컴포넌트와 시각 자식을 붙이고 데이터를 지정한다. 시각 자식의 flip/회전으로 루트 캡슐을 반전하지 않는다. P2 이후 공격 히트박스는 Facing을 기준으로 따로 반전한다.

## InputActionAsset

`Assets/Resources/PlayerCore/P1Player.inputactions`는 Input Actions 편집기로 수정할 수 있다. 공식 InputActionImporter GUID를 메타데이터에 지정했다. 자동 생성 C# 래퍼에 의존하지 않는다.

| 액션 | 타입·바인딩 | 현재 처리 |
| --- | --- | --- |
| Move | Value Vector2, WASD·방향키 | 수평 이동·하강 방향 |
| Jump | Button, Space | 코요테·점프 버퍼·가변 높이·벽 점프 |
| Dash | Button, Left Shift | 수평 대시·횟수 제한·착지 초기화 |
| Drop | Button, S+Space OneModifier | 점프보다 먼저 소비, 단방향 발판만 하강 |
| ModeScroll | PassThrough Axis, 마우스 scroll/y | 임계값·중립 복귀·재입력 간격 |
| ModeToggle | Button, Q | 즉시 모드 반전 |
| Primary | Button, 좌클릭 | Hook이면 GrappleAim 요청, Attack이면 Attack 요청 |
| Collect | Button, 우클릭 홀드 | CollectHeld 제공. P3에서 입자 수집 연결 |
| Throw | Button, E | Throw 요청. P5에서 연결 |
| Laser | Button, F | 누름·뗌을 각각 버퍼. P6에서 연결 |
| Explode | Button, R | 폭발 회수 요청. P5에서 연결 |
| Pause | Button, Esc | 연습 플레이어 물리·행동 정지 |
| Point | PassThrough Vector2, 마우스 위치 | 조준 좌표·모드색 크로스헤어 |

Move와 Collect 같은 연속 값은 현재 상태를 제공한다. 일회성 동작은 0.15초 버퍼를 사용한다. 모드 전환·Pause는 즉시 처리한다. 모드 전환 시 이전 좌클릭·레이저 버퍼를 지워 오발을 방지한다. 포커스를 잃으면 버퍼와 홀드 상태를 지운다.

스크롤은 절댓값 0.1 미만을 무시하고, 중립으로 돌아오기 전까지 한 번만 처리한다. 처리 간격은 0.16초다. 연속 트랙패드 패킷으로 수십 번 전환되지 않는다. Q는 프레임당 새 눌림만 처리한다.

## 상태와 후속 단계 연결

상태 패턴의 `IPlayerState.Enter/Tick/Exit`를 사용한다. Idle·Run·Jump·Fall·WallSlide·WallJump·Dash는 실제 이동 상태다. Hurt·Dead와 Respawn도 구현돼 있다. GrappleAim·GrappleDash·Attack·Throw·LaserCharge·LaserFire는 후속 서비스가 제어하는 연결 상태다. 해당 기술 자체를 P1 완료로 계산하지 않는다.

`ActionRequested(command, requestedState)`를 구독한 서비스는 행동을 실제로 수락했을 때만 true를 반환한다. 수락한 서비스는 `BeginExternal`로 상태에 진입하고 `ExternalStep`에서 물리 틱을 받는다. 끝나면 `FinishExternal(launchVelocity, resetAirActions)`를 호출한다. 갈고리 도착은 resetAirActions=true로 점프/대시 준비를 초기화한다. 외부 상태 중 이동 속도는 해당 서비스가 관리한다.

P1 연습 씬의 수신기는 요청 경로만 표시하고 false를 반환한다. 좌클릭으로 갈고리가 날아가거나 공격이 발생하지는 않는다. 따라서 연습 씬을 통해 입력 라우팅과 이동을 먼저 확인할 수 있다.

현재 우선순위는 Dead/Hurt > Drop 판정 > Dash > Jump > Explode > Throw > LaserRelease > LaserPress > Primary > 이동이다. S+Space는 Jump를 삭제한다. Dash 중 후속 전투 입력은 실행하지 않고 버퍼 만료를 따른다. 최종 Zip Strike와 회수 우선순위는 P2/P5/P7에서 조정한다. 대시에는 무적이 없다. 가시·톱날 전용 돌진 무적은 P2 범위다.

## 튜닝 기본값

| 항목 | 값 |
| --- | --- |
| 이동 속도 / 지상 가속 / 감속 / 공중 가속 | 9 / 65 / 85 / 32 |
| 점프 속도 / 중력 / 낙하 중력 배수 / 최대 낙하 | 14 / 32 / 1.65 / 24 |
| 점프 해제 시 상승 속도 배수 | 0.5 |
| 코요테 / 입력 버퍼 | 0.12초 / 0.15초 |
| 정점 판정 속도 / 유지 시간 / 중력 배수 | 1.5 / 0.08초 / 0.45 |
| 벽 슬라이드 최대 하강 / 벽 점프 X·Y / 입력 잠금 | 3 / 10·13 / 0.16초 |
| 공중 대시 수 / 속도 / 지속 / 종료 후 간격 | 1 / 22 / 0.17초 / 0.18초 |
| Ground Probe / Wall Probe / Skin | 0.09 / 0.12 / 0.025 |
| 발판 하강 충돌 무시 시간 | 0.22초 |

가속 곡선은 목표 속도와의 차이 0~1을 받아 가속 배율로 쓴다. AnimationCurve 인스펙터에서 곡선을 바꾼다. 상승 중 Space를 떼면 상승 속도를 한 번 줄이며, 피격 넉백에는 이 처리를 적용하지 않는다. 벽 점프 잠금 중에는 방향 입력과 새 벽 점프로 기존 밀어내기를 덮어쓰지 않는다.

## 2D 지형 구성

지면·벽은 Rigidbody2D.Cast 결과의 법선으로 판정하고 Trigger는 제외한다. 대시는 실제 캡슐의 다음 물리 이동 거리보다 Skin만큼 앞을 검사해 벽 앞에서 속도를 제한한다. Collider2D/Physics2D만 사용한다.

연습 바닥은 Grid → Tilemap + TilemapRenderer + TilemapCollider2D + CompositeCollider2D + Static Rigidbody2D다. Unity 6의 Composite Operation=Merge, Extrusion Factor=0.01, Composite Geometry=Polygons다. 타일마다 생기는 경계를 합쳐 이음새 걸림을 줄인다.

하강 발판은 BoxCollider2D와 같은 오브젝트에 PlatformEffector2D를 붙이고 Collider의 Used By Effector, Effector의 Use One Way·Use One Way Grouping을 켠다. 일반 바닥은 S+Space로 통과하지 않는다. 하강 중에는 해당 발판과의 충돌만 잠시 무시하고, 종료·재배치·비활성화 때 복원한다.

## 테스트 체크리스트

- [ ] Unity에서 Input System 설치·임포트 완료 후 연습 씬의 Input 표시가 정상인지 확인한다.
- [ ] A/D를 눌렀다 놓고 방향을 바꿔 가속·감속과 시각 표시를 확인한다.
- [ ] 발판 끝에서 늦게 점프하고, 착지 직전에 Space를 눌러 코요테·버퍼를 확인한다.
- [ ] Space 짧게/길게 누르기의 높이 차이와 정점 머무름을 확인한다.
- [ ] 벽에 붙어 하강 속도를 확인하고 벽 점프 직후 반대 입력이 0.16초 동안 밀어내기를 취소하지 않는지 확인한다.
- [ ] 공중 대시 1회 후 두 번째 대시가 막히고, 착지 뒤 다시 가능한지 확인한다.
- [ ] 벽을 향해 대시하며 30/60/144fps에서 관통·모서리 걸림을 확인한다.
- [ ] S+Space가 단방향 발판에서는 하강하고 일반 바닥에서는 관통하지 않는지 확인한다.
- [ ] Q와 휠로 모드·전구·크로스헤어가 함께 바뀌고, 이전 모드의 클릭이 오발하지 않는지 확인한다.
- [ ] 빠른 연속 스크롤·트랙패드 스크롤의 중복 전환 방지를 확인한다.
- [ ] Esc·Alt+Tab에서 이동이 정지하고 복귀 때 보류된 점프/공격이 나오지 않는지 확인한다.
- [ ] 실물 검사 메뉴를 실행해 상태·Rigidbody2D·재질·이전 게임 미생성을 확인한다.
- [ ] Main.unity에서 기존 저장·스토리 실행이 유지되는지 확인한다.

## 흔한 버그

- 입력이 안 됨: Input System 패키지 임포트와 Active Input Handling=Both, Unity 재시작 여부를 확인한다.
- 기존 메인 화면이 같이 뜸: 연습 씬 이름을 바꾸었거나 두 씬을 동시에 로드했는지 확인한다. 부트스트랩의 연습 씬 제외 조건과 이름이 같아야 한다.
- 발판을 못 내려감: Effector가 Collider와 같은 오브젝트인지, Used By Effector와 Use One Way가 켜졌는지 확인한다.
- 타일 경계에 걸림: Tilemap Collider의 Merge·Extrusion과 Composite/Static Rigidbody2D 구성을 확인한다.
- 벽을 바닥으로 인식: Ground Normal을 지나치게 낮추지 말고, Root를 회전·반전하지 않는다.
- 좌클릭에 아무 기술도 나오지 않음: P1에서는 요청 표시가 정상이다. 갈고리는 P2, 근접은 P4에서 구현한다.
- 입력이 두 번 실행됨: 새 Player와 AncientHero가 동시에 켜져 있지 않은지 확인한다.

자동 검사는 버퍼·스크롤·입력 에셋 계약과 보조 컴파일을 확인한다. 이 환경에는 Unity Editor가 없어 위 실기 체크리스트와 Editor 메뉴는 실행하지 못했다. 실제 이동 감각·타일 충돌·임포트·FPS는 Unity에서 확인해야 한다.
