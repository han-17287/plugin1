"""파란 플러그 두 단자의 체결을 그린 여섯 셀. 단자 끝은 각 셀의 중심이다."""
from math import cos, radians, sin
from pathlib import Path
from PIL import Image, ImageDraw

SIZE = 96
SCALE = 4
INK = "#102b4c"
METAL = "#3779a2"
BLUE = "#529fe7"
LIGHT = "#9ce4fc"
WHITE = "#eaffff"


def draw_cell(frame):
    image = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE))
    draw = ImageDraw.Draw(image)

    def path(points, color, width):
        points = [(round((48 + x) * SCALE), round((48 + y) * SCALE)) for x, y in points]
        draw.line(points, fill=color, width=round(width * SCALE), joint="curve")

    # 두 고정쇠가 안으로 닫히고 그 접점에서만 방전된다.
    gap = [22, 13, 10, 12, 17, 20][frame]
    for side in (-1, 1):
        if frame < 5:
            clip = [(-14, side*(gap+2)), (-8, side*gap), (-3, side*(gap-4)), (5, side*(gap-4))]
            path(clip, INK, 5)
            path(clip, METAL, 3)
            path(clip[1:], WHITE if frame in (1, 2) else LIGHT, 1.5)
        if frame in (1, 2, 3):
            length = [0, 13, 28, 35][frame]
            # 두 가닥은 각각의 단자에서 갈라지고 중앙을 비워 둔다.
            points = [(-1,side*7), (-9,side*10), (-12,side*8),
                      (-length*.65,side*(10+frame*2)), (-length,side*(12+frame*4))]
            path(points, BLUE, 3.0 if frame == 2 else 2.0)
            path(points[:-1], WHITE, 1.4)
        if frame >= 2:
            p = (frame - 2) / 3
            for a, start, length in ((57, 14, 8), (133, 18, 9)):
                v = radians(a + 7 * side)
                r = start + p * 16
                delta = length * (1 - p*.78)
                path([(cos(v)*r, side*sin(v)*r),
                      (cos(v)*(r+delta), side*sin(v)*(r+delta))],
                     WHITE if frame < 4 else BLUE, 2.5 - p)
    return image.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def main():
    sheet = Image.new("RGBA", (SIZE * 6, SIZE))
    for frame in range(6):
        sheet.paste(draw_cell(frame), (frame * SIZE, 0))
    dest = Path(__file__).resolve().parents[2] / "Assets/Resources/Traversal/BluePlugContact.png"
    sheet.save(dest, optimize=True)


if __name__ == "__main__":
    main()
