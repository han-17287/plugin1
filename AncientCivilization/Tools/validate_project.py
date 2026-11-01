"""Validate portable Unity project structure and shipped art, without Unity.
Optional packages: Pillow, tree-sitter, tree-sitter-c-sharp.
This does NOT compile C# or validate gameplay.
"""
from pathlib import Path
import json
import re
import struct
import zlib

ROOT = Path(__file__).resolve().parents[1]
results = []


def check(condition, description):
    if not condition:
        raise AssertionError(description)
    results.append('PASS: ' + description)


def png_info(path):
    data = path.read_bytes()
    check(data[:8] == b'\x89PNG\r\n\x1a\n', path.name + ' PNG signature')
    offset = 8
    header = None
    ended = False
    while offset < len(data):
        size = struct.unpack('>I', data[offset:offset + 4])[0]
        kind = data[offset + 4:offset + 8]
        payload = data[offset + 8:offset + 8 + size]
        stored_crc = struct.unpack('>I', data[offset + 8 + size:offset + 12 + size])[0]
        if zlib.crc32(kind + payload) & 0xffffffff != stored_crc:
            raise AssertionError(path.name + ' corrupt PNG chunk')
        if kind == b'IHDR':
            header = struct.unpack('>IIBBBBB', payload)
        offset += size + 12
        if kind == b'IEND':
            ended = True
            break
    check(ended and header is not None, path.name + ' PNG chunk integrity')
    return header


def main():
    manifest = json.loads((ROOT / 'Packages/manifest.json').read_text())
    check(set(manifest['dependencies']) == {
        'com.unity.modules.physics2d', 'com.unity.modules.imgui', 'com.unity.modules.audio'
    }, 'required builtin Unity modules declared')
    check('6000.6.7f1' in (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text(), 'Unity version 6000.6.7f1')
    guids = []
    for asset in (ROOT / 'Assets').rglob('*'):
        if asset.suffix == '.meta':
            check(asset.with_suffix('').exists(), 'metadata target ' + str(asset.relative_to(ROOT)))
            match = re.search(r'^guid: ([a-f0-9]{32})$', asset.read_text(), re.M)
            check(match is not None, '32-character GUID ' + asset.name)
            guids.append(match[1])
        else:
            check(asset.with_name(asset.name + '.meta').exists(), 'metadata coverage ' + asset.name)
    check(len(guids) == len(set(guids)), 'unique asset GUIDs')
    scene = ROOT / 'Assets/Scenes/Main.unity'
    scene_guid = re.search(r'guid: (\w+)', scene.with_suffix('.unity.meta').read_text())[1]
    check(scene_guid in (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text(), 'Main scene build GUID')
    dimensions = {}
    for name in ('City', 'Hero', 'World', 'Rooftop', 'Enemies', 'Scrap', 'Actions', 'Motorcycle'):
        path = ROOT / ('Assets/Resources/Art/' + name + '.png')
        header = png_info(path)
        dimensions[name] = header[:2]
        check(header[:2] == ((1254, 1254) if name == 'Scrap' else (1536, 1024)), name + ' expected image dimensions')
        if name not in ('City', 'Rooftop'):
            check(header[3] == 6, name + ' has RGBA channel')
        meta = path.with_suffix('.png.meta').read_text()
        check('isReadable: 1' in meta and 'filterMode: 0' in meta and 'textureCompression: 0' in meta,
              name + ' readable point-filtered uncompressed import')
    art = (ROOT / 'Assets/Scripts/AncientArt.cs').read_text()
    rect_block = art.split('Rect[] cells = {', 1)[1].split('};', 1)[0]
    rects = [tuple(map(int, m)) for m in re.findall(r'new Rect\((\d+),(\d+),(\d+),(\d+)\)', rect_block)]
    check(len(rects) == 8, 'eight explicit world asset slices')
    for i, (x, y, w, h) in enumerate(rects):
        check(x >= 0 and y >= 0 and x + w <= dimensions['World'][0] and y + h <= dimensions['World'][1],
              'world slice in texture bounds ' + str(i))
    enemy_block = art.split('Rect[] enemyCells={', 1)[1].split('};', 1)[0]
    enemy_rects = [tuple(map(int, m)) for m in re.findall(r'new Rect\((\d+),(\d+),(\d+),(\d+)\)', enemy_block)]
    check(len(enemy_rects) == 6, 'six distinct enemy sprite slices')
    for i, (x, y, w, h) in enumerate(enemy_rects):
        check(x >= 0 and y >= 0 and x + w <= 1536 and y + h <= 1024, 'enemy slice in texture bounds ' + str(i))
    bike_block = art.split('Rect[] bikeCells={', 1)[1].split('};', 1)[0]
    bike_rects = [tuple(map(int, m)) for m in re.findall(r'new Rect\((\d+),(\d+),(\d+),(\d+)\)', bike_block)]
    check(len(bike_rects) == 4, 'four modular motorcycle components')
    for i, (x, y, w, h) in enumerate(bike_rects):
        check(x >= 0 and y >= 0 and x + w <= 1536 and y + h <= 1024,
              'motorcycle component in texture bounds ' + str(i))
    for name in ('ParticleGlow', 'CharacterRim', 'Presentation'):
        shader = ROOT / ('Assets/Resources/Shaders/' + name + '.shader')
        source = shader.read_text()
        check('Shader "' in source and source.count('CGPROGRAM') == source.count('ENDCG') == 1,
              'shader resource and program delimiters ' + name)
    try:
        from PIL import Image
        image = Image.open(ROOT / 'Assets/Resources/Art/Hero.png')
        for i in range(8):
            x, y, w, h = i % 4 * 384, i // 4 * 512, 384, 512
            if i == 2:
                x, w = 768, 400
            if i == 3:
                x, w = 1168, 368
            if i == 4:
                x, w = 0, 392
            if i == 5:
                x, w = 392, 364
            if i == 6:
                x, w = 756, 412
            if i == 7:
                x, w = 1168, 368
            region = image.getchannel('A').crop((x, y, x + w, y + h))
            bbox = region.point(lambda a: 255 if a >= 110 else 0).getbbox()
            check(bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < w and bbox[3] < h,
                  'hero pose has opaque silhouette and clear gutters ' + str(i))
        image = Image.open(ROOT / 'Assets/Resources/Art/World.png')
        for i, (x, y, w, h) in enumerate(rects):
            region = image.getchannel('A').crop((x, y, x + w, y + h))
            check(region.point(lambda a: 255 if a >= 110 else 0).getbbox() is not None,
                  'world slice contains visible art ' + str(i))
        image = Image.open(ROOT / 'Assets/Resources/Art/Actions.png')
        for i in range(8):
            x, y, w, h = i % 4 * 384, i // 4 * 512, 384, 512
            if i == 1:
                x, w = 384, 392
            if i == 2:
                x, w = 776, 376
            region = image.getchannel('A').crop((x, y, x + w, y + h))
            bbox = region.point(lambda a: 255 if a >= 110 else 0).getbbox()
            check(bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < w and bbox[3] < h,
                  'action pose has clear transparent gutters ' + str(i))
            root = 207 if i == 2 else 215
            check(bbox and bbox[0] <= root < bbox[2], 'action root lies inside silhouette width ' + str(i))
        image = Image.open(ROOT / 'Assets/Resources/Art/Enemies.png')
        for i, (x, y, w, h) in enumerate(enemy_rects):
            region = image.getchannel('A').crop((x, y, x + w, y + h))
            bbox = region.point(lambda a: 255 if a >= 110 else 0).getbbox()
            check(bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < w and bbox[3] < h,
                  'enemy silhouette and transparent gutters ' + str(i))
        image = Image.open(ROOT / 'Assets/Resources/Art/Motorcycle.png')
        bike_bounds = []
        for i, (x, y, w, h) in enumerate(bike_rects):
            region = image.getchannel('A').crop((x, y, x + w, y + h))
            bbox = region.point(lambda a: 255 if a >= 110 else 0).getbbox()
            check(bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < w and bbox[3] < h,
                  'motorcycle component silhouette and transparent gutters ' + str(i))
            bike_bounds.append(bbox)
        wheel = bike_bounds[1]
        check(abs((wheel[2] - wheel[0]) / (wheel[3] - wheel[1]) - 1) < .05,
              'rotating wheel silhouette is circular')
        normal, damaged = bike_bounds[0], bike_bounds[3]
        check(abs((damaged[2] - damaged[0]) * 1.06 / (normal[2] - normal[0]) - 1) < .05,
              'healthy and damaged chassis display widths agree')
        image = Image.open(ROOT / 'Assets/Resources/Art/Scrap.png')
        bbox = image.getchannel('A').point(lambda a: 255 if a >= 110 else 0).getbbox()
        check(bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < image.width and bbox[3] < image.height,
              'scrap projectile silhouette and transparent gutters')
    except ImportError:
        results.append('SKIP: per-pixel alpha checks (Pillow unavailable)')
    try:
        import tree_sitter
        import tree_sitter_c_sharp
        parser = tree_sitter.Parser(tree_sitter.Language(tree_sitter_c_sharp.language()))
        for script in (ROOT / 'Assets/Scripts').glob('*.cs'):
            tree = parser.parse(script.read_bytes())
            check(not tree.root_node.has_error, 'C# syntax ' + script.name)
    except ImportError:
        results.append('SKIP: C# syntax parser (tree-sitter packages unavailable)')
    results.append('NOT RUN: Unity C# type/API compilation, shader compilation, import, Play Mode, gameplay, player build')
    print('\n'.join(results))


if __name__ == '__main__':
    main()
