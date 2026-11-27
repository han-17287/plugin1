# 파란 플러그 V1·V2 — 외형 설계와 제작 프롬프트

이 문서는 V1 외형 설계와 V2 이미지 제작 사양이다. 선택안은 **절연 캡으로 감싼 유리 점등창 플러그, 은회색 2핀, 이동색 `#5CCBFF`**다. 이미지 생성 프롬프트는 제작 지시이며 생성·수정·인게임 검증을 완료했다는 뜻이 아니다. 구현 조작과 현재 기능은 [BLUE_PLUG_KO.md](BLUE_PLUG_KO.md)를 따른다.

## 1. 기준과 구현 경계

- 크기는 1타일=1 world unit로 표기한다. PPU는 에셋별 픽셀 밀도다. 타일 크기와 이미지 파일 크기를 혼동하지 않는다.
- 선택 플러그 작업 캔버스는 **60×34px, 52PPU**, 약 **1.15×0.65타일**이다. 오른쪽 핀 끝이 삽입 기준이며 피벗은 원본 좌하단 기준 **(58.5,16.5)px**, 정규화 **(0.975,0.4853)**다. 케이블 부싱까지는 단자 기준 뒤쪽 **55/52≈1.058타일**, 몸통 중심은 대략 x=−0.59타일이다.
- 소켓 작업 캔버스는 **51×51px, 44PPU**, 약 **1.16×1.16타일**이다. 중심 피벗, 검은 접점 두 개, 비대칭 키 홈과 분할 표시등을 공통으로 쓴다. 7종은 이 공통 인서트 주변의 프레임을 바꾼다.
- 위 크기·2핀 간격·피벗은 이번 선택안의 제작 계약이다. 향후 생성 이미지는 계약에 맞춰 정리한 후 교체한다. 큰 콘셉트 이미지를 축소한 결과를 정식 픽셀 원화로 바로 사용하지 않는다.
- 현재 플러그의 연결 상태는 Ready/Winding/Flying/Connected/Towing/Swinging/Retracting 등의 이동 세션이 관리한다. 소켓 표시 상태는 `Idle/Ready/Blocked/Linked/Locked`다. **Inactive/Active/Spent는 V2 그림 패키지의 분류이며 새 게임 상태 enum이 아니다.**
- 고정·일회용·이동·시간제 장치와 일부 릴레이·반동 장치는 기존 게임 기능이다. 아래 7개 이름이 각각 독립적인 새 기능으로 구현된 것은 아니다. 고압·과부하·범용 다중 소켓·절연 특성 등의 판정은 후속 기능이다.

관련 소스: [BluePlugArt.cs](Assets/Scripts/BluePlugArt.cs), [BluePlugVisuals.cs](Assets/Scripts/BluePlugVisuals.cs), [BluePlugSocket.cs](Assets/Scripts/BluePlugSocket.cs), [BluePlugMotionTimeline.cs](Assets/Scripts/BluePlugMotionTimeline.cs), [AncientSwingAtlas.cs](Assets/Scripts/AncientSwingAtlas.cs). 그림은 연결·이동 판정을 읽기만 하며 콜라이더나 기술 해금을 바꾸지 않는다.

선택안의 현재 원화·재질은 [BluePlugAtlas.png](Assets/Resources/Traversal/BluePlugAtlas.png), [BluePlugEmission.mat](Assets/Resources/Traversal/BluePlugEmission.mat), [BluePlugEmission.shader](Assets/Resources/Shaders/BluePlugEmission.shader)에 있다. 아래 프롬프트는 이 원화를 대체·확장할 제작 지시다. 7종 전체 원화와 새 양손 회전 시트가 이 아틀라스에 모두 들어 있다는 뜻은 아니다.

## 2. V1 플러그 디자인 3안

| 안 | 실루엣·크기·재질 | 디테일 5개 | 장점 / 약점·가독성 | 판단 |
|---|---|---|---|---|
| **A. 캡 절연 유리 플러그** | 뒤가 둥글고 앞은 두 갈래인 짧은 공구형. 1.15×0.65타일. 짙은 남청 고무·검은 금속·상단 유리·은회색 핀. | ① 두 핀 사이 검은 틈 ② 충격을 막는 상하 절연 캡 ③ 아래쪽 손가락 걸림 홈 ④ 상단 유리 안의 필라멘트 ⑤ 뒤쪽 주름 부싱과 케이블 목. | 플러그의 기능과 잡는 곳이 즉시 읽힌다. 기계팔과도 재질이 맞는다. 유리를 크게 열면 작은 크기에서 장난감처럼 보이므로 창은 몸체 상단 약 1/3에 제한한다. | **채택.** 기존 회전·던지기·삽입 방향을 바꾸지 않고 정체성을 보강한다. |
| B. 필라멘트 전구 플러그 | 배가 둥근 유리 몸통과 좁은 목, 전방 2핀. 약 1.05×0.80타일. 두꺼운 유리·세라믹 목·황동 내부. | ① 둥근 유리 외곽 ② 눈에 띄는 U자 필라멘트 ③ 목의 절연 고리 ④ 충격 방지 가드 두 줄 ⑤ 핀 뿌리의 흰 세라믹. | 전구 세계관이 강하고 충전 단계가 잘 보인다. 원형 실루엣이 회전 때 방향을 흐리고 전구 정령·폭탄과 혼동하기 쉽다. 투명 면이 커서 밝은 배경에서 약하다. | 정비소·구형 장비 콘셉트 후보. 현재 플레이어 기본형으로는 A가 선명하다. |
| C. 샤프심 쐐기 플러그 | 길고 얇은 육각 쐐기 몸통, 끝의 2핀. 약 1.35×0.48타일. 흑연 세라믹·절연 금속·좁은 발광 창. | ① 육각 흑연 면 ② 중앙 압축 코일 ③ 길쭉한 유리 슬릿 ④ 갈라진 핀 캡 ⑤ 뒤쪽 고정 클립. | 날카로운 투척 방향과 속도감이 좋다. 작은 화면에서 칼·화살처럼 보여 꽂는 공구라는 기능이 약해지고 노란 블레이드와 역할을 혼동한다. 긴 몸체가 회전 반경을 더 크게 보이게 한다. | 후속 장비 콘셉트로 보관. 기본 장비로 교체하지 않는다. |

A의 검은 외곽은 1~2 native px, 두 핀 사이 빈틈은 최소 2px를 확보한다. 위아래를 완전히 대칭으로 만들지 않고 상단 창·하단 홈으로 방향을 표시한다. 핀 끝이 빛에 묻히지 않게 본체 색과 분리한다. 이동색은 유리 안과 전류 면에 집중하고 절연 외피 전체를 파랗게 발광시키지 않는다.

### 전선 디자인

| 안 | 모양·권장 폭 | 사용 판단 |
|---|---|---|
| **절연 케이블 + 발광 도관** | 짙은 윤곽, 안쪽 파란 선, 이동 때만 밝아지는 중심. 현재 중심선 폭 0.037타일, 윤곽 약0.080타일, 낮은 알파 후광 약0.185타일. | 기본 채택. 느슨할 때 무게감 있는 곡선, 당겨질 때 일직선, 스윙 때 줄 길이가 보인다. 전류 이동 중 작은 파동으로 전환한다. |
| 직선 중심 + DNA형 이중 나선 | 팽팽한 중심선은 곧게 유지하고 두 가는 빛선이 반대 위상으로 감싼다. 나선 진폭 약0.03~0.05타일. | 입자 와이어 콘셉트의 시각 확장 후보. 줄 전체가 뱀처럼 요동하지 않게 한다. 장거리에서는 나선을 생략하고 중심선만 남기는 LOD가 필요하다. 현재 절연 케이블을 자동으로 대체하는 구현은 아니다. |
| 분절 절연 띠 | 짧은 고무 마디와 밝은 이음매가 반복된다. 중심선 방향을 따라 배치. | 짧은 정비소·장비 근접컷에 적합. 긴 줄에서 체인처럼 보이거나 드로콜·무아레가 늘 수 있어 기본형에는 사용하지 않는다. |

전선 텍스처는 길이 방향 U축으로 반복한다. 왼쪽과 오른쪽 끝의 색·알파·패턴 위상이 일치해야 한다. 32×8px 절연 패턴과 32×8px 발광 마스크를 별도 제작하고, 실제 화면에서 1px 미만인 줄을 억지로 반복 텍스처로 표현하지 않는다. 줄의 손쪽 끝은 백 경위 왼손의 릴, 플러그 쪽 끝은 뒤쪽 부싱에 붙인다. 핀 끝에서 전선이 나오는 그림은 금지한다.

고무 코드의 느슨함은 중력 방향의 큰 곡선 하나로 읽힌다. 전화기식 코일 코드는 느슨할 때 감긴 마디가 붙고 당길 때 간격이 벌어진다. 코일은 가까운 정비 연출에 유리하지만 원경과 빠른 좌우 반전에서 모아레가 생긴다. 이동 기본형에는 고무 피복을 쓰고, 릴 안쪽만 감긴 구조로 그린다.

## 3. 발광과 색상 구분

아래 색은 표시 문법이다. 기존 공격·입자 색을 이 문서만으로 일괄 변경하지 않는다. 현재 레이저와 수집 입자도 파란색을 쓰므로 **플러그 전용 하늘색의 역할은 색상 차이와 형태를 함께 써야** 읽힌다.

| 대상 | 중심 색·명도 | 형태·시간 신호 | 겹칠 때 우선순위 |
|---|---|---|---|
| 플러그·이동·실제 소켓 | **하늘색 `#5CCBFF`**, 밝은 끝단 `#D6F7FF` | 얇은 이어진 줄, 두 접점, 화살표/호, 이동 방향 잔상. | 항상 실제 플러그·연결 지점 외곽을 남긴다. 장식 하늘색을 늘리지 않는다. |
| 플레이어 입자 레이저 | 현행 파랑/청백 몸체 + 흰 고온 코어 | 와이어보다 훨씬 두꺼운 직선 빔, 시작/충돌부 원반. | 빔 코어가 가장 밝다. 이동선은 짙은 윤곽으로 살아남고 중복 후광을 줄인다. |
| 수집 입자 | 현행 파랑 계열. 대표값 `#1F8CFF` 부근, 작은 청백 핵 | 흩어진 짧은 점·결정, 수집 시 플레이어 쪽으로 수렴. 길게 연결된 선을 만들지 않는다. | 입자는 점, 이동은 선이라는 규칙을 유지한다. 파란 장식 파편은 실제 보상으로 오인되지 않게 탁하게 한다. |
| 샤프심 전구 블레이드 | 노랑 `#FFD83D` + 따뜻한 흰 핵 | 길고 두꺼운 칼날, 세로/가로/사선 베기 호. | 소켓의 약점 표시와 같은 노란 검 모양을 사용하지 않는다. |
| 일반 위험·차단 | 주황 `#FF9A48` / 주홍 `#EF5A4D` | ×, 닫힌 셔터, 톱니·해칭·영역 경계. | 경고 신호는 플러그 후광·스파크보다 앞에서 읽혀야 한다. |
| 보스 공격·지속 전기 | 보스별 기존 팔레트와 공격 문양. 신규 지속 전류는 보라 계열 후보 | 굵은 위험 경계·시계침·조준선 등 고유 형태. | 보스의 장식 광원을 소켓과 같은 두 접점+하늘색 고리로 만들지 않는다. 실제 연결 포트만 이동 문법을 공유한다. |
| 비활성·잠김·사용 완료 | 저채도 남회색 `#495566`, 검은 홈 | 자물쇠, 끊긴 띠, 남은 점등 수·닫힌 접점으로 구별. | 단순 알파0으로 없애기보다 상태 문양을 남기는 것이 향후 원화 기준이다. 현재 일회용 표시는 사용 후 숨겨지는 경로가 있다. |

### HDR 강도와 지속 시간

강도는 선형 RGB에 곱하는 **배율**이며 EV 노출값이 아니다. `_Intensity` 기본 **1.65**는 선택 발광 재질의 기준이다. 표시 컨트롤러는 필라멘트·전선에 상태별 세기를 별도로 전달한다. 아래는 작성 시점 소스의 기본값이며 실제 밝기는 카메라·셰이더·알파·배경에 따라 확인한다. 본체 PNG·알파에 Bloom을 구워 넣지 않는다. 실제 프로젝트의 렌더 파이프라인·카메라 HDR·Bloom 지원도 별도 검증한다.

| 구간 | 발광 배율 / 알파 | 지속 시간·곡선 | 구분·확인 사항 |
|---|---|---|---|
| 휴대/대기 | 플러그 조립체 숨김 | Ready 동안 | 휴대 창을 항상 보이게 하는 것은 향후 표시 선택이다. 현재 동작을 문서만으로 바꾸지 않는다. |
| 회전 충전 | 필라멘트·전선0.5→4 / 후광0.38→0.78 | 0→0.65초 상승 후 유지 | `peakEmission=4` 기본. 3단계 그림은 연속값을 보여주는 제작안이며 레이저 충전 단계와 별개다. |
| 투척 | 필라멘트·전선3 / 후광0.52 | 비행 중 유지 | 투척 몸 포즈는0.18초. 비행 종료시간을 이 포즈 길이로 제한하지 않는다. |
| 삽입 섬광 | 필라멘트4→0.9, 섬광 재질1.65 / 최대0.91 | 0.16초 감쇠 | 두 핀·단자 홈이 완전히 가려지지 않아야 한다. |
| 연결 대기 | 필라멘트·전선0.9 / 후광0.52 | 연결 유지 중 | 접속이 이동 시작을 뜻하지 않는다. C/V 입력 전 줄은 느슨할 수 있다. |
| 견인/스윙 | 필라멘트·전선3.4 / 후광0.52 | 실제 이동 중만 유지 | 이동 때 전선 흐름과 작은 파동. 견인 잔상은 별도 풀의 몸체 샘플이다. |
| 회수 | 필라멘트·전선0.6 | 회수 중, 완료 후 릴 점등0.12초 | 재투척 허용 시간·자원 판정과 별개인 표시다. |
| 도착 섬광 | 재질1.65 / 최대0.82→0 | 0.18초 팽창·감쇠 | 복귀 몸 포즈0.26초와 독립이다. |
| 견인 잔상 | 일반 스프라이트, HDR 배율 없음 / 기본0.24→0 | 0.15초 수명, 최대6개 | 새 V2 잔상 이미지 없이 실제 몸체를 샘플링한다. 연출 축소 시 알파0.10. |
| 접속/도착 스파크 | 일반 스프라이트, HDR 배율 없음 / 기본0.72→0 | 0.13~0.21초, 접속6개/도착8개, 풀16개 | 연출 축소 시2개·0.12초. V2의 별도 스파크 시트는 교체 후보이며 현재 필수 에셋이 아니다. |
| 소켓 대기/조준/연결 | 재질1.65, 상태별 알파×`glowIntensity=.82` | 대기 지속 / 선택 중 / 연결 중 | 조준·연결이 가장 밝다. 상태별 알파와 HDR 강도를 같은 수치로 취급하지 않는다. |

두 핀과 몸체는 불투명 원화, 필라멘트·창 내부·작은 잔광만 발광면으로 분리한다. 강도 증가보다 창 면적·필라멘트 고리 수·소켓 분할등 수로 충전을 읽게 한다. 연출 축소 모드에서는 섬광·잔상 면적과 알파를 낮추되 상태 문양은 남긴다.

소켓 맥동은 표시등과 후광에만 적용한다. 대기는 2.4초 주기·최대 밝기 대비10% 이내, 조준은0.95초·14% 이내, 연결은1.6초·4.5% 이내로 밝기를 줄였다 복귀한다. 견인·스윙·상태 문양은 고정 밝기다. 잠김·차단·전원 꺼짐·사용 완료에는 맥동이 없고, 일시정지 중에는 위상이 멈춘다. 저품질에서는 고정 표시로 남기며 선택 소켓의 선택적 Light2D는 끈다.

## 4. 소켓 7종 외형과 상태

공통 **Inactive**는 기술 미해금/전원 부족 등 세부 이유를 잠금·차단 문양으로 덧붙일 베이스 원화다. **Active**는 사용할 수 있는 상태, **Spent**는 소모/작동 완료용 원화다. 반복 사용 가능한 벽 소켓을 사용했다고 영구 소모된 그림으로 바꾸지 않는다. 현재의 Idle/Ready/Blocked/Linked/Locked는 인서트 위 아이콘으로 표현한다.

| 이름 | 외형·재질과 인식 요소 | Inactive / Active / Spent 그림 | 실제 기능과 구분 |
|---|---|---|---|
| **벽 콘센트** | 단단한 사각 금속 프레임, 모서리4개, 중심의 비대칭 접점 인서트. | 어두운 홈 / 하늘색 분할 고리·화살표/호 / 인서트가 닫힌 회색 시험 원화. | 기본 고정 소켓으로 사용 가능. 반복형의 Spent는 제작용 변형이며 현재 상시 소모를 뜻하지 않는다. |
| **낡은 콘센트** | 비뚤어진 프레임, 금 간 세라믹, 볼트 하나 누락. 접점은 정상 위치. | 금 간 외곽과 꺼진 창 / 작은 하늘색 점등 두 칸 / 전구가 끊기고 접점 덮개가 내려옴. | 일회용 장치에 붙일 수 있는 외형 후보. 모든 낡은 소켓이 일회용이라는 현재 규칙은 없다. |
| **멀티탭** | 세로 또는 가로로 나란한 모듈, 번호 대신 점1/2/3, 넓은 외곽. 접속 가능한 인서트를 각각 구분. | 모든 창 꺼짐 / 현재 대상만 점등 / 완료된 칸은 검은 가로선·닫힌 창. | 범용 다중 연결·순서 퍼즐은 향후 기능. 그림의 여러 구멍이 자동으로 여러 연결점을 만들지 않는다. |
| **회전 콘센트** | 원형 외륜, 비대칭 키 홈, 외부 3개의 짧은 눈금. | 외륜 정지 / 연결 가능한 인서트와 목표 눈금 점등 / 외륜 고정핀·창 닫힘. | 왕복 이동 소켓은 현재 있지만 회전 접점 정렬 퍼즐은 별개 향후 기능이다. 실제 접점 각도와 그림 회전이 일치해야 한다. |
| **변압기 콘센트** | 양옆 코일과 굵은 세라믹 절연체, 중앙 소켓. 벽형보다 넓은 프레임. | 코일 꺼짐 / 중앙 접점만 하늘색·코일에는 흐름 방향 / 닫힌 셔터와 방전 흔적. | 기존 보스 장치의 실제 포트와 새 과부하 약점 기능을 구분한다. 범용 ×2 과부하를 원화만으로 적용하지 않는다. |
| **고압 콘센트** | 뾰족한 세라믹 돌기, 큰 경고판, 굵은 접점 테두리. | 경고 테두리 유지·접점 꺼짐 / 중앙은 이동 하늘색, 외곽 위험 표시는 주황 해칭 / 차단판·끊긴 경고선. | 위험 접속·감전·쿨다운 판정은 향후 기능. 사용할 수 있다는 파랑과 위험한 주변을 다른 형태로 함께 표시한다. |
| **절연체 차단벽** | 두꺼운 흰 세라믹 사각판, 검은 분리 홈, 막힌 면. 두 핀 구멍을 그리지 않는다. | 대기 때 내려간 판 / 차단할 때 가로막는 검은 띠·주황 해칭 / 작동 후 접힌 판. 세 상태 모두 이동 하늘색을 쓰지 않는다. | 전선과 몸의 이동을 막는 장애물이다. 같은 설비에 실제 콘센트를 추가할 때만 독립된 접점 부품으로 옆에 배치한다. |

멀티탭·변압기·고압 프레임을 크게 만들더라도 공통 접점 인서트의 핀 간격은 유지한다. 프레임은 연결 판정 영역이 아니다. 화살표(C:견인)·호(V:스윙)·자물쇠를 미세한 글자로 대체하지 않는다.

## 5. V2 영어 이미지 프롬프트

아래 공통 머리말 + 개별 지시 + 공통 negative를 함께 사용한다. 참조 이미지가 필요한 캐릭터 프롬프트에는 실제 [Hero.png](Assets/Resources/Art/Hero.png)·[HeroSwing.png](Assets/Resources/Art/HeroSwing.png)를 도구에 첨부한다. 파일 이름을 텍스트로 적는 것만으로 이미지 참조가 전달되지는 않는다.

### 공통 머리말과 negative

```text
Production concept for an original 2D side-scrolling pixel-art action game. Strict orthographic view, crisp hand-placed pixel clusters, controlled dark outline, a small coherent palette, no antialiasing. Movement energy uses sky blue #5CCBFF. Separate opaque material surfaces from emissive areas. Transparent background, isolated asset, generous transparent padding, consistent lighting from upper left. Show all functional parts clearly at small gameplay size. No baked bloom. Exact final pixel dimensions will be enforced during manual cleanup.
```

```text
Negative: perspective camera, isometric view, 3D render, painterly brushwork, soft cartoon shading, photographic texture, gradient background, text, logo, watermark, checkerboard baked into the image, baked bloom, opaque black background, blurred edges, random pin count, merged pins, disconnected cable, inconsistent proportions, cropped parts.
```

### 5.1 플러그 정투영·필라멘트·전선

**선택 플러그 정투영 시트**

```text
Design A: a compact insulated industrial electrical plug, protected clear glass light window across the upper third, durable midnight-blue rubber caps above and below, five shallow finger-grip notches underneath, a dark rear strain-relief bushing, exactly TWO separate silver-gray flat pins pointing right with a visible dark gap. Sky-blue #5CCBFF filament inside the window. The shell is opaque and does not glow. One dominant side elevation plus smaller orthographic top, front and rear views, all the same object and scale, no perspective. Annotated layout must contain no written labels; leave empty label space. Side-view production footprint: 60 by 34 pixels at 52 pixels per world unit; clear pin-tip anchor at pixel (58.5, 16.5) measured from bottom left. Supply a separate side-view asset without the other views.
```

**필라멘트 3단계 시트**

```text
Three aligned copies of the exact SAME side-view plug, identical silhouette, pins, window, bushing and pivot. Stage 1: one short dim sky-blue filament segment. Stage 2: two connected bright filament loops inside the glass. Stage 3: three continuous filament loops with a tiny near-white core, leaving dark gaps between loops. Only the internal filament changes; no weapon growth, extra pins or explosion. Provide a second aligned mask sheet showing only the three emissive filament shapes in white on transparent background. These are visual winding-charge stages, not laser-damage levels.
```

**타일형 전선**

```text
A seamless HORIZONTAL repeating 2D pixel-art cable segment, target 32 by 8 pixels. Dark insulated outer jacket, a straight narrow sky-blue #5CCBFF central conduit, subtle one-pixel ridges. The first and last columns must match exactly in color, alpha and pattern phase. Transparent padding above and below. No plug, socket, knots, perspective or cast shadow. Also produce a separate aligned emission mask containing only the central conduit. Optional second variant: a perfectly straight center line wrapped by two thin opposite-phase helical light traces, with matching tile boundaries and a restrained amplitude.
```

### 5.2 소켓 7종: 각각 Inactive / Active / Spent

각 행은 별도 이미지 요청으로 사용한다. 공통 머리말 뒤에 아래 공통 소켓 지시와 해당 행을 붙인다. 출력은 가로 3칸이며 프레임 간격·중심·핀 간격을 유지한다. 시트 한 장에 7종을 모두 작게 넣지 않는다.

```text
Create one socket design as a 3-cell horizontal sprite sheet: INACTIVE, ACTIVE, SPENT from left to right. Every cell has the same centered 51 by 51 pixel working canvas at 44 pixels per world unit, with equal transparent gutters. The central insert has exactly TWO dark separated contact slots and one asymmetric key notch, matching the same plug pin spacing in every state. Inactive: dark insert and unlit segmented indicator. Active: #5CCBFF segmented ring, clear dark contacts and no bloom. Spent: a physically closed insert or broken filament mark, dim gray indicator, visibly different in grayscale. No text labels or numbers. Keep all frame silhouettes aligned.
```

| 소켓 | 영어 개별 지시 |
|---|---|
| 벽 | `A sturdy wall-mounted industrial receptacle, clipped-square dark steel frame, four chunky corner bolts, small ceramic central insert. In spent state show a closed mechanical shutter. Preserve clear arrow and curved-arc icon mounting space beside the insert.` |
| 낡은 | `An old receptacle with a slightly bent metal frame, one missing bolt, two large readable cracks in the ceramic outside the contacts, not noisy rust texture. Active uses only two small blue indicator bars. Spent has a snapped internal filament and lowered contact cover. Contacts stay accurately aligned.` |
| 멀티탭 | `A power-strip receptacle with three clearly separated modules, distinguished by one, two and three embossed dots, no written numerals. Keep each contact insert equal in size. Active highlights only the designated middle module; spent closes completed modules with a dark crossbar. Do not suggest three usable targets with one undivided glowing center.` |
| 회전 | `A rotary receptacle with a circular outer collar, three chunky index marks and one asymmetric key notch. Inactive collar is dark; active highlights the correct index and central two-slot insert; spent shows a locking pin holding the collar and closed contacts. Orthographic front view, no perspective wheel.` |
| 변압기 | `A transformer receptacle with a central two-contact insert, thick ceramic insulators and two compact copper coils framing it. The opaque housing stays dark. Active lights only the central sky-blue connection and one directional flow mark; spent closes the center shutter with restrained discharge marks. No electricity covering the contacts.` |
| 고압 | `A high-voltage receptacle with pointed ceramic fins, heavy insulated rim and bold orange diagonal hazard hatching outside the central connection insert. Active central contact ring is sky-blue while the orange hazard rim remains distinct. Spent has a thick shutoff plate and interrupted hazard stripe. No text, no generic lightning cloud.` |
| 절연체 | `Use the material palette and three-cell framing only. This seventh fixture is a solid insulating barrier. Three aligned states: idle ivory plate, deployed barrier with black transverse bars and amber hatching, spent folded plate. No socket, no contact holes and no sky-blue light in any cell. Preserve a readable dark edge against a pale wall.` |

3칸 시트가 핀·피벗·외곽을 바꾸면 먼저 Active 한 장을 확정한 뒤 그 이미지를 편집 참조로 써서 다른 두 상태를 제작한다. 모듈 수가 많은 멀티탭의 프레임은 51px보다 넓게 확장할 수 있지만 **공통 인서트의 크기·기준점은 확대하지 않는다.**

### 5.3 전류 잔상과 스파크

**전류 이동 잔상**

```text
A 6-frame horizontal sprite sheet of a compact electric transit afterimage traveling to the RIGHT, original 2D pixel art. Preserve the reference officer's broad silhouette only as a simplified tapered energy echo, not a solid second character. Sky-blue #5CCBFF leading edge, tiny near-white core, dark transparent gaps, progressively thinner broken tails. Each 96 by 64 pixel cell shares one leading-point pivot and equal transparent padding. Frame 1 gathers, frames 2-3 peak, frames 4-6 dissipate. Transparent background, no thick laser beam, no full-screen lightning, no face, no text, no baked glow.
```

**접속 스파크**

```text
A 6-frame pixel-art electrical contact spark sheet, equal 32 by 32 pixel cells, fixed central contact pivot. One small central white-blue point with three to five short angular #5CCBFF sparks. Frame 1 contact, frames 2-3 brief spread, frames 4-6 rapid fade into separate small dots. Keep a dark empty center gap so two metal contact pins remain legible when composited. No circular explosion, orange fire, smoke, starburst covering the whole sprite or baked bloom. Transparent background.
```

### 5.4 주인공 양손 회전·투척 스프라이트

참조 원화의 특징을 보존한다: 표정이 보이지 않는 검은 모자 아래 얼굴, 긴 흑백 코트, 붉은 목도리, 금색 작은 장식, 흰 금속과 하늘색 관로의 기계팔. 기계팔 끝을 사람 손가락으로 바꾸지 않는다. 기계팔은 장비 그립, 반대쪽 검은 장갑 손은 전선/손잡이를 받쳐 **양쪽 팔을 쓰는 자세**로 만든다. 블레이드는 회전 궤적과 겹치지 않게 수납·분리 레이어로 처리한다.

```text
Use the attached existing Hero.png and HeroSwing.png as strict character-identity references. Create an ORIGINAL production sprite sheet for this SAME tall officer, not a new character: white-and-black long coat with distinct panels, small gold trim, red scarf, dark peaked cap, face completely hidden in shadow with no readable expression, white ancient mechanical forearm with sky-blue channels, black trousers and boots. Preserve proportions, coat hem, shoulder width, hat, scarf and leg length across every cell. Do not copy any named game's character.

Side view facing right, 8 equally spaced WINDING keyframes for a two-arm industrial plug cast. His anatomical LEFT hand stays closed around the reel and cable at his waist in every frame. His anatomical RIGHT mechanical hand grips the plug-side cable and guides its orbit. Keep these anatomical roles even when he faces left. Both arms work together, elbows and shoulders follow the circular pull, weight stays over planted feet, torso counter-rotates slightly. One clockwise plug orbit sampled at 0,45,90,135,180,225,270,315 degrees. Keep the hip/root at the same pixel in each cell. The plug head and full cord are separate layers; show only a tiny temporary locator at the hand connector, no long baked cable or glow. Stow the yellow blade so it does not cut across the spinning cord. Transparent background, clean hand-placed pixel clusters, no facial features, no human fingers added to the machine arm. An optional lower-cost version uses 4 keyframes at 0,90,180,270 degrees with the same root and identity.

Provide each pose separately as well as the sheet. For each pose reserve a matching marker-only layer for HIP, FOOT CONTACT and CABLE EMITTER positions; markers must be removed from final color sprites. Do not claim exact anatomy or frame registration without manual cleanup.
```

```text
Using the approved winding sprite as the only character reference, make 4 side-view RELEASE and RECOVERY keyframes: the right shoulder opens, the anatomical RIGHT mechanical grip opens on the exact release frame, the left fist braces the reel as cable pays out of the LEFT hand, then the torso returns to a balanced stance. Show a continuous left-to-right-to-plug path before release and a left-to-plug path afterward. The actual plug and cable remain separate. Keep the same hip anchor, foot line and character scale. Show no teleport, duplicated hands, changing coat design or disconnected mechanical shoulder. Final animation timing will be 0.18 seconds for throw presentation and 0.26 seconds for recovery; do not print timing text in the image.
```

4~8프레임 회전 원화는 `HeroSwing`의 스윙8포즈와 다른 제작물이다. 기존 스윙 포즈·절차적 팔 움직임을 사용한 표현과 새로 그린 회전 시트를 구분한다. 새 원화를 통합할 때는 프레임을 실제 회전 각도에서 선택하며 기본4회전/초, 최대 약6.6회전/초에 맞춘다. 프레임 번호 자체를 투척·연결 판정의 근거로 삼지 않는다.

## 6. 편집·임포트·발광 분리 순서

1. **형태 확정:** A의 측면 한 장과 플러그에 맞는 소켓 인서트를 먼저 정리한다. 컬러·검은 실루엣·발광 끔 시트를 나란히 비교한다. 콘셉트 시트의 설명용 시점과 실제 게임용 정투영 원화를 분리한다.
2. **픽셀 정리:** 생성 이미지의 임의 반투명 외곽·노이즈·왜곡된 핀을 직접 수정한다. 60×34/51×51 크기는 마지막 단계에서 nearest-neighbor로만 줄이는 것으로 충족되지 않는다. 최종 격자에서 핀과 홈을 다시 그린다.
3. **레이어 분리:** `Plug_Base`(불투명 외피·핀), `Plug_Emission`(유리 내부·필라멘트), `Plug_GlassHighlight`(작은 반사), `Cord_Base`, `Cord_Emission`, `Socket_Base`, `Socket_Emission`, `Socket_Indicator`로 나눈다. 발광 맵은 Base와 같은 캔버스·피벗이며 외곽 캡과 핀의 전체 면을 흰색으로 칠하지 않는다.
4. **상태 파생:** 승인한 Base 한 장을 편집 참조로 재사용해 충전3단계와 소켓3상태를 만든다. 매번 처음부터 생성해 핀 간격·몸체 길이가 바뀌게 하지 않는다. 원본과 마스크는 동일한 폴더에 버전명으로 보관한다.
5. **Import:** Sprite(2D and UI), Point, Mip Maps Off, Compression None을 시작값으로 사용한다. 전선은 필요한 U축만 Repeat, 몸체·아이콘은 Clamp. 흐린 후광 전용 텍스처만 Bilinear 후보로 둔다. 알파는 투명 배경을 실제로 확인한다.
6. **PPU·피벗:** 플러그52PPU·핀 끝(58.5,16.5), 소켓44PPU·중심. 자동 Trim으로 기준점이 바뀌지 않게 한다. 픽셀 좌표의 좌상단/좌하단 체계를 명시한다. 캐릭터는 기존 220PPU 원화 메타데이터와 힙·단자 좌표를 보존하거나 전체 atlas 메타데이터를 함께 갱신한다. 파일만 교체하지 않는다.
7. **발광 적용:** 별도 발광 SpriteRenderer 또는 실제 지원 셰이더의 emission 입력을 사용한다. 흰 마스크×`#5CCBFF`×선형 강도배율로 계산하고 Base에는 일반 스프라이트 재질을 둔다. 캐릭터 코트 전체나 플러그 외피 전체가 가산광이 되지 않게 한다. Color/mask 텍스처의 sRGB 처리는 선택 셰이더의 샘플링 계약에 맞춘다.
8. **조립·검증:** 핀 끝은 실제 삽입점, 부싱은 전선 끝, 캐릭터 단자는 손쪽 끝에 붙인다. 별도 프레임을 추가하면 atlas bounds/pivot/emitter 좌표를 측정한다. 저품질·발광 끔에서도 실물의 상태 문양을 확인한다.

파일 이름 예: `BluePlug_A_Base.png`, `BluePlug_A_Filament_01.png`, `BluePlug_A_Emission_01.png`, `BlueSocket_Wall_Base.png`, `BlueSocket_Wall_Active_Emission.png`, `Officer_PlugWind_00.png`. Active/Emission을 혼용해 불투명 원화를 잃지 않는다. 이 이름의 파일이 이미 생성됐다는 뜻은 아니다.

## 7. 흑백·배율·동작 확인표

- [ ] **검은 실루엣:** A/B/C의 앞·뒤, 두 핀, 부싱이 구분되는가? 선택 A가 블레이드·전구 몬스터로 오인되지 않는가?
- [ ] **흑백:** 소켓 Inactive/Active/Spent가 색 없이도 분할등·셔터·문양으로 구분되는가? 사용 불가와 영구 소모가 같은 ×만 남지 않는가?
- [ ] **1×/2×/3× 정수 확대:** 핀 두 개와 검은 틈이 유지되는가? 비정수 카메라 축소에서는 실제 최소 표시 크기·윤곽을 확인했는가? 한 원본 픽셀보다 작은 세부를 필수 정보로 쓰지 않는가?
- [ ] **다섯 층 배경:** 밝은 벽·어두운 공장·하늘색 장식 앞에서도 외피·접점·소켓 문양이 남는가? 실제 게임 카메라와 시야 가장자리에서도 확인했는가?
- [ ] **발광 끔/연출 축소:** 유리창의 점등 자체, 두 접점, 견인 화살표·스윙 호·자물쇠가 읽히는가? 과한 밝기로 핀 간격이 사라지지 않는가?
- [ ] **겹침:** 레이저·청색 입자·보스 공격·전선이 함께 있을 때 점/선/빔의 두께와 위험 문법이 유지되는가? 위험 ×가 잔상에 가려지지 않는가?
- [ ] **회전/삽입/스윙:** 30/60/120fps에서 전선이 부싱·단자에 붙고 회전 한 바퀴·투척 후0.18초 자세가 끊기지 않는가? 단자 각도가 소켓 홈과 맞는가?
- [ ] **원화 교체 안전성:** 시각 변경으로 실제 사거리·회전 반경·충돌·소켓 기능·보상 수치가 바뀌지 않았는가? 미구현 고압·멀티탭 기능을 화면만으로 작동한다고 안내하지 않는가?

이 확인표는 제작 후 수행할 항목이다. 이번 문서만으로 Unity Play, 실제 Bloom, 이미지 정합성, 프레임 비용을 통과했다고 보고하지 않는다.

## 8. 작업 용어

| 용어 | 이 작업에서의 뜻 |
|---|---|
| Plug / Receptacle | 던지는 수 플러그 / 고정 또는 장치에 달린 암 소켓. 둘을 모두 plug라고 부르지 않는다. |
| Pin tip / Bushing | 삽입·위치 정렬 기준인 금속 단자 끝 / 전선이 나오는 뒤쪽 고무 부싱. |
| Orthographic / Silhouette | 원근 없는 정투영 / 내부 디테일을 지운 검은 외곽 형태. |
| PPU / Pivot / Emitter | 1 world unit당 원본 픽셀 수 / 그림의 변환 기준점 / 캐릭터 전선 연결 단자점. |
| Base / Emission / Bloom | 불투명 재질색 / 광원을 표현할 면·마스크 / 카메라 후처리 번짐. 서로 다른 자료다. |
| Inactive / Active / Spent | 꺼진 베이스 / 사용 가능한 그림 / 소모·완료 그림. 실제 플레이 가능 여부는 코드 상태를 따른다. |
| Afterimage / Spark | 이동 방향을 남기는 짧은 잔상 / 접촉 순간의 작은 방전. 지속 위험 구역과 구별한다. |
