# PLUGIN · Ancient Civilization

**v0.35.0 · 캠페인 사운드트랙**

[Unity 프로젝트 ZIP](https://github.com/han-17287/plugin1/archive/refs/tags/v0.35.0-download.zip) · [집행관 여섯 자세](ArtReferences/Record_Enforcer_Poses.png) · [실행·조작](AncientCivilization/README_KO.md)

**국악 퓨전 사운드트랙 50곡 · 수정판** · [메인 테마 MP3](https://raw.githubusercontent.com/han-17287/plugin1/city-score-50-revised/Soundtrack/Main-Theme.mp3) · [46초 수정 전후 비교](https://raw.githubusercontent.com/han-17287/plugin1/city-score-50-revised/Soundtrack/Before-After.mp3) · [하이라이트 MP3](https://raw.githubusercontent.com/han-17287/plugin1/city-score-50-revised/Soundtrack/City-Score-Preview.mp3) · [음악 ZIP](https://github.com/han-17287/plugin1/archive/refs/tags/city-score-50-revised.zip) · [전곡과 Unity 사용법](Soundtrack/README_KO.md)

메인 화면의 〈봉인된 하늘 너머〉부터 추격전, 보스전, 엔딩까지 50곡이 게임 진행에 따라 재생됩니다. 지역 이동 연습에서도 설정한 층과 난관에 맞춰 음악이 바뀝니다. F7로 음악과 효과음을 함께 끄고 켤 수 있습니다.

행정구 기록 집행관은 짧은 흑발과 단안 렌즈, 분절 견갑, 입자 조형 장갑, 등 추진기를 착용한다. 입장 컷신에서 백 경위가 떨어지는 쇳덩이를 보고 뒷걸음으로 피한다. 전투 뒤 집행관은 원본에 관한 말을 남기고 추진기로 빠져나가며 2회차에 다시 등장한다. Unity에서 `Assets/Scenes/Main.unity`를 열고 **PLUGIN → 검사 → 기록 집행관 바로 플레이**를 선택하면 곧바로 확인할 수 있다. [전투와 퇴장](AncientCivilization/ENFORCER_BOSS_KO.md)

| 첨부 지시 | 문서 |
|---|---|
| D0 · 10개 항목 진단, 재미 저하 재현법, 수정 우선순위 | [플레이테스트 진단표](AncientCivilization/D0_PLAYTEST_KO.md) |
| D1 · 장비 실루엣, 광원, 색과 형태, URP 2D 이행 | [비주얼 가이드](AncientCivilization/D1_VISUAL_GUIDE_KO.md) |
| D2–D3 · 동작 타이밍, 등장 컷신, 비정지 무전 | [모션과 컷신](AncientCivilization/D2_D3_MOTION_CINEMA_KO.md) |
| D4 · 집행관 패턴 시간표, 다음 보스 후보 3개 | [보스 설계와 QA 메뉴](AncientCivilization/D4_BOSS_WORKSHOP_KO.md) |
| D5–D6 · 타격 피드백, 장비 효과음, 페이즈별 음악 | [피드백과 믹서](AncientCivilization/D5_D6_FEEDBACK_AUDIO_KO.md) |
| D7 · 프레임, GC, 전투 비용, 출시 점검 | [프로파일링](AncientCivilization/D7_PROFILING_KO.md) |

Main 메인 화면에서 **새 지역 맵 둘러보기**를 선택합니다. 생산구 하역장, 생활부 주거 골목, 행정구 보안 기록동, 거주구 온실 테라스, 최상층 정비 외벽을 서로 다른 실제 지형으로 만들었습니다. 각 지도는112~120타일이며 지상 길·건물 위 정비로·중앙 난관·착지 체크포인트3곳을 연결합니다. 연속 천장, 하역·주거 원화, 구조 지지대·창문·간판을 추가했고, 임시 사각형 주인공은 기존 백 경위 원화로 교체했습니다.

[지역 구성·실행·코드·설정·체크리스트](AncientCivilization/DISTRICT_MAPS_KO.md) · [실제 좌표 배치도](AncientCivilization/DistrictMapPreview.html)

새 이동 컨트롤러의 지역 맵을 구현한 버전입니다. **기존 Main 순찰의 모든 현장·무기·보스를 새 좌표로 이전하지는 않았습니다.** 기존 스토리와 저장은 메인 순찰에서 계속 사용합니다. 새 지도에서는 오른쪽 위 도움말로 층·장치 난도·안전 바닥을 선택하며, 설정을 연 동안 이동·장치가 멈춥니다. 지도끼리의 연결문은 시험용 선택이며 스토리 승진/엘리베이터를 대체하지 않습니다. Unity6000.6.7f1 실기 임포트·Play·체감·FPS는 이 환경에서 확인하지 못했습니다.

기존 [60개 기믹 설계](AncientCivilization/TraversalPreview.html)와 [P2 구성](AncientCivilization/P2_TRAVERSAL_KO.md)은 기초 방/후속 무기 기믹 자료로 유지합니다. 그 단순 도면과 현재 실행되는 건축형 지역 지도는 구분합니다. 고대 문명 지역은2회차 범위로 남겨 둡니다.

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
