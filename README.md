# PLUGIN · Ancient Civilization

**v0.30.0 — 다섯 층의 흐름·난관과 고유 기믹 시험**

[다운로드 ZIP](https://github.com/han-17287/plugin1/archive/refs/tags/v0.30.0-download.zip) · [개발 브랜치](https://github.com/han-17287/plugin1/tree/ancient-civilization-prototype) · [실행·조작](AncientCivilization/README_KO.md)

**다른 네 층도 추가:** 생활부의 탐조등, 행정구의 보안 셔터, 상층 거주구의 환기류, 최상층의 시간 앵커를 시험합니다. `Assets/Scenes/TraversalRhythmPractice.unity`를 열고 Play한 뒤 층 버튼을 누르세요. 5층×안전/난관×세 난도, 총30개 조합을 선택할 수 있습니다.

[다른 층의 리듬·48개 번안·Prefab·설정·체크리스트](AncientCivilization/OTHER_FLOORS_RHYTHM_KO.md) · [다섯 층의 지형 검토 HTML](AncientCivilization/TraversalPreview.html)

실제 추가는 네 층의8개 시험 Room과 고유 기믹입니다. 20개 구간의75/25 리듬과48개 번안은 설계안이며, 기존 Main 전체 맵이나 보스를 교체하지 않았습니다. 고대 문명은2회차로 남겨 둡니다. 실제 Unity Play·성공률·재시도 시간·FPS는 미측정입니다.

**기본 갈고리 시험:** `Assets/Scenes/TraversalRhythmPractice.unity`를 열고 Play합니다. 고정·일회용·움직이는 앵커, 벽/천장 직선 돌진, 위험 레이어 무적, 도착 관성·공중 점프/대시 회복을 연결했습니다. 각 층의 안전 흐름 방과 짧은 난관 방을 쉬움/보통/어려움 데이터로 바꿔 시험할 수 있습니다.

[생산구5구간 리듬·12개 난관·타일 도면·복구 규칙](AncientCivilization/M35_RHYTHM_KO.md) · [P2 스크립트·Prefab·인스펙터·테스트](AncientCivilization/P2_TRAVERSAL_KO.md) · [지형 검토 HTML](AncientCivilization/TraversalPreview.html)

전체 리듬은 흐름75%·필수 난관25%인 설계안입니다. 기존 Main 맵을 이 배치로 교체하지 않았습니다. 실제 구현은 별도 두 시험 방이며, 투척·충전·거울 등 나머지 설계는 P3~P6 이후 검증합니다. M1은 코드 수치로 계산한 잠정 표이고 실제 Unity 플레이·성공률·재도전 시간·FPS는 미측정입니다.

P1 이동 연습도 포함합니다. **새 이동을 확인하려면 `Assets/Scenes/PlayerMovementPractice.unity`를 열고 Play**하세요. 신 Input System, 모드별 입력 연결, 상태 패턴, 가변 점프·벽 점프·대시·발판 하강과 디버그 표시를 포함합니다. [설정·스크립트 구성·튜닝 값·테스트·흔한 버그](AncientCivilization/P1_MOVEMENT_KO.md). 기존 `Main.unity`는 아직 기존 조작을 사용합니다. 추가 앵커와 무기·입자·레이저는 후속 단계에서 순서대로 연결합니다.

Input System 1.11.2를 추가하고 입력 설정을 Both로 변경했습니다. Unity가 요청하면 재시작하세요. 실제 Unity 임포트·Play·물리·FPS는 이 환경에서 실행하지 못했습니다.

Main 씬에는 기존 초반 학습과 하층 진행 개선도 포함돼 있습니다. 베기·입자 수집·수광부 레이저로 실제 검수문을 열고, 필수 행동 현장은 접근 안내 후 해결 즉시 다음 길로 이어집니다. 경비는 등장 전에 위치를 예고하며, 고정된 안전 발판에 착지하면 재도전 지점이 저장됩니다. 기존 지형·이야기·보스는 유지합니다. **실제 Unity 플레이와 난이도·완주 시간 검증은 아직 남아 있습니다.** [변경 내용과 확인할 점](AncientCivilization/GAMEPLAY_FLOW_KO.md)

- 생산구: 재고 보고 중 코어 절도 → 자동주행 오토바이/트럭 갑판 전투 → 코어 회수·생존 검거 → 야간 장부·전력 복구·재하 구조·사본 전송. 검거 뒤 무전 지시로 동쪽 수사로가 바로 열립니다.
- 생활부: 도둑의 흔적을 따라 주거 골목·폐차장·감시 외벽·창고를 통과합니다. 상자·차량 와이어 투척, 탐조등 엄폐, 경비 로봇, 조직원전 뒤 거대 로봇 3페이즈로 이어집니다.
- 로봇 전투: 체력 68%/34% 전환, 로켓 반격, 부분/풀게이지 레이저 배리어 판정, 흡입 때 천장 와이어 고정, X 공격 중복 피해 방지, 제압 뒤 잔해와 두목 진술을 보완했습니다.
- 맵: 일반 길은 단차와 하부 통로·상부 지름길을 사용하며, 지정된 점프 구간은 이동 크레인·반동 부유체·천장 와이어를 사용합니다. 생활부의 용도를 구분하는 도트 배경 8종을 추가했습니다. 경비는 한 번에 둘씩, 역할 조합은 구간에 따라 달라집니다.
- 저장: 완료한 수사와 보스 제압을 따로 복구하며, 남은 보스의 잠긴 출구 뒤에 배치되는 구버전 위치 오류를 수정했습니다.

[하층 진행 구조도](AncientCivilization/LowerDistrictOverview.svg) · [현장 목록·목표 시간](AncientCivilization/LOWER_DISTRICTS_KO.md) · [도트 배경 소스 미리보기](AncientCivilization/LowerDistrictWalls.png)

ZIP을 풀고 `AncientCivilization` 폴더를 Unity **6000.6.7f1**에서 연 뒤 `Assets/Scenes/Main.unity`를 실행하세요. 월드는 Play 중 생성됩니다. 실행 EXE가 아닌 소스 프로젝트입니다.

조작은 A/D 이동, Space 점프, Ctrl 달리기, 휠 위 공격/아래 와이어, 우클릭 입자 수집입니다. 와이어 좌클릭은 직선 견인·대형 물체 투척, 가운데 클릭은 스윙입니다. 공격 좌클릭은 짧게 세로·가로·사선 베기, 길게 조준 후 놓아 굵은 레이저를 쏩니다. E는 대화·현장·엘리베이터, Tab은 기록입니다.

초반 호칭은 **백 경위**이며, 대화는 캐릭터 곁의 작은 한국어 순차 말풍선과 무전기 아이콘으로 표시합니다. 층 이동은 실제 엘리베이터를 이용합니다. 저격수의 친구 정체와 고대 문명 플레이 구역은 2회차에 남겨 둡니다. 비서실장은 1페이즈 뒤 살아서 퇴장합니다.

실제 Unity 임포트·Play·빌드·FPS·전체 완주 시간은 이 환경에서 검증하지 못했습니다. 자동 검사는 소스·에셋·독립 규칙과 보조 타입 확인이며, 게임의 완성 비율이나 플레이 시간을 보증하지 않습니다.

- [현재 구현·남은 작업](AncientCivilization/COMPLETION_KO.md) · [Unity 확인표](AncientCivilization/PLAYTEST_KO.md) · [검사 결과](AncientCivilization/VALIDATION.txt)
- [전체 이야기](AncientCivilization/STORY_KO.md) · [보스 패턴](AncientCivilization/FLOOR_DESIGN_KO.md) · [블레이드](AncientCivilization/BLADE_COMBO_KO.md) · [변경 내역](AncientCivilization/CHANGELOG.md)
- [보관한 고그래픽 무기 시안](ArtReferences) · [별도 블레이드 콘셉트 MP4](https://raw.githubusercontent.com/han-17287/plugin1/ancient-civilization-prototype/Video/Officer_Blade_Concept_60fps.mp4). 영상은 12초·1080p·60fps의 정지 이미지 기반 애니매틱이며 게임 플레이 녹화가 아닙니다.
