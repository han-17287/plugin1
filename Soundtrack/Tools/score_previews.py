"""Build the album's chaptered previews and level-matched A/B from the game masters."""
import argparse
import json
from pathlib import Path
import subprocess
import tempfile

import numpy as np

RATE = 44100
ALBUM = Path(__file__).resolve().parents[1]
CATALOG = json.loads((ALBUM / "catalog.json").read_text(encoding="utf-8"))

# One-based score bars. A pair always samples precisely the same passage.
BOSS = [
    ("roof-captain", 17, 8), ("iron-suit-3", 25, 8), ("enforcer-3", 25, 8),
    ("residence-warden-3", 21, 8), ("joker-sightline", 1, 8), ("clock-secretary", 21, 8),
]
FIELD = [
    ("shift-current", 32, 8), ("heat-riser", 9, 8), ("filament", 9, 4),
    ("glass-terrace", 9, 4), ("greenhouse-fault", 9, 8), ("truck-pursuit", 9, 8),
    ("redline-remount", 17, 8), ("retreat", 1, 2),
]
# 타이틀의 피아노/플루트 교대, 재고 관리의 기타 응답, 비서실장 곡의 루프 종지를 비교한다.
COMPARISON = [("ignition-title", 11, 4), ("morning-inventory", 15, 3), ("clock-secretary", 31, 4)]
PATROL = [
    ("shift-current", 33, 3), ("alley-trail", 9, 3), ("shutter-index", 13, 3),
    ("greenhouse-fault", 9, 3), ("summit-trespass", 9, 3),
]


def excerpt(directory, cue, first, count):
    record = CATALOG[cue]
    frames_per_bar = RATE * 240 / record["bpm"]
    lo = round((first - 1) * frames_per_bar)
    hi = round((first + count - 1) * frames_per_bar)
    frames = record["frames"]
    if not record["loop"]:
        hi = min(hi, frames)
    filters = []
    if hi > frames:
        # 마지막 마디에서 첫 마디로 넘어가는 구절은 게임처럼 PCM 샘플 경계에서 잇는다.
        # 발췌 시작과 끝에만 페이드를 적용해 실제 루프 접점을 들을 수 있게 한다.
        filters.append(f"aloop=loop=1:size={frames}")
    filters.append(f"atrim=start_sample={lo}:end_sample={hi}")
    result = subprocess.run(["ffmpeg", "-v", "error", "-i", str(directory / (cue + ".ogg")),
                             "-af", ",".join(filters),
                             "-f", "f32le", "-ac", "2", "-ar", str(RATE), "-"],
                            capture_output=True, check=True)
    audio = np.frombuffer(result.stdout, dtype="<f4").reshape(-1, 2).copy()
    audio[:220] *= np.linspace(0, 1, 220)[:, None]
    audio[-1323:] *= np.linspace(1, 0, 1323)[:, None]
    return audio


def integrated_loudness(audio):
    result = subprocess.run(["ffmpeg", "-nostats", "-hide_banner", "-f", "f32le",
                             "-ar", str(RATE), "-ac", "2", "-i", "-", "-af",
                             "loudnorm=print_format=json", "-f", "null", "-"],
                            input=audio.tobytes(), capture_output=True, check=True)
    message = result.stderr.decode()
    stats, _ = json.JSONDecoder().raw_decode(message[message.rfind("{"):])
    return float(stats["input_i"])


def sequence(path, excerpts, masters, previous=None):
    pieces, metadata = [], [";FFMETADATA1", "album=PLUGIN", "title=" + path.stem]
    clock = 0
    for cue, first, count in excerpts:
        sources = [previous, masters] if previous else [masters]
        pair = [excerpt(source, cue, first, count) for source in sources]
        levels = [integrated_loudness(x) for x in pair]
        gains = [10 ** ((-16 - level) / 20) for level in levels]
        peak = max(np.max(np.abs(x)) * g for x, g in zip(pair, gains))
        common_trim = min(1, 0.88 / max(peak, 0.001))
        for i, x in enumerate(pair):
            tag = ["이전", "수정"][i] if previous else ""
            last = first + count - 1
            bars = CATALOG[cue]["bars"]
            span = f"{first}–{min(last, bars)}"
            if CATALOG[cue]["loop"] and last > bars:
                span += f" → 1–{last - bars}"
            title = f"{CATALOG[cue]['title']} · {span}마디 {tag}".rstrip()
            if pieces:
                gap = .27 if previous and i == 1 else .52 if path.name == "Before-After.mp3" else .10
                silence = np.zeros((round(RATE * gap), 2), dtype="<f4")
                pieces.append(silence)
                clock += len(silence)
            end = clock + len(x)
            metadata += ["[CHAPTER]", f"TIMEBASE=1/{RATE}", f"START={clock}", f"END={end}", "title=" + title]
            print(f"{path.name} {clock/RATE:.2f}s  {tag} {cue}  {levels[i]:.2f} LUFS before matching")
            pieces.append(x * np.float32(gains[i] * common_trim))
            clock = end

    with tempfile.TemporaryDirectory() as tmp:
        meta = Path(tmp) / "chapters.txt"
        meta.write_text("\n".join(metadata) + "\n", encoding="utf-8")
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", str(RATE), "-ac", "2",
                        "-i", "-", "-f", "ffmetadata", "-i", str(meta), "-map", "0:a",
                        "-map_metadata", "1", "-map_chapters", "1", "-c:a", "libmp3lame", "-b:a", "224k",
                        str(path)], input=np.concatenate(pieces).astype("<f4").tobytes(), check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--masters", type=Path, default=ALBUM / "Mixes")
    parser.add_argument("--previous", type=Path, required=True, help="Unmodified OGG masters for A/B")
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)
    for name, excerpts, previous in [
        ("Before-After.mp3", COMPARISON, args.previous), ("Patrol-Rhythm-AB.mp3", PATROL, args.previous),
        ("Field-Score-Preview.mp3", FIELD, None), ("City-Score-Preview.mp3", BOSS, None),
    ]:
        sequence(args.out / name, excerpts, args.masters, previous)
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(args.masters / "main-theme.ogg"),
                    "-map_metadata", "-1", "-metadata", "title=봉인된 하늘 너머",
                    "-c:a", "libmp3lame", "-b:a", "256k", str(args.out / "Main-Theme.mp3")], check=True)
    cue = CATALOG["shift-current"]
    split = round(4 * 240 * RATE / cue["bpm"])
    for name, trim in [("intro", f"end_sample={split}"), ("loop", f"start_sample={split}")]:
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(args.masters / "shift-current.ogg"),
                        "-af", f"atrim={trim},asettb=1/{RATE},asetpts=N", "-map_metadata", "-1", "-c:a", "libvorbis", "-q:a", "6",
                        str(args.out / f"shift-current-{name}.ogg")], check=True)


if __name__ == "__main__":
    main()
