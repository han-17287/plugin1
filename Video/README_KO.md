# 백 경위 — 블레이드 콘셉트 영상

[수정본 MP4 · 1080p60](Officer_Blade_Concept_60fps.mp4) · [기존 MP4 · 1080p30](Officer_Blade_Concept.mp4)

수정본은 **12초·1920×1080·60fps**입니다. 전개 전의 어색한 팔을 어깨–팔꿈치–손목이 이어지는 슬림한 고대 기계팔로 다시 만들었습니다. 손가락·포신·아래로 튀어나온 바늘은 없애고, 같은 자세의 전개 전후 이미지를 맞췄습니다.

기존 30fps 영상의 프레임을 복제하지 않고 카메라·칼날 전개·빛·불꽃·충격파를 매 프레임 다시 렌더링합니다. 칼날은 손목에서 칼끝 방향으로 점차 드러나고, 카메라는 천천히 가속·감속하며 베기와 폭발의 과한 흔들림을 줄였습니다. 소리도 포함합니다.

**정지 이미지 기반 콘셉트 애니매틱**입니다. 실제 게임 플레이 녹화나 캐릭터 동작 전체를 연속 생성한 영상, 완전한 3D 리깅 애니메이션은 아닙니다. 60fps는 카메라·전개·합성 효과의 시간 해상도입니다.

## 장면

| 시간 | 구성 |
| --- | --- |
| 0.0–1.72초 | 수정된 기계팔의 입자 코어 충전 |
| 1.72–2.72초 | 같은 자세에서 노란 블레이드가 손목부터 칼끝까지 전개 |
| 2.72–4.5초 | 전개된 긴 블레이드와 부드러운 카메라 이동 |
| 4.5–6.9초 | 낮은 시점에서 경위와 경비 로봇 대치 |
| 6.9–8.5초 | 옆 시점의 베기와 금속 불꽃 |
| 8.5–12.0초 | 어깨 너머 시점의 로봇 폭발·파편·충격파 |

얼굴을 가린 검은 제모, 검정·흰색 제복, 붉은 스카프, 청록색 고대 기계팔과 노랗게 빛나는 흑연 코어 블레이드를 유지했습니다. 적은 완전한 기계 로봇입니다.

## 파일

- `Officer_Blade_Concept_60fps.mp4`: 수정본. H.264/AAC, 1080p60, 브라우저 재생용 fast-start MP4.
- `Stills60/shot01.png`, `shot02.png`: image_gen으로 수정한 전개 전후 이미지. 생성 원본을 변경 없이 복사했습니다.
- `Preview60/`: 수정된 MP4에서 추출한 확인 프레임.
- `source_manifest_60fps.json`, `technical_metadata_60fps.json`: 수정본 원본 추적·해시·실제 FFprobe 결과.
- `render_concept_v2.py`: 수정본 재현 스크립트. 이미지 원본을 변경하지 않고 영상 마스크·카메라·효과·소리를 합성합니다.
- `Officer_Blade_Concept.mp4`, `Stills/`, `Preview/`, `source_manifest.json`, `technical_metadata.json`, `render_concept.py`: 기존 30fps 영상과 원본·재현 자료. 그대로 보존합니다. 수정본은 기존 `Stills/shot03.png`–`shot05.png`를 재사용합니다.

효과음은 외부 음원 없이 사인파와 필터링한 잡음으로 합성했습니다. 음악과 대사는 넣지 않았습니다. 저장된 고퀄리티 참고 원본은 `ArtReferences/RelicArm_LongFilamentBlade_3D_Reference.png`입니다.

## 다시 렌더링

Python 3, NumPy, SciPy, FFmpeg, FFprobe가 필요합니다. 저장소 루트에서 실행합니다.

```sh
python Video/render_concept_v2.py
```

임시 영상·효과 레이어·WAV는 `work/video60/`에 생성됩니다. 기존 30fps 버전은 `python Video/render_concept.py`로 다시 렌더링합니다.
