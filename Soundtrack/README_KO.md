# 도시의 악보 · PLUGIN 1회차 사운드트랙

[보스 하이라이트](City-Score-Preview.mp3) · [메인 테마](Main-Theme.mp3) · [이동·추격 하이라이트](Field-Score-Preview.mp3) · [Unity 프로젝트](../AncientCivilization)

보스 여섯 명에게 각각 두 마디 주제를 썼다. 조커의 기타는 다섯 음을 끊어 치고 다음 마디 첫 박을 비운다. 비서실장의 피아노는 16분음표를 3·3·2로 묶고 비브라폰이 아래로 답한다. 거대 슈트는 낮은 기타와 베이스가 킥과 같이 멎고, 집행관은 금관과 기타가 짧은 문답을 주고받는다. 운송대장의 기타에는 오르간이, 코어 보안 병기의 상승하는 현악에는 건반이 응답한다.

50곡을 한 번씩 들으면 **31분 45초**다. 완성 믹스는 44.1 kHz 스테레오 Ogg이며, 41곡은 반복, 9곡은 단발이다. ZIP에서 `City-Score.m3u8`을 열면 게임 순서대로 재생할 수 있다. [플레이리스트](City-Score.m3u8)

## 같은 박에서 함께 연주하기

메인 테마와 탐험·추격곡은 원곡 MIDI에서 주선율을 남기고 반주를 다시 썼다. 피아노와 기타가 동시에 빈틈을 채우던 구절에서는 한쪽이 선율에 답하고 다른 쪽은 킥과 베이스가 짚는 박을 받친다. 같은 음을 두 번 연주하던 보조 베이스는 덜어냈다. 쉬는 장면과 짧은 신호음은 각각의 호흡을 유지한다.

전투곡 15개는 원래 녹음된 악기군 스템에서 출발한다. 주선율을 맡는 악기와 받아치는 반주가 구절 안에서 자리를 바꾸고, 낮은 박을 짚는 악기들이 함께 쉬었다 들어온다. 게임용 완성 파일은 `Mixes/`에 들어 있다. `MIDI/`와 `Stems/`에는 편곡의 재료가 들어 있다. [다섯 층 같은 마디 전후 비교](Patrol-Rhythm-AB.mp3)

## 악기가 선율을 넘기는 순간

세 구절을 같은 청취 음량으로 맞춰 이전판과 수정판 순서로 담았다. 비서실장 구절은 실제 연속 재생처럼 곡 끝에서 첫 마디로 넘어간다. [44초 전후 비교 MP3](Before-After.mp3)

| 곡 | 이전판 | 수정판 | 들어볼 곳 |
|---|---:|---:|---|
| 점등 | 0:00.00 | 0:11.56 | 11~14마디. 대금에서 건반으로 선율을 넘길 때 다음 음이 들어오는 순간 |
| 장부를 펴는 아침 | 0:23.38 | 0:29.36 | 15~17마디. 앞 구절의 마지막 음을 이어받는 기타 |
| 영점 결재 | 0:35.60 | 0:39.97 | 마지막 두 마디와 첫 두 마디. 되돌아올 때 14반음이던 도약을 2반음으로 좁힌 종지 |

메인 테마와 탐험곡은 같은 구절 안에서 다음 음까지 남기는 틈을 악기별로 조절했다. 보스곡은 응답 선율과 종지를 각각 다시 썼다. 조커의 끊어 치는 다섯 발과 장전 전의 쉼은 여전히 전투의 박자를 만든다.

[곡별 편성·음역·프롬프트](CUE_SHEET_KO.md) · [전투 진행과 음악 연동](ADAPTIVE_KO.md)

## 메인 테마와 타이틀

| 순서 | 곡 | 장면 | BPM | 길이 | 재생 | 악보 |
|---:|---|---|---:|---:|---|---|
| 01 | [봉인된 하늘 너머](Mixes/main-theme.ogg) | 첫 타이틀과 백 경위의 메인 테마 | 204 | 1:01 | 루프 | [MIDI](MIDI/main-theme.mid) |
| 02 | [점등](Mixes/ignition-title.ogg) | 게임 중 복귀한 메인 메뉴 | 85 | 1:30 | 루프 | [MIDI](MIDI/ignition-title.mid) |

## 하층 생산구와 첫 추격

| 순서 | 곡 | 장면 | BPM | 길이 | 재생 | 악보 |
|---:|---|---|---:|---:|---|---|
| 03 | [장부를 펴는 아침](Mixes/morning-inventory.ogg) | 생산구 출근길과 코어 재고 확인 | 126 | 0:46 | 루프 | [MIDI](MIDI/morning-inventory.mid) |
| 04 | [교대 전류](Mixes/shift-current.ogg) | 생산구 직선 돌진 | 220 | 0:52 | 루프 | [MIDI](MIDI/shift-current.mid) |
| 05 | [열기 위의 정비로](Mixes/heat-riser.ogg) | 생산구 상부 선택 와이어 경로 | 216 | 0:27 | 루프 | [MIDI](MIDI/heat-riser.mid) |
| 06 | [굴절 회로](Mixes/prism-circuit.ogg) | 수광부와 반사 장치를 정렬해 전력 복구 | 132 | 0:44 | 루프 | [MIDI](MIDI/prism-circuit.mid) |
| 07 | [필라멘트](Mixes/filament.ogg) | 정밀 앵커 구간 | 158 | 0:49 | 루프 | [MIDI](MIDI/filament.mid) |
| 08 | [벽 너머의 시동](Mixes/breach-alarm.ogg) | 재고 보고 도중 벽이 뚫리고 트럭이 출발하는 컷신 | 204 | 0:14 | 단발 | [MIDI](MIDI/breach-alarm.mid) |
| 09 | [화물 차선을 비워](Mixes/truck-pursuit.ogg) | 생산구 첫 오토바이 추격과 트럭 접근 | 218 | 0:35 | 루프 | [MIDI](MIDI/truck-pursuit.mid) |
| 10 | [짐칸 위의 결투](Mixes/roof-captain.ogg) | 트럭 지붕 위에서 대장과 맞붙는 전투 | 212 | 0:27 | 루프 | [MIDI](MIDI/roof-captain.mid) |
| 11 | [두 번째 점화](Mixes/redline-remount.ogg) | 다시 오토바이에 올라 트럭을 쫓는 구간 | 236 | 0:33 | 루프 | [MIDI](MIDI/redline-remount.mid) |
| 12 | [검거 신호](Mixes/cargo-verdict.ogg) | 짐칸 절단, 공중 레이저, 트럭 폭파와 생존 검거 | 198 | 0:19 | 단발 | [MIDI](MIDI/cargo-verdict.mid) |

## 하층 생활구와 거대 슈트

| 순서 | 곡 | 장면 | BPM | 길이 | 재생 | 악보 |
|---:|---|---|---:|---:|---|---|
| 13 | [빨랫줄 너머](Mixes/alley-trail.ogg) | 하층 생활구 골목, 도둑의 행적을 듣고 다음 표식을 찾아가는 길 | 136 | 0:42 | 루프 | [MIDI](MIDI/alley-trail.mid) |
| 14 | [폐차장 공전](Mixes/salvage-orbit.ogg) | 생활구 폐차장에서 와이어로 물체를 돌려 던지기 | 138 | 0:42 | 루프 | [MIDI](MIDI/salvage-orbit.mid) |
| 15 | [불빛이 지나는 자리](Mixes/spotlight-grid.ogg) | 보안 외벽, 탐조등이 지나간 뒤 골목 사이를 이동하는 구간 | 154 | 0:37 | 루프 | [MIDI](MIDI/spotlight-grid.mid) |
| 16 | [철문 두 번](Mixes/warehouse-crew.ogg) | 범죄 조직의 창고, 거대 슈트 투입 전 조직원전 | 208 | 0:28 | 루프 | [MIDI](MIDI/warehouse-crew.mid) |
| 17 | [리벳의 맥박 I · 기동](Mixes/iron-suit-1.ogg) | 거대 슈트 1페이즈, 총격과 유도 로켓 주먹 | 208 | 0:37 | 루프 | [MIDI](MIDI/iron-suit-1.mid) |
| 18 | [리벳의 맥박 II · 압력실](Mixes/iron-suit-2.ogg) | 거대 슈트 2페이즈, 배리어 접근과 후방 포격 | 228 | 0:34 | 루프 | [MIDI](MIDI/iron-suit-2.mid) |
| 19 | [리벳의 맥박 III · 푸른 균열](Mixes/iron-suit-3.ogg) | 거대 슈트 3페이즈, 불완전한 코어와 X 블래스트 | 250 | 0:31 | 루프 | [MIDI](MIDI/iron-suit-3.mid) |

## 휴식과 진행 신호

| 순서 | 곡 | 장면 | BPM | 길이 | 재생 | 악보 |
|---:|---|---|---:|---:|---|---|
| 20 | [교대실의 등](Mixes/service-lantern.ogg) | 작은 휴게실에서 저장과 정비 | 78 | 0:49 | 루프 | [MIDI](MIDI/service-lantern.mid) |
| 21 | [저장등](Mixes/checkpoint.ogg) | 체크포인트 점등 | 112.5 | 0:03 | 단발 | [MIDI](MIDI/checkpoint.mid) |
| 22 | [소등](Mixes/death.ogg) | 사망과 즉시 재도전 | 90 | 0:04 | 단발 | [MIDI](MIDI/death.mid) |
| 23 | [출입 승인](Mixes/rank-access.ogg) | 승진과 새 층 출입 권한 | 108 | 0:10 | 단발 | [MIDI](MIDI/rank-access.mid) |
| 24 | [층과 층 사이](Mixes/lift-between.ogg) | 승강기 이동 | 78 | 1:02 | 루프 | [MIDI](MIDI/lift-between.mid) |

## 중층 행정구와 기록 집행관

| 순서 | 곡 | 장면 | BPM | 길이 | 재생 | 악보 |
|---:|---|---|---:|---:|---|---|
| 25 | [창마다 다른 번호](Mixes/numbered-facades.ogg) | 중층 행정구 · 민원 통로와 기록동 | 140 | 0:55 | 루프 | [MIDI](MIDI/numbered-facades.mid) |
| 26 | [셔터 사이의 색인](Mixes/shutter-index.ogg) | 중층 행정구 · 보안 셔터 난관 | 162 | 0:36 | 루프 | [MIDI](MIDI/shutter-index.mid) |
| 27 | [장부의 눌린 자국](Mixes/ledger-imprint.ogg) | 행정구에서 두 기록의 원본 대조 | 87 | 0:44 | 루프 | [MIDI](MIDI/ledger-imprint.mid) |
| 28 | [검문소 사이](Mixes/witness-crossing.ogg) | 주안과 함께 무전을 들으며 통제선을 통과 | 152 | 0:38 | 루프 | [MIDI](MIDI/witness-crossing.mid) |
| 29 | [봉쇄선](Mixes/sealed-gate.ogg) | 보스 직전 대기 | 85 | 0:34 | 루프 | [MIDI](MIDI/sealed-gate.mid) |
| 30 | [집행 개시 Ⅰ](Mixes/enforcer-1.ogg) | 중층 행정구 · 기록 집행관 | 214 | 0:36 | 루프 | [MIDI](MIDI/enforcer-1.mid) |
| 31 | [집행 개시 Ⅱ](Mixes/enforcer-2.ogg) | 중층 행정구 · 기록 집행관 | 232 | 0:33 | 루프 | [MIDI](MIDI/enforcer-2.mid) |
| 32 | [집행 개시 Ⅲ](Mixes/enforcer-3.ogg) | 중층 행정구 · 기록 집행관 | 252 | 0:30 | 루프 | [MIDI](MIDI/enforcer-3.mid) |
| 33 | [철수](Mixes/retreat.ogg) | 제압 후 생존 퇴장 | 90 | 0:05 | 단발 | [MIDI](MIDI/retreat.mid) |

## 상층 거주구와 코어 보안 병기

| 순서 | 곡 | 장면 | BPM | 길이 | 재생 | 악보 |
|---:|---|---|---:|---:|---|---|
| 34 | [온실 너머의 오후](Mixes/glass-terrace.ogg) | 상층 거주구 · 유리 테라스와 주민 구역 | 110 | 1:10 | 루프 | [MIDI](MIDI/glass-terrace.mid) |
| 35 | [유리 아래의 상승류](Mixes/greenhouse-fault.ogg) | 상층 거주구 · 환기 상승류와 부유체 난관 | 172 | 0:33 | 루프 | [MIDI](MIDI/greenhouse-fault.mid) |
| 36 | [진열장 속 심장 Ⅰ · 계측](Mixes/residence-warden-1.ogg) | 상층 거주구 · 코어 보안 병기 전투 편곡 Ⅰ | 210 | 0:27 | 루프 | [MIDI](MIDI/residence-warden-1.mid) |
| 37 | [진열장 속 심장 Ⅱ · 굴절](Mixes/residence-warden-2.ogg) | 상층 거주구 · 코어 보안 병기 전투 편곡 Ⅱ | 230 | 0:33 | 루프 | [MIDI](MIDI/residence-warden-2.mid) |
| 38 | [진열장 속 심장 Ⅲ · 파쇄](Mixes/residence-warden-3.ogg) | 상층 거주구 · 코어 보안 병기 전투 편곡 Ⅲ | 250 | 0:31 | 루프 | [MIDI](MIDI/residence-warden-3.mid) |

## 최상층과 조커·비서실장

| 순서 | 곡 | 장면 | BPM | 길이 | 재생 | 악보 |
|---:|---|---|---:|---:|---|---|
| 39 | [도시의 뒷면](Mixes/summit-trespass.ogg) | 최상층 정비 외벽과 우회 통로 | 184 | 0:37 | 루프 | [MIDI](MIDI/summit-trespass.mid) |
| 40 | [숨을 세는 눈](Mixes/joker-sightline.ogg) | 저격수 · 본체의 조준과 장전 | 204 | 0:33 | 루프 | [MIDI](MIDI/joker-sightline.mid) |
| 41 | [유리 속의 둘](Mixes/joker-double.ogg) | 저격수 · 한 명의 분신과 교차 사격 | 222 | 0:35 | 루프 | [MIDI](MIDI/joker-double.mid) |
| 42 | [바닥 아래의 포좌](Mixes/joker-turrets.ogg) | 저격수 · 분신과 포탑 탈취 | 238 | 0:32 | 루프 | [MIDI](MIDI/joker-turrets.mid) |
| 43 | [두 빛의 접점](Mixes/joker-beam.ogg) | 저격수 · 충전과 최후의 광선 대결 | 258 | 0:22 | 루프 | [MIDI](MIDI/joker-beam.mid) |
| 44 | [초침이 닿기 전](Mixes/clock-ante.ogg) | 비서실장 집무실 문 앞 | 92 | 0:52 | 루프 | [MIDI](MIDI/clock-ante.mid) |
| 45 | [영점 결재](Mixes/clock-secretary.ogg) | 비서실장의 시계 · 1페이즈 | 234 | 0:33 | 루프 | [MIDI](MIDI/clock-secretary.mid) |

## 회수 계획, 마지막 추격과 엔딩

| 순서 | 곡 | 장면 | BPM | 길이 | 재생 | 악보 |
|---:|---|---|---:|---:|---|---|
| 46 | [결재자 부재](Mixes/vacant-office.ogg) | 빈 회장실의 노트와 회수 계획 | 83 | 1:09 | 루프 | [MIDI](MIDI/vacant-office.mid) |
| 47 | [회수 명령까지 60초](Mixes/sixty-to-recovery.ogg) | 회장의 노트를 펼친 뒤 코어 회수 작전이 시작되기까지 | 120 | 1:00 | 단발 | [MIDI](MIDI/sixty-to-recovery.mid) |
| 48 | [아래로 달리는 불빛](Mixes/descent-convoy.ogg) | 최상층에서 최하층까지 마지막 트럭 추격 | 248 | 0:35 | 루프 | [MIDI](MIDI/descent-convoy.mid) |
| 49 | [단 한 발의 공명](Mixes/core-ignition.ogg) | 저격수가 경위를 구하고 코어가 폭발해 추락한다 | 104 | 0:28 | 단발 | [MIDI](MIDI/core-ignition.mid) |
| 50 | [하늘 아래의 하늘](Mixes/first-ending.ogg) | 최하층 아래로 떨어진 뒤 이어지는 1회차 크레디트 | 98 | 1:28 | 단발 | [MIDI](MIDI/first-ending.mid) |

## 수록 파일과 음색

`Mixes/`와 `MIDI/`에는 같은 이름의 50곡이 있다. `Stems/`에는 15개 전투곡의 악기군별 음원, `Accents/`에는 피아노 전환음과 장비 신호 5개가 들어 있다. `LoopParts/`에는 〈교대 전류〉의 4마디 도입과 44마디 반복부를 따로 담았다.

보스 15곡은 기존 FluidR3 GM 연주에 TimGM6mb로 연주한 새 주제와 응답, 직접 합성한 드럼과 기타·베이스를 더했다. 나머지 35곡은 편곡한 MIDI를 TimGM6mb와 합성 악기로 연주했다. 스테레오 위치, 기타와 베이스의 음역, 타악기 어택, 악기들이 공유하는 방의 반사음을 렌더링할 때 결정한다. 게임에는 완성된 Ogg가 들어간다. [원본 녹음 크레딧](FluidR3-Credits.txt) · [재연주 음원 크레딧](TimGM-Credits.txt)

편곡을 다시 렌더할 때는 저장소 루트에서 아래 순서로 실행한다. 예제의 `/tmp/city-ensemble`은 작업용 출력 폴더다. Python 3, NumPy, SciPy, FFmpeg, libfluidsynth와 TimGM6mb가 필요하다. 다른 위치에 설치한 사운드폰트는 `render_field.py --soundfont`로 지정한다.

```sh
python3 Soundtrack/Tools/ensemble_score.py --output /tmp/city-ensemble
python3 Soundtrack/Tools/render_field.py --midi /tmp/city-ensemble/midi --roles /tmp/city-ensemble/roles --output /tmp/city-ensemble/masters
python3 Soundtrack/Tools/render_boss_ensemble.py --output /tmp/city-ensemble/masters
python3 AncientCivilization/Tools/check_score_pcm.py --music /tmp/city-ensemble/masters
```
