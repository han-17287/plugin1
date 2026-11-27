"""Pack the three authored blade sheets without resampling their painted frames."""
from pathlib import Path
import re
from PIL import Image


def layout(rectangles, width):
    x = y = 2
    row_height = 0
    for _, _, w, h in rectangles:
        if x + w + 2 > width:
            x = 2
            y += row_height + 2
            row_height = 0
        yield x, y, w, h
        x += w + 2
        row_height = max(row_height, h)


def pack(root):
    source = (root / 'Assets/Scripts/AncientBladeAtlas.cs').read_text()
    raw = source.split('Rectangles={', 1)[1].split('};', 1)[0]
    rects = [tuple(map(int, match)) for match in re.findall(r'new Rect\((\d+),(\d+),(\d+),(\d+)\)', raw)]
    width = int(re.search(r'FieldSheetWidth\s*=\s*(\d+)', source)[1])
    slots = list(layout(rects, width))
    art = root / 'Assets/Resources/Art'
    atlas = Image.new('RGBA', (width, max(y + h for _, y, _, h in slots) + 2))
    for step, sheet in enumerate(('BladeVertical', 'BladeHorizontal', 'BladeDiagonal')):
        with Image.open(art / (sheet + '.png')) as original:
            for frame in range(16):
                i = step * 16 + frame
                x, y, w, h = rects[i]
                atlas.paste(original.crop((x, y, x + w, y + h)), slots[i][:2])
    atlas.save(art / 'FieldBladeDetail.png', compress_level=9)


if __name__ == '__main__':
    pack(Path(__file__).resolve().parents[1])
