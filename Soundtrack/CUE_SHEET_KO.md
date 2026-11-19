# 제작용 큐시트 · 국악 퓨전 필드 스코어

전체 50곡의 편곡과 재생 기준이다. 영어 두 문장은 같은 역할의 곡을 새로 생성할 때 스타일 칸에 붙여 넣을 수 있다. 각 곡의 음원·MIDI가 이 편곡의 기준이다.

P0는 먼저 플레이테스트할 대표 테마·이동·전투 신호, P1은 1회차 필수 장면, P2는 선택 경로와 재방문 공간이다. BPM 앞 수치는 음원의 고정 템포이고 괄호 안은 새 편곡을 시험할 때의 권장 범위다. 마디는 모두 4/4이다. 구획은 DAW에서 비교 청취할 편집 구간이다. 숫자로 적은 음역은 해당 트랙의 MIDI 최저·최고 음높이이며 타악기를 제외한다.

## 소재와 장단 운용

| 재료 | 연주·믹스 방향 |
|---|---|
| 장구와 록 백비트 | 장구의 낮은 편과 높은 편이 킥·스네어의 빈자리를 받는다. 특정 장단을 그대로 옮겼다는 표기 없이 4/4 위에서 3+3+2, 셔플, 셋잇단음 등 실제 곡의 세분을 설명한다. |
| 대금·피리·태평소 | 숨과 리드가 들어올 때 기타·스트링의 같은 음역을 비운다. 태평소는 종결부 몇 마디에 두면 장기전에서도 고음 피로가 덜하다. |
| 가야금·거문고·해금·아쟁 | 뜯는 소리는 피아노의 건반을 피해서 당김음에 두고, 활은 짧은 어택 뒤를 이어 긴 음을 맡긴다. |
| 전구·전기·유리 | 세기가 약한 유리 배음과 짧은 코일음을 박 끝의 대답으로 배치한다. 경고음과 맞닿으면 고역 장식부터 줄인다. |

## 제작·게임 연결 순서

첫 이동과 난관을 먼저 이어 듣고, 집행관 세 페이즈의 모티프가 템포가 달라져도 들리는지 시험한다. 이어 화면에 동작이 보이는 추격·컷신, 지역 루프, 저장·퇴장 신호를 맞춘다. 상태 전환 시 서로 다른 템포의 마디를 동시에 포개는 대신 이전 루프의 경계에서 다음 곡의 첫 박을 DSP 시간으로 예약한다. 피격·돌진·체크포인트의 판정과 효과음은 입력을 기다리게 하지 않고 즉시 재생한다. 저체력 필터는 BGM 버스에, 경고는 별도 버스에 둔다. 충전 및 블레이드 투척에 따른 레이어 변경은 분리 스템이 있는 곡에서만 적용하고 복귀 시간은 짧은 크로스페이드로 시험한다.

## 01 · 봉인된 하늘 너머

**P0 · 백 경위의 메인 테마**
168 BPM (재편곡 163–173) · B단조 · D장조 · 52마디 · 74.29초 · 마디 경계 루프

편곡 흐름 — 독주 서두 → 주제·응답 → 숨 고르기 → 합주 재현.

편집 구획(마디) — 1–4 / 5–12 / 13–20 / 21–28 / 29–44 / 45–52.

편성 — 피아노, 보조 피아노, 가야금, 드럼 키트, 리드 일렉기타, 베이스, 리듬 일렉기타, 장구, 클린 기타, 대금, 첼로, 합주 현.

주요 파트 실음역(MIDI) — 피아노 52–81, 가야금 69–76, 리드 일렉기타 62–86, 베이스 30–52.

**짧은 스타일** (121자)

```text
168 BPM, determined officer theme, lyrical keys, electric guitar and breathing room, gayageum, janggu, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 168 BPM in 4/4. Give the officer a theme playable on a single keyboard before increasing the band. Leave room for a breath-led answer and bring back the hook in a wider register. Feature gayageum (finger-plucked strings with a bending tail); janggu (contrasting low and high hourglass-drum heads); daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 02 · 점등

**P0 · 타이틀 화면**
85 BPM (재편곡 80–90) · E단조와 G장조 · 32마디 · 90.35초 · 마디 경계 루프

편곡 흐름 — 피아노 독주 → 대금과 기타의 대답 → 현 브리지 → 발광 잔향.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 대금, 보조 피아노, 가야금, 피아노, 해금, 코일음, 합주 현, 첼로, 베이스, 클린 기타, 드럼 키트, 패드, 유리음.

주요 파트 실음역(MIDI) — 대금 66–83, 가야금 52–76, 피아노 63–79, 해금 52–59.

**짧은 스타일** (123자)

```text
85 BPM, intimate piano, airy Korean bamboo flute, delayed clean guitar, warm strings, daegeum, gayageum, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 85 BPM in 4/4. Open on one exposed piano note, let a breathy bamboo flute finish the rising phrase, then leave the unresolved glass overtone hanging over the menu. Feature daegeum (breathy transverse bamboo flute with a buzzing membrane); gayageum (finger-plucked strings with a bending tail); haegeum (narrow two-string bowed tone and slow vibrato). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 03 · 장부를 펴는 아침

**P1 · 생산구 출근길과 코어 재고 확인**
120 BPM (재편곡 115–125) · D장조 · 리디아 색채 · 24마디 · 48.00초 · 마디 경계 루프

편곡 흐름 — 밝은 출근 선율 → 대금·클린 기타 문답 → 작업장 리듬 복귀.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 피아노, 클린 기타, 가야금, 유리음, 베이스, 드럼 키트, 대금, 합주 현.

주요 파트 실음역(MIDI) — 피아노 66–78, 클린 기타 54–73, 가야금 64–69, 베이스 38–59.

**짧은 스타일** (107자)

```text
120 BPM, bright workday piano, plucked zither replies, clean guitar and flute, gayageum, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 120 BPM in 4/4. Walk the keyboard melody up through a raised fourth. Let the plucked instrument finish alternate bars, as if one worker had answered another across a belt. Feature gayageum (finger-plucked strings with a bending tail); daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 04 · 교대 전류

**P0 · 생산구 직선 돌진**
175 BPM (재편곡 170–180) · E단조 → G장조 → A장조 · 48마디 · 65.83초 · 마디 경계 루프

편곡 흐름 — 피아노·가야금 문답 → 기타 주제 → 중간 감편 → 합주 재현.

편집 구획(마디) — 1–4 / 5–8 / 9–20 / 21–28 / 29–44 / 45–48.

편성 — 피아노, 가야금, 피크 베이스, 드럼 키트, 장구, 코일음, 클린 기타, 첼로, 유리음, 리드 일렉기타, 합주 현, 리듬 일렉기타, 금관.

주요 파트 실음역(MIDI) — 피아노 45–81, 가야금 52–85, 피크 베이스 32–56, 클린 기타 54–69.

**짧은 스타일** (113자)

```text
175 BPM, rapid piano and zither relays, bright electric guitar, agile kit and janggu, gayageum, janggu, no vocals
```

**긴 스타일**

```text
Instrumental game score, 175 BPM in 4/4. Split the running hook between the piano attack and a plucked zither. Give the guitar a separate soaring phrase and let a drum fill hand its final stroke to the hourglass drum. Feature gayageum (finger-plucked strings with a bending tail); janggu (contrasting low and high hourglass-drum heads). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 05 · 열기 위의 정비로

**P2 · 생산구 상부 선택 와이어 경로**
172 BPM (재편곡 167–177) · E 도리안 · 24마디 · 33.49초 · 마디 경계 루프

편곡 흐름 — 열기 속 상행 → 리듬 완충 → 높은 길 재진입.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 피아노, 클린 기타, 피크 베이스, 드럼 키트, 리드 일렉기타, 장구, 대금, 합주 현.

주요 파트 실음역(MIDI) — 피아노 64–83, 클린 기타 54–66, 피크 베이스 38–59, 리드 일렉기타 66–81.

**짧은 스타일** (117자)

```text
172 BPM, rising wire-route theme, open fifth leaps, angled guitar chords and nimble drums, janggu, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 172 BPM in 4/4. Let the head jump from an open fifth to a ninth while the updraft carries the line. Keep the feet audible through the shimmer of the higher guitar voicing. Feature janggu (contrasting low and high hourglass-drum heads); daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 06 · 굴절 회로

**P2 · 수광부와 반사 장치를 정렬해 전력 복구**
125 BPM (재편곡 120–130) · B장조 · 리디아 색채 · 24마디 · 46.08초 · 마디 경계 루프

편곡 흐름 — 빛의 원형 → 반사된 음형 → 합쳐지는 선율.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 피아노, 하프, 가야금, 유리음, 베이스, 드럼 키트, 코일음, 클린 기타, 신스.

주요 파트 실음역(MIDI) — 피아노 71–85, 하프 71–85, 가야금 71–83, 베이스 35–44.

**짧은 스타일** (106자)

```text
125 BPM, reflection puzzle, mirrored piano figure, plucked zither and glass overtones, gayageum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 125 BPM in 4/4. Reflect a short line around one shared pitch. Hand its last attack from the keyboard to plucked strings so the ear can follow the ray across the room. Feature gayageum (finger-plucked strings with a bending tail). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 07 · 필라멘트

**P0 · 정밀 앵커 구간**
150 BPM (재편곡 145–155) · C♯단조와 낮은 2도 · 32마디 · 51.20초 · 마디 경계 루프

편곡 흐름 — 짧은 피아노 오스티나토 → 활의 긴 음 → 바짝 마른 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 가야금, 해금, 신스, 유리음, 드럼 키트, 북, 코일음, 피아노, 베이스.

주요 파트 실음역(MIDI) — 가야금 51–73, 해금 68–78, 신스 32–49, 피아노 51–74.

**짧은 스타일** (127자)

```text
150 BPM, precision platforming pulse, staccato keys, bowed replies, narrow stereo percussion, gayageum, haegeum, buk, no vocals
```

**긴 스타일**

```text
Instrumental game score, 150 BPM in 4/4. Pin one high pilot tone above a moving bass. Vary the bow length in the response, with enough silence to hear the anchor lock. Feature gayageum (finger-plucked strings with a bending tail); haegeum (narrow two-string bowed tone and slow vibrato); buk (round low barrel-drum stroke). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 08 · 벽 너머의 시동

**P1 · 재고 보고 도중 벽이 뚫리고 트럭이 출발하는 컷신**
152 BPM (동일 길이 동기용 고정) · C♯ 프리지안 · 12마디 · 18.95초 · 1회 재생

편곡 흐름 — 점검 신호 → 반음 하강 → 벽 붕괴 → 출발.

편집 구획(마디) — 1–4 / 5–8 / 9–12.

편성 — 코일음, 피아노, 합주 현, 드럼 키트, 북, 금관, 신스, 피리, 리듬 일렉기타, 베이스.

주요 파트 실음역(MIDI) — 피아노 49–68, 합주 현 62–73, 금관 37–62, 신스 32–50.

**짧은 스타일** (106자)

```text
152 BPM, cinematic cut cue, clipped electrical pulse, stretching reed cry and impact, buk, piri, no vocals
```

**긴 스타일**

```text
Instrumental game score, 152 BPM in 4/4. Start with the regularity of an inspection signal. Flatten its last pitch by a semitone, stretch three short calls into a long cry, then leave the break in the wall audible. Feature buk (round low barrel-drum stroke); piri (compact, penetrating double reed). Reach a definite ending and leave a short natural decay. No vocals.
```

## 09 · 화물 차선을 비워

**P1 · 생산구 첫 오토바이 추격과 트럭 접근**
168 BPM (재편곡 163–173) · D 도리안 · 32마디 · 45.71초 · 마디 경계 루프

편곡 흐름 — 주행 리프 → 승선이 읽히는 공간 → 추진력 복귀.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 피아노, 피크 베이스, 리듬 일렉기타, 드럼 키트, 장구, 리드 일렉기타, 태평소, 합주 현.

주요 파트 실음역(MIDI) — 피아노 57–79, 피크 베이스 33–55, 리듬 일렉기타 45–62, 리드 일렉기타 67–81.

**짧은 스타일** (108자)

```text
168 BPM, motorcycle chase, up-picked guitar, hard drums, short shawm answers, janggu, taepyeongso, no vocals
```

**긴 스타일**

```text
Instrumental game score, 168 BPM in 4/4. Push the bass ahead of the downbeat while the guitar plays upward strokes. Let the shawm replace a phrase instead of competing with the motorcycle and the drums. Feature janggu (contrasting low and high hourglass-drum heads); taepyeongso (bright conical shawm with a projecting bell). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 10 · 짐칸 위의 결투

**P1 · 트럭 지붕 위에서 대장과 맞붙는 전투**
154 BPM (재편곡 149–159) · F♯단조 · 24마디 · 37.40초 · 마디 경계 루프

편곡 흐름 — 교차 악센트 → 중간 문답 → 갑판 리프 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 리듬 일렉기타, 베이스, 거문고, 드럼 키트, 리드 일렉기타, 유리음, 해금, 피아노, 금관.

주요 파트 실음역(MIDI) — 리듬 일렉기타 49–64, 베이스 37–45, 거문고 61–69, 리드 일렉기타 61–72.

**짧은 스타일** (112자)

```text
154 BPM, close-quarters roof fight, syncopated riffs, dry accents, compact low end, geomungo, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 154 BPM in 4/4. Give the duel a foot-to-foot rhythm with sudden rests in the third bar of the phrase. Use the gap to frame the captain's next move. Feature geomungo (low zither snapped with a short stick); haegeum (narrow two-string bowed tone and slow vibrato). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 11 · 두 번째 점화

**P1 · 다시 오토바이에 올라 트럭을 쫓는 구간**
184 BPM (재편곡 179–189) · A단조 · 32마디 · 41.74초 · 마디 경계 루프

편곡 흐름 — 3+3+2 주행 악센트 → 감편 → 재탑승 후 합주.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 신스, 피크 베이스, 피아노, 리듬 일렉기타, 드럼 키트, 가야금, 꽹과리, 리드 일렉기타, 합주 현.

주요 파트 실음역(MIDI) — 신스 33–57, 피크 베이스 33–45, 피아노 69–84, 리듬 일렉기타 45–64.

**짧은 스타일** (118자)

```text
184 BPM, accelerating road cue at fixed tempo, picked bass, engine-like syncopation, gayageum, kkwaenggwari, no vocals
```

**긴 스타일**

```text
Instrumental game score, 184 BPM in 4/4. Build apparent speed with interlocking 3+3+2 eighths against a four-beat kit. Drop the upper kit in the underpass and bring it back with a higher guitar register. Feature gayageum (finger-plucked strings with a bending tail); kkwaenggwari (small sharp brass gong with a short choke). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 12 · 검거 신호

**P1 · 짐칸 절단, 공중 레이저, 트럭 폭파와 생존 검거**
144 BPM (동일 길이 동기용 고정) · D단조 → D장조 · 16마디 · 26.67초 · 1회 재생

편곡 흐름 — 나란히 주행 → 절단 → 체공 → 방전 → 검거.

편집 구획(마디) — 1–4 / 5–7 / 8–10 / 11–12 / 13–16.

편성 — 베이스, 드럼 키트, 합주 현, 리드 일렉기타, 피아노, 장구, 대금, 하프, 코일음, 금관, 유리음.

주요 파트 실음역(MIDI) — 베이스 38–50, 합주 현 54–70, 리드 일렉기타 69–81, 피아노 50–74.

**짧은 스타일** (128자)

```text
144 BPM, through-composed chase finisher, clipped blade accents, brief suspension, bright resolution, janggu, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 144 BPM in 4/4. Score wheel alignment first, cut sharply to a shearing accent, suspend the harmony in midair, then spend the cadence on the recovered cargo. Feature janggu (contrasting low and high hourglass-drum heads); daegeum (breathy transverse bamboo flute with a buzzing membrane). Reach a definite ending and leave a short natural decay. No vocals.
```

## 13 · 빨랫줄 너머

**P1 · 하층 생활구 골목, 도둑의 행적을 듣고 다음 표식을 찾아가는 길**
126 BPM (재편곡 121–131) · E Dorian / E minor · 24마디 · 45.71초 · 마디 경계 루프

편곡 흐름 — 8마디 골목 걸음 → 대금이 넓히는 중간부 → 가야금 대답과 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 클린 기타, 피아노, 베이스, 가야금, 드럼 키트, 유리음, 대금.

주요 파트 실음역(MIDI) — 클린 기타 64–76, 피아노 54–67, 베이스 35–57, 가야금 66–76.

**짧은 스타일** (113자)

```text
126 BPM, curious urban groove, clean guitar pickup, bamboo flute and plucked zither, gayageum, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 126 BPM in 4/4. Let the melody enter half a beat late like overheard conversation. Change from guitar to breathy flute above a bass line that steps from root to fifth. Feature gayageum (finger-plucked strings with a bending tail); daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 14 · 폐차장 공전

**P2 · 생활구 폐차장에서 와이어로 물체를 돌려 던지기**
114 BPM (재편곡 109–119) · F♯단조 · 펑크 · 24마디 · 50.53초 · 마디 경계 루프

편곡 흐름 — 저음 선회 → 짧은 데드노트 → 한 바퀴 뒤 복귀.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 베이스, 클린 기타, 거문고, 드럼 키트, 신스, 오르간, 금관.

주요 파트 실음역(MIDI) — 베이스 37–57, 클린 기타 53–64, 거문고 61–76, 신스 64–68.

**짧은 스타일** (104자)

```text
114 BPM, junkyard funk, damped clean guitar, syncopated bass and struck bass zither, geomungo, no vocals
```

**긴 스타일**

```text
Instrumental game score, 114 BPM in 4/4. Displace the sixteenths as if a heavy object were being swung around the player. Let a ringing low pluck mark the instant its weight changes direction. Feature geomungo (low zither snapped with a short stick). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 15 · 불빛이 지나는 자리

**P1 · 보안 외벽, 탐조등이 지나간 뒤 골목 사이를 이동하는 구간**
145 BPM (재편곡 140–150) · C minor / Phrygian inflection · 24마디 · 39.72초 · 마디 경계 루프

편곡 흐름 — 3+3+2 탐조등 → 해금의 지속음 → 짧은 피아노 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 피아노, 거문고, 짧은 활 현, 베이스, 드럼 키트, 코일음, 해금, 패드.

주요 파트 실음역(MIDI) — 피아노 63–77, 거문고 44–60, 짧은 활 현 51–61, 베이스 32–43.

**짧은 스타일** (110자)

```text
145 BPM, stealth ostinato, broken 3+3+2 pulse, dry bass zither and bowed tension, geomungo, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 145 BPM in 4/4. Pan the sense of motion through alternating accents, not constant loudness. Stop the low zither just before the dangerous sweep; make the returning piano note unmistakable. Feature geomungo (low zither snapped with a short stick); haegeum (narrow two-string bowed tone and slow vibrato). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 16 · 철문 두 번

**P1 · 범죄 조직의 창고, 거대 슈트 투입 전 조직원전**
158 BPM (재편곡 153–163) · A minor / Dorian lift · 24마디 · 36.46초 · 마디 경계 루프

편곡 흐름 — 셔플 리프 → 피리와 기타 문답 → 셔플 후렴.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 리드 일렉기타, 클린 기타, 피크 베이스, 드럼 키트, 장구, 피리.

주요 파트 실음역(MIDI) — 리드 일렉기타 64–81, 클린 기타 52–60, 피크 베이스 38–57, 피리 65–81.

**짧은 스타일** (106자)

```text
158 BPM, compact warehouse shuffle, snarling guitar, short reed bursts, hand drum, janggu, piri, no vocals
```

**긴 스타일**

```text
Instrumental game score, 158 BPM in 4/4. Swing the pickups over a walking picked bass. Have clean muted chords answer the lead from the other end of the room before the final drum fill. Feature janggu (contrasting low and high hourglass-drum heads); piri (compact, penetrating double reed). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 17 · 리벳의 맥박 I · 기동

**P1 · 거대 슈트 1페이즈, 총격과 유도 로켓 주먹**
160 BPM (재편곡 155–165) · G minor · 32마디 · 48.00초 · 마디 경계 루프

편곡 흐름 — 리벳 주제 → 피리 대답 → 정비 브리지 → 주제 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 리드 일렉기타, 거문고, 피크 베이스, 리듬 일렉기타, 피아노, 드럼 키트, 유리음, 피리, 합주 현.

주요 파트 실음역(MIDI) — 리드 일렉기타 62–79, 거문고 46–62, 피크 베이스 34–55, 리듬 일렉기타 39–57.

**짧은 스타일** (114자)

```text
160 BPM, heavy mechanical riff, double-struck guitar hook, punchy bass and reed replies, geomungo, piri, no vocals
```

**긴 스타일**

```text
Instrumental game score, 160 BPM in 4/4. Attack G twice, vault through B-flat and C to F, then fall through E-flat and D. Allow the rocket to pass through a deliberate gap in the riff. Feature geomungo (low zither snapped with a short stick); piri (compact, penetrating double reed). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 18 · 리벳의 맥박 II · 압력실

**P1 · 거대 슈트 2페이즈, 배리어 접근과 후방 포격**
172 BPM (재편곡 167–177) · G minor · 32마디 · 44.65초 · 마디 경계 루프

편곡 흐름 — 주제 변주 → 아쟁 저음 → 장벽 앞 감편 → 기타 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 피아노, 피크 베이스, 리듬 일렉기타, 합주 현, 드럼 키트, 북, 보조 피아노, 유리음, 꽹과리, 아쟁, 리드 일렉기타.

주요 파트 실음역(MIDI) — 피아노 53–79, 피크 베이스 34–55, 리듬 일렉기타 39–57, 합주 현 54–75.

**짧은 스타일** (120자)

```text
172 BPM, armoured battle groove, irregular guitar jabs, bowed low zither, deep buk, buk, kkwaenggwari, ajaeng, no vocals
```

**긴 스타일**

```text
Instrumental game score, 172 BPM in 4/4. Keep the rivet motif under heavier drums. Stretch the bowed response beneath the shield, then use a single metal accent as the distant artillery returns. Feature buk (round low barrel-drum stroke); kkwaenggwari (small sharp brass gong with a short choke); ajaeng (grainy bowed low zither). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 19 · 리벳의 맥박 III · 푸른 균열

**P1 · 거대 슈트 3페이즈, 불완전한 코어와 X 블래스트**
186 BPM (재편곡 181–191) · A minor · 32마디 · 41.29초 · 마디 경계 루프

편곡 흐름 — A단조 전조 → 간주 → 코어 정적 → 태평소와 합주.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 리드 일렉기타, 피크 베이스, 리듬 일렉기타, 피아노, 합주 현, 금관, 드럼 키트, 북, 가야금, 유리음, 태평소.

주요 파트 실음역(MIDI) — 리드 일렉기타 64–84, 피크 베이스 36–57, 리듬 일렉기타 40–57, 피아노 55–81.

**짧은 스타일** (125자)

```text
186 BPM, unstable core finale, raised guitar hook, Korean shawm, plucked zither sparks, buk, gayageum, taepyeongso, no vocals
```

**긴 스타일**

```text
Instrumental game score, 186 BPM in 4/4. Transpose the seven-note hook to A and let the shawm take just its final declarations. Cut the rising texture short before each giant diagonal discharge. Feature buk (round low barrel-drum stroke); gayageum (finger-plucked strings with a bending tail); taepyeongso (bright conical shawm with a projecting bell). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 20 · 교대실의 등

**P2 · 작은 휴게실에서 저장과 정비**
78 BPM (재편곡 73–83) · A장조 · 6/9화음 · 16마디 · 49.23초 · 마디 경계 루프

편곡 흐름 — 작업등의 첫 울림 → 목관과 기타 대답 → 작은 종지.

편집 구획(마디) — 1–8 / 9–16.

편성 — 피아노, 가야금, 클린 기타, 유리음, 대금, 패드, 첼로.

주요 파트 실음역(MIDI) — 피아노 38–66, 가야금 73–78, 클린 기타 66–76, 대금 66–76.

**짧은 스타일** (112자)

```text
78 BPM, safe-room miniature, warm added-sixth chords, soft zither and bamboo flute, gayageum, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 78 BPM in 4/4. Place one pluck near the left and one guitar harmonic near the right. Let both decay while the centre stays open for equipment and menu sounds. Feature gayageum (finger-plucked strings with a bending tail); daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 21 · 저장등

**P0 · 체크포인트 점등**
112.5 BPM (동일 길이 동기용 고정) · G장조 / E단조 · 1.5마디 · 3.20초 · 1회 재생

편곡 흐름 — 피아노와 가야금 동형 → 한 음의 등불 → 짧은 여운.

편집 구획(마디) — 1–1.5.

편성 — 유리음, 피아노, 하프, 합주 현, 가야금.

주요 파트 실음역(MIDI) — 피아노 59–67, 하프 55–55, 합주 현 71–71, 가야금 59–67.

**짧은 스타일** (95자)

```text
112.5 BPM, brief luminous save chime, plucked Korean zither and soft piano, gayageum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 112.5 BPM in 4/4. Give the pluck and the key the same rising contour, then resolve into a quiet upper harmonic. Keep the finish small enough to sit over footsteps. Feature gayageum (finger-plucked strings with a bending tail). Reach a definite ending and leave a short natural decay. No vocals.
```

## 22 · 소등

**P0 · 사망과 즉시 재도전**
90 BPM (동일 길이 동기용 고정) · E단조 · 1.5마디 · 4.00초 · 1회 재생

편곡 흐름 — 저음 충격 → 유리음 하강 → 아쟁 잔향.

편집 구획(마디) — 1–1.5.

편성 — 보조 피아노, 드럼 키트, 합주 현, 아쟁, 유리음, 피아노.

주요 파트 실음역(MIDI) — 합주 현 66–66, 아쟁 55–55, 피아노 52–54.

**짧은 스타일** (100자)

```text
90 BPM, short defeat sting, muted piano cluster, bowed bass zither, falling glass, ajaeng, no vocals
```

**긴 스타일**

```text
Instrumental game score, 90 BPM in 4/4. Drop a low piano chord and a coarse bowed zither together. Let three high flecks fall away before the restart input. Feature ajaeng (grainy bowed low zither). Reach a definite ending and leave a short natural decay. No vocals.
```

## 23 · 출입 승인

**P2 · 승진과 새 층 출입 권한**
108 BPM (동일 길이 동기용 고정) · D장조 · 4.5마디 · 10.00초 · 1회 재생

편곡 흐름 — 호출음 → 승인 → 잔향.

편집 구획(마디) — 1–4.5.

편성 — 보조 피아노, 합주 현, 피아노, 가야금, 북, 유리음.

주요 파트 실음역(MIDI) — 합주 현 54–69, 피아노 66–78, 가야금 69–81.

**짧은 스타일** (107자)

```text
108 BPM, compact promotion flourish, ascending harmony, clean institutional chime, gayageum, buk, no vocals
```

**긴 스타일**

```text
Instrumental game score, 108 BPM in 4/4. Distinguish authorization from a checkpoint: let the first signal ask, the higher response confirm, then release the upper note cleanly. Feature gayageum (finger-plucked strings with a bending tail); buk (round low barrel-drum stroke). Reach a definite ending and leave a short natural decay. No vocals.
```

## 24 · 층과 층 사이

**P2 · 승강기 이동**
78 BPM (재편곡 73–83) · D장조 · B단조 · 20마디 · 61.54초 · 마디 경계 루프

편곡 흐름 — 승강기 상승·하강의 완만한 동형 → 건반 대답.

편집 구획(마디) — 1–8 / 9–16 / 17–20.

편성 — 클린 기타, 첼로, 패드, 피아노, 유리음, 대금.

주요 파트 실음역(MIDI) — 클린 기타 52–79, 첼로 42–52, 피아노 61–74, 대금 67–78.

**짧은 스타일** (99자)

```text
78 BPM, gentle transit interlude, open chords, quiet motor pulse and steady air, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 78 BPM in 4/4. Use an even pulse that can survive any number of floors. Give each change of harmony its own breathing space so the opening doors can carry the scene. Feature daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 25 · 창마다 다른 번호

**P1 · 중층 행정구 · 민원 통로와 기록동**
132 BPM (재편곡 127–137) · A minor / C major · 32마디 · 58.18초 · 마디 경계 루프

편곡 흐름 — 피아노 행진 → 피리 주제 → 클린 기타 완충 → 피아노 회귀.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 피아노, 베이스, 거문고, 드럼 키트, 유리음, 피리, 합주 현, 보조 피아노, 클린 기타.

주요 파트 실음역(MIDI) — 피아노 64–79, 베이스 31–52, 거문고 50–64, 피리 64–81.

**짧은 스타일** (113자)

```text
132 BPM, measured civic procession, precise piano, Korean reed and angular bass zither, geomungo, piri, no vocals
```

**긴 스타일**

```text
Instrumental game score, 132 BPM in 4/4. Set the recurring street pattern in keys and dry zither. Shift the head to a compact reed, then put a more human clean-guitar phrase between its repetitions. Feature geomungo (low zither snapped with a short stick); piri (compact, penetrating double reed). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 26 · 셔터 사이의 색인

**P1 · 중층 행정구 · 보안 셔터 난관**
146 BPM (재편곡 141–151) · C♯ minor · 24마디 · 39.45초 · 마디 경계 루프

편곡 흐름 — 짧은 신호 → 박의 공백 → 셔터 간격 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 보조 피아노, 거문고, 신스, 장구, 코일음, 드럼 키트, 짧은 활 현, 합주 현, 유리음, 클린 기타.

주요 파트 실음역(MIDI) — 거문고 44–61, 신스 32–42, 짧은 활 현 66–80, 합주 현 71–73.

**짧은 스타일** (116자)

```text
146 BPM, security shutters, clipped note then silence, clocklike plucks and sparse bass, geomungo, janggu, no vocals
```

**긴 스타일**

```text
Instrumental game score, 146 BPM in 4/4. Make the player anticipate the silence after the click. Remove the reference pulse at the second cadence, then return it beside a shifted pitch. Feature geomungo (low zither snapped with a short stick); janggu (contrasting low and high hourglass-drum heads). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 27 · 장부의 눌린 자국

**P2 · 행정구에서 두 기록의 원본 대조**
87 BPM (재편곡 82–92) · B♭단조 · 16마디 · 44.14초 · 마디 경계 루프

편곡 흐름 — 두 기록의 대조 → 다른 음 하나 → 문구의 회귀.

편집 구획(마디) — 1–8 / 9–16.

편성 — 베이스, 피아노, 클린 기타, 코일음, 드럼 키트, 해금.

주요 파트 실음역(MIDI) — 베이스 30–41, 피아노 49–68, 클린 기타 57–68, 해금 57–68.

**짧은 스타일** (108자)

```text
87 BPM, forensic chamber cue, paired piano figures, low electric colour and bowed fiddle, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 87 BPM in 4/4. Set two closely related phrases side by side. Change a small detail in the second and allow a bowed tone to linger over the discrepancy. Feature haegeum (narrow two-string bowed tone and slow vibrato). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 28 · 검문소 사이

**P2 · 주안과 함께 무전을 들으며 통제선을 통과**
137 BPM (재편곡 132–142) · G단조 · 24마디 · 42.04초 · 마디 경계 루프

편곡 흐름 — 호송 걸음 → 무전 사이 응답 → 검문 통과.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 첼로, 베이스, 클린 기타, 짧은 활 현, 드럼 키트, 피아노, 대금, 합주 현, 장구.

주요 파트 실음역(MIDI) — 첼로 50–65, 베이스 38–53, 클린 기타 65–67, 짧은 활 현 53–63.

**짧은 스타일** (110자)

```text
137 BPM, careful escort pulse, lower strings, muted steps and spaced flute replies, daegeum, janggu, no vocals
```

**긴 스타일**

```text
Instrumental game score, 137 BPM in 4/4. Anchor the meter below the speaking range. Fit upper melodic answers in the gaps so radio dialogue can be understood without stopping the walk. Feature daegeum (breathy transverse bamboo flute with a buzzing membrane); janggu (contrasting low and high hourglass-drum heads). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 29 · 봉쇄선

**P0 · 보스 직전 대기**
85 BPM (재편곡 80–90) · E단조 · B 페달 · 12마디 · 33.88초 · 마디 경계 루프

편곡 흐름 — 긴 저음 위에 유리 신호를 쪼개고 다음 박을 비운다.

편집 구획(마디) — 1–4 / 5–8 / 9–12.

편성 — 첼로, 패드, 가야금, 코일음, 아쟁, 합주 현, 드럼 키트, 대금, 피아노, 유리음.

주요 파트 실음역(MIDI) — 첼로 45–52, 가야금 57–64, 아쟁 47–52, 합주 현 67–72.

**짧은 스타일** (113자)

```text
85 BPM, suspended pre-battle pulse, hollow low notes, isolated glass chimes, gayageum, ajaeng, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 85 BPM in 4/4. Put a question in the high register and delay the answer for a whole bar. Keep any transient softer than the first combat warning. Feature gayageum (finger-plucked strings with a bending tail); ajaeng (grainy bowed low zither); daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 30 · 집행 개시 Ⅰ

**P0 · 중층 행정구 · 기록 집행관**
160 BPM (재편곡 155–165) · D단조 · 32마디 · 48.00초 · 마디 경계 루프

편곡 흐름 — 8마디 주제 → 8마디 대답 → 페달 브리지 → 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 피리, 거문고, 장구, 피아노, 베이스, 드럼 키트, 유리음, 보조 피아노, 팀파니.

주요 파트 실음역(MIDI) — 피리 64–81, 거문고 40–62, 피아노 52–82, 베이스 33–55.

**짧은 스타일** (116자)

```text
160 BPM, reedy five-note call, sharp Korean bass zither, piano and hourglass drum, piri, geomungo, janggu, no vocals
```

**긴 스타일**

```text
Instrumental game score, 160 BPM in 4/4. State D, A, C, E, D with a short breath before the fourth note. Give the low plucks a separate syncopation and use the middle pedal passage to reduce pressure. Feature piri (compact, penetrating double reed); geomungo (low zither snapped with a short stick); janggu (contrasting low and high hourglass-drum heads). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 31 · 집행 개시 Ⅱ

**P0 · 중층 행정구 · 기록 집행관**
175 BPM (재편곡 170–180) · D단조 · 32마디 · 43.89초 · 마디 경계 루프

편곡 흐름 — 기타 질문 → 해금 응답 → 낮은 브리지 → 확장 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 리드 일렉기타, 거문고, 장구, 피아노, 피크 베이스, 리듬 일렉기타, 합주 현, 드럼 키트, 해금, 유리음, 클린 기타, 보조 피아노, 첼로.

주요 파트 실음역(MIDI) — 리드 일렉기타 64–82, 거문고 41–62, 피아노 52–79, 피크 베이스 33–55.

**짧은 스타일** (123자)

```text
175 BPM, electric guitar and Korean fiddle trade phrases, clipped kit, moving strings, geomungo, janggu, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 175 BPM in 4/4. Carry the five-note contour into the guitar and let the two-string fiddle answer across the bar line. Pull the orchestral strings down when the bow enters. Feature geomungo (low zither snapped with a short stick); janggu (contrasting low and high hourglass-drum heads); haegeum (narrow two-string bowed tone and slow vibrato). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 32 · 집행 개시 Ⅲ

**P0 · 중층 행정구 · 기록 집행관**
190 BPM (재편곡 185–195) · E단조 · 32마디 · 40.42초 · 마디 경계 루프

편곡 흐름 — E단조 주제 → 기타 독주 → 건반 속주 → 태평소 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 태평소, 북, 피아노, 피크 베이스, 리듬 일렉기타, 드럼 키트, 유리음, 리드 일렉기타, 합주 현, 신스, 금관, 꽹과리, 보조 피아노, 첼로, 클린 기타.

주요 파트 실음역(MIDI) — 태평소 66–83, 피아노 54–78, 피크 베이스 35–57, 리듬 일렉기타 38–58.

**짧은 스타일** (129자)

```text
190 BPM, raised battle refrain, piercing Korean shawm, keyboard runs and stadium drums, taepyeongso, buk, kkwaenggwari, no vocals
```

**긴 스타일**

```text
Instrumental game score, 190 BPM in 4/4. Move the refrain up a whole tone. Alternate the shawm declaration, fast piano figures and a separate electric-guitar lead; place just a few metal accents at section boundaries. Feature taepyeongso (bright conical shawm with a projecting bell); buk (round low barrel-drum stroke); kkwaenggwari (small sharp brass gong with a short choke). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 33 · 철수

**P0 · 제압 후 생존 퇴장**
90 BPM (동일 길이 동기용 고정) · G장조 → Em(add9) · 2마디 · 5.33초 · 1회 재생

편곡 흐름 — 착지 화음 → 기타 상행 → 열린 종지.

편집 구획(마디) — 1–2.

편성 — 보조 피아노, 팀파니, 드럼 키트, 유리음, 리드 일렉기타, 합주 현, 첼로, 대금.

주요 파트 실음역(MIDI) — 팀파니 43–43, 리드 일렉기타 67–74, 합주 현 71–78, 첼로 47–55.

**짧은 스타일** (108자)

```text
90 BPM, short nonlethal victory cue, guitar ascent, bamboo flute and an open final chord, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 90 BPM in 4/4. Celebrate a won encounter without sealing the harmony. Let the flute carry the departing character beyond the final guitar note. Feature daegeum (breathy transverse bamboo flute with a buzzing membrane). Reach a definite ending and leave a short natural decay. No vocals.
```

## 34 · 온실 너머의 오후

**P1 · 상층 거주구 · 유리 테라스와 주민 구역**
108 BPM (재편곡 103–113) · D major / Lydian colour · 32마디 · 71.11초 · 마디 경계 루프

편곡 흐름 — 테라스 노래 → 실내의 얇은 합주 → 밝은 바깥 주제.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 대금, 베이스, 패드, 가야금, 유리음, 클린 기타, 합주 현, 하프, 드럼 키트, 해금, 피아노.

주요 파트 실음역(MIDI) — 대금 68–79, 베이스 33–43, 가야금 52–66, 클린 기타 69–81.

**짧은 스타일** (125자)

```text
108 BPM, sunlit upper district, spacious harmony, translucent strings and a calm pulse, daegeum, gayageum, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 108 BPM in 4/4. Preserve a raised-fourth shimmer in the voicing. Leave the upper register open until a soft melodic answer casts a reflection across it. Feature daegeum (breathy transverse bamboo flute with a buzzing membrane); gayageum (finger-plucked strings with a bending tail); haegeum (narrow two-string bowed tone and slow vibrato). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 35 · 유리 아래의 상승류

**P1 · 상층 거주구 · 환기 상승류와 부유체 난관**
154 BPM (재편곡 149–159) · B Dorian · 24마디 · 37.40초 · 마디 경계 루프

편곡 흐름 — 상승 선율 → 반대 방향의 동기 → 높은 픽업으로 복귀.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 짧은 활 현, 가야금, 피크 베이스, 드럼 키트, 장구, 합주 현, 대금, 피아노, 클린 기타, 유리음.

주요 파트 실음역(MIDI) — 짧은 활 현 66–81, 가야금 52–73, 피크 베이스 33–54, 합주 현 71–74.

**짧은 스타일** (122자)

```text
154 BPM, buoyant precision cue, lifted pickups, inverted phrases, airy rhythmic gaps, gayageum, janggu, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 154 BPM in 4/4. Invert the little upward vent phrase partway through and hold its former pickup across a gap. The player should feel when the next platform will rise. Feature gayageum (finger-plucked strings with a bending tail); janggu (contrasting low and high hourglass-drum heads); daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 36 · 진열장 속 심장 Ⅰ · 계측

**P1 · 상층 거주구 · 코어 보안 병기 전투 편곡 Ⅰ**
150 BPM (재편곡 145–155) · B minor · 24마디 · 38.40초 · 마디 경계 루프

편곡 흐름 — 피아노 계측 → 해금 응답 → 피아노 재현.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 피아노, 베이스, 거문고, 합주 현, 드럼 키트, 짧은 활 현, 해금.

주요 파트 실음역(MIDI) — 피아노 62–78, 베이스 31–54, 거문고 54–66, 합주 현 57–66.

**짧은 스타일** (112자)

```text
150 BPM, calibrated security-machine pulse, piano, glass clock, plucked low zither, geomungo, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 150 BPM in 4/4. Interlock a short low-zither attack with a measured piano melody. Use a sustained fiddle response as the only broad gesture in the middle section. Feature geomungo (low zither snapped with a short stick); haegeum (narrow two-string bowed tone and slow vibrato). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 37 · 진열장 속 심장 Ⅱ · 굴절

**P1 · 상층 거주구 · 코어 보안 병기 전투 편곡 Ⅱ**
164 BPM (재편곡 159–169) · B minor / D major · 32마디 · 46.83초 · 마디 경계 루프

편곡 흐름 — 해금 선율 → 기타 굴절 → 피아노 감편 → 해금 복귀.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 해금, 유리음, 피크 베이스, 거문고, 리듬 일렉기타, 합주 현, 드럼 키트, 장구, 보조 피아노, 클린 기타, 피아노.

주요 파트 실음역(MIDI) — 해금 62–78, 피크 베이스 31–54, 거문고 54–66, 리듬 일렉기타 43–61.

**짧은 스타일** (125자)

```text
164 BPM, refracting battle theme, bowed Korean fiddle, guitar answers, widening strings, haegeum, geomungo, janggu, no vocals
```

**긴 스타일**

```text
Instrumental game score, 164 BPM in 4/4. Pass the same figure between bow and clean guitar. Give the middle of the fight a reduced drum texture so the glass reflections remain countable. Feature haegeum (narrow two-string bowed tone and slow vibrato); geomungo (low zither snapped with a short stick); janggu (contrasting low and high hourglass-drum heads). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 38 · 진열장 속 심장 Ⅲ · 파쇄

**P1 · 상층 거주구 · 코어 보안 병기 전투 편곡 Ⅲ**
178 BPM (재편곡 173–183) · C♯ minor / E major · 32마디 · 43.15초 · 마디 경계 루프

편곡 흐름 — 전조한 기타 → 해금 → 현악 고조 → 기타·금관 귀환.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 리드 일렉기타, 유리음, 피크 베이스, 거문고, 리듬 일렉기타, 합주 현, 보조 피아노, 금관, 팀파니, 드럼 키트, 장구, 해금.

주요 파트 실음역(MIDI) — 리드 일렉기타 64–80, 피크 베이스 33–56, 거문고 56–68, 리듬 일렉기타 45–63.

**짧은 스타일** (120자)

```text
178 BPM, shattering finale, electric guitar head, Korean fiddle, brass and timpani, geomungo, janggu, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 178 BPM in 4/4. Lift the preceding contour a whole tone. Put a brief timpani and brass accent on structural pillars while the guitar reclaims the hook in the final block. Feature geomungo (low zither snapped with a short stick); janggu (contrasting low and high hourglass-drum heads); haegeum (narrow two-string bowed tone and slow vibrato). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 39 · 도시의 뒷면

**P1 · 최상층 정비 외벽과 우회 통로**
142 BPM (재편곡 137–147) · D Dorian · 28마디 · 47.32초 · 마디 경계 루프

편곡 흐름 — 외벽 탐색 → 피아노 이동 → 숨 고르기 → 우회로 재현.

편집 구획(마디) — 1–4 / 5–12 / 13–20 / 21–28.

편성 — 패드, 합주 현, 거문고, 베이스, 클린 기타, 대금, 드럼 키트, 피아노, 유리음.

주요 파트 실음역(MIDI) — 합주 현 74–76, 거문고 48–64, 베이스 36–57, 클린 기타 64–77.

**짧은 스타일** (105자)

```text
142 BPM, covert ascent, low Korean zither, cold piano, distant bamboo flute, geomungo, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 142 BPM in 4/4. Begin with a view through a maintenance duct. Make the flute reply only between footsteps and raise the piano attack as the officer emerges on the outer wall. Feature geomungo (low zither snapped with a short stick); daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 40 · 숨을 세는 눈

**P1 · 저격수 · 본체의 조준과 장전**
147 BPM (재편곡 142–152) · F♯ minor · 28마디 · 45.71초 · 마디 경계 루프

편곡 흐름 — 거리 두기 → 피아노 변형 → 호흡 → 클린 기타 복귀.

편집 구획(마디) — 1–8 / 9–16 / 17–20 / 21–28.

편성 — 베이스, 보조 피아노, 클린 기타, 드럼 키트, 아쟁, 코일음, 피아노.

주요 파트 실음역(MIDI) — 베이스 37–59, 클린 기타 61–76, 아쟁 54–61, 피아노 64–80.

**짧은 스타일** (99자)

```text
147 BPM, long-distance duel, clean guitar sighting motif and breath-length rests, ajaeng, no vocals
```

**긴 스타일**

```text
Instrumental game score, 147 BPM in 4/4. State an upward minor third, a semitone retreat and a leap to the fifth. Give every shot an opening in the melody so a reload feels distinct. Feature ajaeng (grainy bowed low zither). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 41 · 유리 속의 둘

**P1 · 저격수 · 한 명의 분신과 교차 사격**
156 BPM (재편곡 151–161) · F♯ minor / Dorian colour · 32마디 · 49.23초 · 마디 경계 루프

편곡 흐름 — 첫 선율 → 늦게 들어오는 분신 → 주도권 교대 → 합류.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 클린 기타, 신스, 피크 베이스, 드럼 키트, 리듬 일렉기타, 해금, 피아노, 장구.

주요 파트 실음역(MIDI) — 클린 기타 53–76, 신스 49–68, 피크 베이스 37–59, 리듬 일렉기타 53–64.

**짧은 스타일** (120자)

```text
156 BPM, two-voice sniper counterpoint, delayed answers, offset accents and restrained drums, haegeum, janggu, no vocals
```

**긴 스타일**

```text
Instrumental game score, 156 BPM in 4/4. Echo only the first three onsets at first, from a different register. Reverse which voice leads in the bridge without blurring the original attack. Feature haegeum (narrow two-string bowed tone and slow vibrato); janggu (contrasting low and high hourglass-drum heads). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 42 · 바닥 아래의 포좌

**P1 · 저격수 · 분신과 포탑 탈취**
168 BPM (재편곡 163–173) · F♯ minor · 32마디 · 45.71초 · 마디 경계 루프

편곡 흐름 — 포탑 리프 → 피아노 문답 → 스텝 감편 → 기타 복귀.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 리듬 일렉기타, 피크 베이스, 리드 일렉기타, 짧은 활 현, 드럼 키트, 장구, 피아노, 금관, 거문고, 신스.

주요 파트 실음역(MIDI) — 리듬 일렉기타 49–66, 피크 베이스 37–59, 리드 일렉기타 61–76, 짧은 활 현 65–80.

**짧은 스타일** (113자)

```text
168 BPM, turret siege, low chugging guitars, picked bass, clipped zither and strings, janggu, geomungo, no vocals
```

**긴 스타일**

```text
Instrumental game score, 168 BPM in 4/4. Give the emplacements a slow compressed downstroke against the quicker sniper phrase. Withhold the top of the kit during the breakdown. Feature janggu (contrasting low and high hourglass-drum heads); geomungo (low zither snapped with a short stick). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 43 · 두 빛의 접점

**P1 · 저격수 · 충전과 최후의 광선 대결**
180 BPM (재편곡 175–185) · G♯ minor · 24마디 · 32.00초 · 마디 경계 루프

편곡 흐름 — 입자 수집 → 광선 충돌 → 힘겨루기 → 재진입.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 피아노, 패드, 거문고, 베이스, 드럼 키트, 신스, 리듬 일렉기타, 북, 합주 현, 리드 일렉기타, 태평소, 유리음.

주요 파트 실음역(MIDI) — 피아노 66–82, 거문고 52–59, 베이스 39–61, 신스 63–78.

**짧은 스타일** (123자)

```text
180 BPM, laser standoff, charged piano figure, suspended synth and short shawm calls, geomungo, buk, taepyeongso, no vocals
```

**긴 스타일**

```text
Instrumental game score, 180 BPM in 4/4. Begin with the pulse of gathering energy. Lift the sniper motif, open sustained space for the clash, then leave a downbeat ready for either another attempt or victory. Feature geomungo (low zither snapped with a short stick); buk (round low barrel-drum stroke); taepyeongso (bright conical shawm with a projecting bell). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 44 · 초침이 닿기 전

**P1 · 비서실장 집무실 문 앞**
92 BPM (재편곡 87–97) · C minor / chromatic · 20마디 · 52.17초 · 마디 경계 루프

편곡 흐름 — 5마디 단위 네 번, 두 시계 세분음이 조금씩 드러난다.

편집 구획(마디) — 1–5 / 6–10 / 11–15 / 16–20.

편성 — 보조 피아노, 패드, 클린 기타, 피아노, 대금, 코일음, 합주 현, 유리음.

주요 파트 실음역(MIDI) — 클린 기타 55–62, 피아노 62–70, 대금 67–72, 합주 현 77–77.

**짧은 스타일** (102자)

```text
92 BPM, clock antechamber, paired subdivisions, delayed flute and sparse low piano, daegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 92 BPM in 4/4. Set a short quarter-note tick against an intermittent triplet coil. Allow a flute to answer only at the end of each five-bar circuit. Feature daegeum (breathy transverse bamboo flute with a buzzing membrane). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 45 · 영점 결재

**P1 · 비서실장의 시계 · 1페이즈**
172 BPM (재편곡 167–177) · C minor / octatonic colour · 32마디 · 44.65초 · 마디 경계 루프

편곡 흐름 — 피아노 시계 → 기타·피리 → 아쟁 감속 → 첫 박 복귀.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32.

편성 — 짧은 활 현, 피아노, 베이스, 오르간, 드럼 키트, 유리음, 클린 기타, 피리, 아쟁, 보조 피아노, 금관.

주요 파트 실음역(MIDI) — 짧은 활 현 65–83, 피아노 67–83, 베이스 36–56, 오르간 53–71.

**짧은 스타일** (107자)

```text
172 BPM, clockwork duel, cross-rhythmic strings, chromatic piano, bowed low zither, piri, ajaeng, no vocals
```

**긴 스타일**

```text
Instrumental game score, 172 BPM in 4/4. Let the two hands disagree: short triplet strokes against a straight kick. Enlarge the space between the bowed pitches before the hands resume together. Feature piri (compact, penetrating double reed); ajaeng (grainy bowed low zither). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 46 · 결재자 부재

**P1 · 빈 회장실의 노트와 회수 계획**
83 BPM (재편곡 78–88) · F단조 · 프리지안 색채 · 24마디 · 69.40초 · 마디 경계 루프

편곡 흐름 — 빈 방의 건반 → 낮은 조사 동기 → 옥타브 아래 귀환.

편집 구획(마디) — 1–8 / 9–16 / 17–24.

편성 — 보조 피아노, 합주 현, 피아노, 클린 기타, 해금, 유리음, 거문고, 드럼 키트.

주요 파트 실음역(MIDI) — 합주 현 52–60, 피아노 52–73, 클린 기타 67–72, 해금 61–67.

**짧은 스타일** (120자)

```text
83 BPM, empty executive room, low piano dyads, unsettled modal colour and paper-thin space, haegeum, geomungo, no vocals
```

**긴 스타일**

```text
Instrumental game score, 83 BPM in 4/4. Let a question hang after each dry keystroke. Move it down an octave when the notebook is opened, keeping the room larger than the melody. Feature haegeum (narrow two-string bowed tone and slow vibrato); geomungo (low zither snapped with a short stick). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 47 · 회수 명령까지 60초

**P1 · 회장의 노트를 펼친 뒤 코어 회수 작전이 시작되기까지**
120 BPM (동일 길이 동기용 고정) · E단조 · 반음계 긴장 · 30마디 · 60.00초 · 1회 재생

편곡 흐름 — 10초 도입 → 작전 신호 축적 → 마지막 10초의 세분.

편집 구획(마디) — 1–5 / 6–15 / 16–25 / 26–30.

편성 — 코일음, 피아노, 유리음, 베이스, 아쟁, 신스, 드럼 키트, 합주 현, 금관, 북.

주요 파트 실음역(MIDI) — 피아노 50–85, 베이스 35–43, 아쟁 42–55, 신스 35–50.

**짧은 스타일** (109자)

```text
120 BPM, one-minute countdown, electric relay every second, dark bowed drone and drum, ajaeng, buk, no vocals
```

**긴 스타일**

```text
Instrumental game score, 120 BPM in 4/4. Use a 120 BPM grid with one relay tick every two beats. Introduce subdivisions in stages and reserve the last attack for the operation starting at the minute boundary. Feature ajaeng (grainy bowed low zither); buk (round low barrel-drum stroke). Reach a definite ending and leave a short natural decay. No vocals.
```

## 48 · 아래로 달리는 불빛

**P1 · 최상층에서 최하층까지 마지막 트럭 추격**
184 BPM (재편곡 179–189) · C♯단조 · 36마디 · 46.96초 · 마디 경계 루프

편곡 흐름 — 건반 추격 → 기타 재점화 → 해금 여백 → 최하층 가속.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32 / 33–36.

편성 — 피아노, 드럼 키트, 피크 베이스, 리듬 일렉기타, 꽹과리, 장구, 리드 일렉기타, 첼로, 해금, 합주 현.

주요 파트 실음역(MIDI) — 피아노 63–80, 피크 베이스 30–52, 리듬 일렉기타 42–59, 리드 일렉기타 66–80.

**짧은 스타일** (129자)

```text
184 BPM, descending convoy, relentless picked rhythm, electric guitar and bowed replies, kkwaenggwari, janggu, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 184 BPM in 4/4. Make the falling route audible through bass direction. Clear a lane in the upper arrangement for the distant rescuer before restoring the pounding chase pulse. Feature kkwaenggwari (small sharp brass gong with a short choke); janggu (contrasting low and high hourglass-drum heads); haegeum (narrow two-string bowed tone and slow vibrato). Finish on a clean bar boundary that can rejoin the opening without a fade. No vocals.
```

## 49 · 단 한 발의 공명

**P1 · 저격수가 경위를 구하고 코어가 폭발해 추락한다**
96 BPM (동일 길이 동기용 고정) · C♯단조 · 반음계 · 12마디 · 30.00초 · 1회 재생

편곡 흐름 — 한 발 → 잠시 정적 → 격발의 세분 → 추락.

편집 구획(마디) — 1–4 / 5–8 / 9–12.

편성 — 클린 기타, 첼로, 거문고, 피아노, 유리음, 코일음, 합주 현, 보조 피아노, 북, 팀파니, 드럼 키트.

주요 파트 실음역(MIDI) — 클린 기타 61–77, 첼로 37–49, 거문고 47–49, 피아노 56–96.

**짧은 스타일** (111자)

```text
96 BPM, single-shot cinematic, held breath, electrical subdivision and a falling tail, geomungo, buk, no vocals
```

**긴 스타일**

```text
Instrumental game score, 96 BPM in 4/4. Give the rescued officer one bright interval followed by a full breath of near-silence. Tighten the pulse as the core wakes, then let the bottom drop out. Feature geomungo (low zither snapped with a short stick); buk (round low barrel-drum stroke). Reach a definite ending and leave a short natural decay. No vocals.
```

## 50 · 하늘 아래의 하늘

**P1 · 최하층 아래로 떨어진 뒤 이어지는 1회차 크레디트**
98 BPM (동일 길이 동기용 고정) · D장조 · B단조 · 36마디 · 88.16초 · 1회 재생

편곡 흐름 — 피아노 → 클린 기타 → 해금의 회상 → 작은 종지.

편집 구획(마디) — 1–8 / 9–16 / 17–24 / 25–32 / 33–36.

편성 — 피아노, 보조 피아노, 하프, 가야금, 합주 현, 유리음, 클린 기타, 베이스, 드럼 키트, 해금.

주요 파트 실음역(MIDI) — 피아노 62–78, 하프 55–67, 가야금 67–79, 합주 현 55–62.

**짧은 스타일** (123자)

```text
98 BPM, bittersweet first-cycle credits, returning piano theme, soft guitar and Korean fiddle, gayageum, haegeum, no vocals
```

**긴 스타일**

```text
Instrumental game score, 98 BPM in 4/4. Bring the hero's theme back quietly on keys, hand it to strings of different textures, and leave one held space for the buried city beyond the credits. Feature gayageum (finger-plucked strings with a bending tail); haegeum (narrow two-string bowed tone and slow vibrato). Reach a definite ending and leave a short natural decay. No vocals.
```
