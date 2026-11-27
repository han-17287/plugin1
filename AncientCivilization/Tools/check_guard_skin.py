"""Verify that the animated LANCER uses exactly the source guard's pixels and density."""
from pathlib import Path
from PIL import Image, ImageChops

art = Path(__file__).resolve().parents[1] / 'Assets' / 'Resources' / 'Art'
with Image.open(art / 'Enemies.png') as enemies:
    # AncientArt.Enemy(0) trims at alpha 110 and retains a three-pixel margin.
    cell = enemies.crop((0, 0, 512, 500)).convert('RGBA')
    bounds = cell.getchannel('A').point(lambda value: 255 if value >= 110 else 0).getbbox()
    left, top, right, bottom = bounds
    base = cell.crop((left - 3, top - 3, right + 3, bottom + 3))
with Image.open(art / 'FieldGuardSkin.png') as skin:
    w, h = base.size
    assert skin.size == (2*w, h) and (w, h) == (435, 327), 'LANCER source density changed'
    body = skin.crop((0, 0, w, h)).convert('RGBA')
    arm = skin.crop((w, 0, w*2, h)).convert('RGBA')
    # Body and blade occupy complementary tiles, including the original antialiased edges.
    assert ImageChops.darker(body.getchannel('A'), arm.getchannel('A')).getbbox() is None
    assert ImageChops.lighter(body, arm).tobytes() == base.tobytes(), 'original LANCER pixels changed'
settings = (art / 'FieldGuardSkin.png.meta').read_text()
assert 'filterMode: 1' in settings and 'textureCompression: 0' in settings
with Image.open(art / 'FieldGuardMotion.png') as drone:
    assert drone.size == (8*184, 3*128), 'unexpected replacement melee frames in drone sheet'
print('PASS original LANCER silhouette, all RGBA samples, texture density, sampling and drone separation.')
