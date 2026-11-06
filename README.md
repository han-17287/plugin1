# PLUGIN · Ancient Civilization

개발 브랜치: [`ancient-civilization-prototype`](https://github.com/han-17287/plugin1/tree/ancient-civilization-prototype)

**v0.18.1 Unity 소스:** [다운로드 ZIP](https://raw.githubusercontent.com/han-17287/plugin1/ancient-civilization-prototype/PLUGIN_v0.18.1_ProductionPursuit_Unity_6000.6.7f1.zip)

생산구 도입을 **재고 점검 → 보고 중 벽 돌파 → 자동주행 오토바이/트럭 위 전투 두 차례 → 세 번째 탑승 회수 컷신**으로 바꿨습니다. 생산장까지362단위의 안전한 이동 튜토리얼과 별도1,600단위 수송 터널을 구성했습니다. 코어를 담은 짐칸을 먼저 분리하고, 높은 백덤블링 뒤 굵은 입자 레이저로 트럭을 폭파합니다. 새 도입부는 **새 게임**으로 확인하세요.

[생산구 맵 구조도](AncientCivilization/ProductionMapOverview.svg)는 설계도이며 실제 Unity 실행 화면은 아닙니다.

- 와이어 좌클릭은 충돌을 검사하며 즉시 직선 견인, 가운데 클릭은 기존 스윙.
- 캐릭터 곁의 작은 말풍선과 한국어 순차 표시. 긴 대사는 페이지로 나누고 통신관은 무전기 아이콘으로 표시합니다.
- 층마다 한 번 임무를 듣고 **도입부 → 네 수사 이정표 → 동쪽 목적지**까지 2,912단위의 같은 맵을 달립니다. 이정표마다 별도 방 입장·소장 복귀 없이 자동 진행하고, 결과는 이동 중 무전으로 전달합니다.
- 이정표 20개는 각각 폭 624단위·3구간이며, 지정된 다섯 곳의 가운데 구간에 층별 점프·와이어 기믹을 유지합니다. 구간별 적은 최대 2명입니다. 안전 단말 E는 그 자리에서 저장하며 층 입구·목적지의 엘리베이터로 왕복합니다.
- 오토바이는 초반·마지막 추격전에서만 배치합니다. 메뉴는 보관한 블레이드 시안과 수정된 주인공·대치·베기 그림 등을 포함한 배경 9종 중 실행별로 하나를 선택합니다.
- 전용 48프레임 연격, 파란 입자, 주민 25명과 저격수 4페이즈·비서실장 시계 최종보스. 레이저는 최대 24단위가 즉시 이어지며 흰 중심·푸른 본체·넓은 빛을 표시합니다. 본체 폭으로 벽과 피해를 판정합니다.
- 일반 적은 경직 중 접촉 피해가 멈추고 화면 근처에서 새 공격을 준비합니다. 입자 수집과 40/80단계·100 완충 표시를 보완했습니다.
- 주민은 확보한 통로에서 빠르게 뒤따라오며, 빈 전력 수광부는 입자 재채취 패널로 안내합니다. 사건 방식에 맞춰 근접 경비와 물체 투척 적을 함께 배치했습니다.

ZIP의 `AncientCivilization` 폴더를 Unity **6000.6.7f1**에서 열고 `Assets/Scenes/Main.unity`를 실행하세요. 월드는 Play 중 생성됩니다. 실행 EXE가 아닌 소스 프로젝트입니다.

초반 화면의 주인공 호칭은 **백 경위**입니다. 1회차 저격수는 같은 처지만 확인하고 친구 공개는 2회차, 비서실장은1페이즈 뒤 살아서 퇴장합니다. 실제 Unity Play/빌드/FPS·전체 완주 시간은 미검증이며, 9시간 플레이나 완성 비율을 보장하지 않습니다.

별도 [수정된 블레이드 콘셉트 MP4](https://raw.githubusercontent.com/han-17287/plugin1/ancient-civilization-prototype/Video/Officer_Blade_Concept_60fps.mp4)는 **12초·1080p·60fps 애니매틱**입니다. 전개 전 팔을 수정하고 같은 자세에서 칼날이 뻗어나오도록 카메라·전개·효과를 새로 렌더링했습니다. 정지 이미지 기반 합성으로, 게임 플레이 녹화나 연속 캐릭터 동작 영상이 아닙니다. [영상 구성·원본·재현 방법](Video/README_KO.md).

- [실행·조작](AncientCivilization/README_KO.md) · [블레이드 연격](AncientCivilization/BLADE_COMBO_KO.md) · [현장 경로](AncientCivilization/FIELD_EXPANSION_KO.md)
- [현재 완성도](AncientCivilization/COMPLETION_KO.md) · [저격수](AncientCivilization/SNIPER_BOSS_KO.md) · [스토리](AncientCivilization/STORY_KO.md)
- [실기 확인표](AncientCivilization/PLAYTEST_KO.md) · [검증 결과](AncientCivilization/VALIDATION.txt) · [성능 측정](AncientCivilization/PERFORMANCE_KO.md)
- [변경 내역](AncientCivilization/CHANGELOG.md) · [보관한 고그래픽 무기 시안](ArtReferences)
