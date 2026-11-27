# V5 · URP 2D 플러그 Shader Graph

두 파일은 Shader Graph에서 열어 편집하는 Sprite Unlit 그래프다. 체결 Bloom은 해당 플레이어를 보는 카메라에서 켤 수 있다. 이 폴더의 원본은 프로젝트의 Assets 밖에 위치한다.

## 설치와 확인

1. Unity 6000.6의 **URP 2D Renderer가 이미 활성화된 프로젝트**에서 Unity가 제공하는 URP·Shader Graph 17.6 계열을 사용한다. 기본 렌더러로 이 게임을 실행할 때는 기존 플러그 원화와 셰이더가 동작한다.
2. 이 문서 옆의 `BluePlugV5` 폴더 전체를 프로젝트 `Assets` 아래로 복사한다. `Resources/BluePlugURP`의 이름과 위치 관계를 보존한다. 같은 리소스 이름으로 복사본을 두 개 만들면 로더가 모호해진다.
3. Unity 메뉴 **Tools → Blue Plug → Validate URP Graphs**를 실행한다. 두 그래프를 다시 임포트하고 컴파일 메시지, 지원 여부, 플레이어가 쓰는 프로퍼티를 검사한다. Project 창에서 그래프를 열어 각 출력이 Base Color와 Alpha에 연결됐는지 확인한다.
4. 체결 순간 주변 밝기가 살짝 올라오게 하려면 게임플레이 카메라에 `BluePlugDockBloom`을 붙인다. URP 카메라의 Post Processing과 HDR을 켜고 Volume Update Mode를 매 프레임 업데이트하도록 설정한다. 카메라 GameObject가 속한 레이어를 카메라의 Volume Mask에 포함한다. 카메라에 별도 레이어를 쓰면 편집 창에서도 어떤 볼륨이 영향을 주는지 구별하기 쉽다.
5. 두 경위 카메라가 같은 소켓을 볼 수 있는 구성에서는 각 컴포넌트의 Watched Player에 해당 경위의 `BluePlugVisuals`를 지정한다. 비워 두면 해당 카메라 화면 안의 체결을 받는다. 인게임 그래픽 품질을 낮추거나 피드백 줄이기를 켜면 체결 Bloom이 꺼진다.

## 그래프의 연결

| 그래프 | 입력과 계산 | 결과 |
|---|---|---|
| SignalCord | UV0, Time, `_Flow`, `_CableLength` → TravelingPulse. UV0와 Time → CableEdge. 맥동에 HDR `_Signal`과 `_Energy`를 곱한다. | CurrentRadiance → Base Color, CableEdge의 부드러운 가장자리 → Alpha. Additive 모드. 선의 UV는 전선 전체에 걸쳐 한 번 펼친다. 원색은 #5CCBFF다. |
| PlugFilament | 메인 스프라이트 `_MainTex`의 유리창 필라멘트·절연 캡 표시등 마스크를 UV0로 한 번 샘플한다. FilamentRadiance가 RGB에 `_Intensity`를 곱한다. | 필라멘트 발광 → Base Color, 원화 알파 → Alpha. Additive 모드에서 SpriteRenderer의 색과 투명도가 적용된다. 금속 핀과 외피는 별도의 불투명 원화로 읽힌다. |

전선은 UV.x 좌표에 길이를 곱해 늘어난 케이블에서도 파형 간격을 유지한다. `_Flow`의 부호가 바뀌면 맥동 방향이 반전된다. `_Intensity` 기본값은 1.65이며 실제 충전·체결 상태는 렌더러별 MaterialPropertyBlock으로 전달된다. 양 그래프의 Custom Function은 String 모드여서 그래프 안에서 바로 식을 고칠 수 있다.

| 그래프 노드 | 직렬화된 Object ID |
|---|---|
| SignalCord · CableEdge | `b6bf996012825a4a9177653a9492d3f6` |
| SignalCord · TravelingPulse | `8ccbb4bf633d529585075c05c729a7d6` |
| SignalCord · CurrentRadiance | `8c3ed8307de856d09d0fbe76dbd337d1` |
| PlugFilament · Sample Texture 2D | `fa6a23efaa4a53a795a1fb12779b32dd` |
| PlugFilament · FilamentRadiance | `d787cc2cdb2d50ba9af554e1b6c67408` |

## 체결 Bloom

실제 연결 순간에 발생하는 `BluePlugVisuals.DockFlash`를 받으면 기본 0.10초간 카메라 전용 볼륨 weight가 최대 0.30에서 0으로 줄어든다. 볼륨은 Bloom 강도만 1.8로 보간하고 원래 프로필의 색, 산란, 임계값은 유지한다. 효과는 포스트 프로세싱이므로 체결 순간 같은 카메라에 보이는 다른 HDR 광원에도 적용된다. 핀의 국소 발광만 필요하면 카메라 컴포넌트를 비활성화한다. 렌더 직전·직후에 weight를 설정·복구해 메뉴나 다른 카메라로 섬광이 넘어가지 않게 했다.

두 그래프의 객체 ID, 슬롯 방향, 중복 연결, HDR 프로퍼티 이름과 Sprite Unlit 타깃을 정적으로 검사했다. SignalCord에는 14개 노드·11개 연결, PlugFilament에는 9개 노드·5개 연결이 있다. native shader import, HDR 장치 출력과 2D Renderer에서의 GPU 프레임은 이 작업 환경에서 실행하지 못했다. 해당 확인은 위 Unity 메뉴와 실제 플레이 화면을 사용한다.

[SignalCord 그래프](BluePlugV5/Resources/BluePlugURP/SignalCord.shadergraph) · [PlugFilament 그래프](BluePlugV5/Resources/BluePlugURP/PlugFilament.shadergraph) · [카메라 Bloom](BluePlugV5/BluePlugDockBloom.cs) · [에디터 검사](BluePlugV5/Editor/BluePlugGraphChecks.cs) · [Unity 6000.6 Sprite Unlit 타깃](https://github.com/Unity-Technologies/Graphics/blob/6000.6/staging/Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Targets/UniversalSpriteUnlitSubTarget.cs)
