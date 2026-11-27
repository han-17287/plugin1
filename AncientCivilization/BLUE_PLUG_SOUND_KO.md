# 파란 플러그의 조작음

왼쪽 버튼을 누르면 회전음의 간격이 짧아지고, 사거리가 최대에 이르면 높은 두 음이 한 번 울린다. 버튼을 놓으면 케이블 풀리는 소리가 이어진다. 콘센트에 꽂히는 딸깍 소리가 끝나면 플레이어가 C 또는 V를 선택할 때까지 조용해진다. 이 간격 덕분에 체결과 이동 시작을 귀로 구분할 수 있다.

## 이벤트와 타이밍

| 이벤트 | 재생 시점과 길이 | 들리는 단서 |
|---|---|---|
| 회전 | Winding 동안 250 ms 루프, 피치 1.00 → 1.65 | 손잡이 안쪽 모터의 맥동이 초당 4회에서 6.6회로 빨라진다. |
| 완충 | 충전 비율 0.999에서 105 ms, 회전당 한 번 | 1,090 Hz에 이어 33 ms 뒤에 1,635 Hz가 울린다. 기본 완충 시간은 BluePlugData의 0.65초다. |
| 투척 | Flying 진입 때 104 ms | 짧은 저중음과 공기 마찰음이 릴이 풀리는 순간을 찍는다. |
| 케이블 풀림 | Flying 동안 300 ms 루프, 피치 1.04 | 축이 돌아가는 저음에 얇은 금속 마찰음이 붙는다. |
| 체결 | 실제 소켓 체결 때 92 ms | 핀 접촉, 16 ms 뒤의 잠금쇠, 24 ms 뒤의 전기 스파크 순으로 들린다. |
| 이동 | 견인·스윙 동안 400 ms 루프 | 견인은 피치 1.15의 빠른 윈치음, 스윙은 0.83의 낮고 조용한 장력음이다. |
| 견인 도착 | 실제 견인 종점 도착 때 126 ms | 136 Hz 부근의 충격음 위로 12 ms 늦게 하우징이 울린다. |
| 되감기 | Retracting 동안 280 ms 루프, 피치 1.04 | 헛던지거나 연결을 풀었을 때 케이블이 릴로 감기는 잔진동이 난다. |
| 재장전 | Retracting → Ready 때 64 ms | 핀과 걸쇠가 16 ms 간격으로 자리 잡는다. 새 입력을 받을 수 있다는 단서다. |

## Unity 연결과 믹스

`BluePlugVisuals`가 본편의 `AncientHero` 또는 연습장의 `PlayerStateMachine`에서 `BluePlugSound.ForPlayer(gameObject)`를 호출한다. `Show(phase, charge)`는 회전량과 이동 상태를 전달하고 `Click()`과 `Arrival()`은 게임 판정에 맞춰 호출한다. 비활성화 때 `Clear()`로 정리한다. 장식 플러그와 적의 소품은 같은 그림을 사용해도 음원 재생기를 만들지 않는다.

- 아홉 음색은 런타임에 합성하는 24 kHz 모노 PCM이다. 첫 플레이어가 준비한 클립을 공유하고 마지막 플레이어가 사라지면 해제한다. 재생기는 플레이어마다 루프 전환용 두 개, 짧은 효과용 두 개를 재사용한다.
- `Resources/Audio/FieldFeedback`의 `effects` 믹서 그룹과 `effectsVolume`을 적용한다. 기본값 0.22에서 견인 루프의 소스 음량은 0.132, 스윙은 약 0.073, 체결은 약 0.255다. 음상은 화면 중앙에 둔다.
- Unity 보이스 우선순위는 보스 경고 32, 플러그 단발음 108, 지속음 142다. 보스 전조에서 BGM을 낮추는 시점과 길이는 기존 `AncientAudio`가 관리한다. 플러그 연속 사용 중에도 배경음악의 레벨이 일정하게 유지된다.
- 게임 일시정지, 전체 음소거, 창 포커스 상실, 플레이어 비활성화 시 재생을 중단한다. 조작으로 돌아오면 현재 상태의 지속음부터 이어진다.

## 대체 음색 제작용 프롬프트

샘플로 교체할 때 쓸 수 있는 영문 효과음 지시문이다. 건조한 모노 WAV로 받아 시작 지연과 잔향을 먼저 자르고 위 표의 길이에 맞춘다. 루프 네 개는 끝과 처음의 주기까지 맞춘다.

| 대상 | 프롬프트 |
|---|---|
| 회전 | Compact electric reel turning under a gloved hand, four soft pulses per second, warm copper motor, seamless mono loop, dry, no music. |
| 완충 | Two crisp rising filament pings, second ping a fifth higher and 33 milliseconds later, short dry electronic equipment cue. |
| 투척 | Fast release of a heavy two-pin cable plug, short mechanical snap into a tight air swish, dry centered mono transient. |
| 케이블 풀림 | Thin insulated cable paying out from a spinning metal reel, light bearing chatter over a low axle tone, seamless close mono loop. |
| 체결 | Two metal pins seat in a keyed socket, one close latch click followed immediately by a tiny electrical crackle, dry mono. |
| 견인·스윙 | Compact powered winch under load, low resonant motor and taut cable overtones, steady seamless loop, mono, pitchable. |
| 견인 도착 | Winch reaches a buffered mechanical end stop, compact low thud followed by a short metal housing tick, dry mono. |
| 되감기 | Small cable reel retracting rapidly, inward ratchet flutter, lighter tooth buzz with a steady motor, seamless mono loop. |
| 재장전 | Short double click as a two-pin plug returns to its grip, soft mechanical catch followed by the seating pin, dry mono. |

## 청음 점검

1. 빨래골목에서 짧게 누른 뒤 던지고, 다시 길게 눌러 던진다. 완충을 유지하는 동안 높은 두 음이 한 차례만 울리는지 듣는다.
2. 콘센트에 꽂고 몇 초 기다린 뒤 **C**로 견인한다. 체결 직후의 정적과 견인 도착의 낮은 충격이 구별되어야 한다. 이번에는 **V**로 스윙해 낮아진 윈치음과 연결 해제 후 재장전음을 듣는다.
3. 빗나간 투척을 반복하면서 되감기 끝에 한 번씩 걸쇠가 닫히는지 확인한다. 회전 중 음소거, 일시정지, 창 전환, 방 재시도를 차례로 시험해 이전 상태의 음이 남는지 듣는다.
4. 보스전 BGM을 켜고 경고음과 견인 타이밍을 겹쳐 본다. 작은 스피커에서도 공격 전조가 들리고, 소품이나 보스가 만든 플러그를 보는 것만으로는 회전음이 추가되지 않아야 한다.
