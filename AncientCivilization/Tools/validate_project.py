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


def rgba_pixels(image):
    px=image.load()
    for y in range(image.height):
        for x in range(image.width):
            yield px[x,y]


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
    for name in ('City', 'Hero', 'HeroEmitterBody', 'World', 'Rooftop', 'Enemies', 'Scrap', 'Actions', 'Motorcycle', 'Truck', 'Dialogue'):
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
        hero_cells=[(0,0,384,512),(384,0,316,480),(700,0,420,480),(1120,0,416,512),
                    (0,512,424,512),(424,480,312,544),(736,512,450,512),(1186,512,350,512)]
        for i,(x,y,w,h) in enumerate(hero_cells):
            region = image.getchannel('A').crop((x, y, x + w, y + h))
            bbox = region.point(lambda a: 255 if a >= 110 else 0).getbbox()
            check(bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < w and bbox[3] < h,
                  'hero pose has opaque silhouette and clear gutters ' + str(i))
        hero_image = Image.open(ROOT / 'Assets/Resources/Art/Hero.png')
        gold = sum(1 for r,g,b,a in rgba_pixels(hero_image.crop((0,0,384,480)))
                   if a >= 110 and r > 180 and g > 130 and b < 100)
        check(gold > 1500, 'idle hero visibly carries a broad golden blade')
        pixels=hero_image.load()
        gold_y=[y for y in range(230,480) for x in range(30,150)
                if pixels[x,y][3]>=110 and pixels[x,y][0]>180 and pixels[x,y][1]>130 and pixels[x,y][2]<100]
        check(gold_y and max(gold_y)-min(gold_y)>190,'idle blade has a long sword silhouette rather than a short stub')
        helix = sum(1 for r,g,b,a in rgba_pixels(hero_image.crop((620,480,750,584)))
                    if a >= 110 and g > 130 and b > 150 and r < 150)
        check(helix > 1000, 'hero equipment sheet contains substantial double-helix wire preview')
        core=hero_image.load()
        continuous=all(any(core[x,y][3]>=110 and core[x,y][0]>160 and core[x,y][1]>200 and core[x,y][2]>200
                           for y in range(628,640)) for x in range(1000,1160))
        check(continuous,'laser preview is a continuous bright beam rather than detached particles')
        body_image = Image.open(ROOT / 'Assets/Resources/Art/HeroEmitterBody.png')
        projected = sum(1 for r,g,b,a in rgba_pixels(body_image.crop((640,480,750,565)))
                        if a >= 110 and g > 130 and b > 150 and r < 150)
        check(projected < 30, 'runtime emitter body has no baked wire to duplicate the live helix')
        atlas = (ROOT / 'Assets/Scripts/AncientCitizenAtlas.cs').read_text()
        atlas_rects = [tuple(map(int,m)) for m in re.findall(r'new Rect\((\d+),(\d+),(\d+),(\d+)\)',atlas.split('Rect[] Bounds={')[1].split('};')[0])]
        walk_rects = [tuple(map(int,m)) for m in re.findall(r'new Rect\((\d+),(\d+),(\d+),(\d+)\)',atlas.split('Rect[] WalkBounds={')[1].split('};')[0])]
        check(len(atlas_rects)==25 and len(walk_rects)==16,'measured citizen and walk crop coverage')
        def measured_grid(image,x0,y0,w,h,step):
            alpha=image.getchannel('A').load()
            points=[(x,y) for y in range(y0,y0+h,step) for x in range(x0,x0+w,step) if alpha[x+step//2,y+step//2-1]>=110]
            if not points:return None,frozenset()
            left=min(x for x,y in points)-3;top=min(y for x,y in points)-3
            right=max(x for x,y in points)+step+3;bottom=max(y for x,y in points)+step+3
            return (left,top,right-left,bottom-top),frozenset((x-x0,y-y0) for x,y in points)
        identities=[]
        for floor in range(5):
            path=ROOT / ('Assets/Resources/Art/Citizens'+str(floor)+'.png');info=png_info(path)
            check(info[:2]==(1536,1024) and info[3]==6,'citizen atlas RGBA dimensions '+str(floor))
            citizen_image=Image.open(path)
            for slot in range(6):
                x0=slot%3*512;y0=slot//3*512;rect,shape=measured_grid(citizen_image,x0,y0,512,512,8)
                if slot==5:
                    check(rect is None,'citizen spare cell is truly transparent '+str(floor));continue
                check(rect==atlas_rects[floor*5+slot],'citizen crop matches exact native-grid alpha '+str(floor*5+slot))
                x,y,w,h=rect;check(x>x0 and y>y0 and x+w<x0+512 and y+h<y0+512,'citizen silhouette clear gutter '+str(floor*5+slot))
                px=citizen_image.load();identities.append(tuple(px[x0+xx+4,y0+yy+3] for xx,yy in sorted(shape)))
            meta=path.with_suffix('.png.meta').read_text()
            check('isReadable: 1' in meta and 'filterMode: 0' in meta,'citizen point readable import '+str(floor))
        check(len(set(identities))==25,'all twenty-five citizen appearances are distinct')
        walk_image=Image.open(ROOT / 'Assets/Resources/Art/CitizensWalk.png')
        check(walk_image.size==(1536,1024),'four-person walking sheet dimensions')
        for row in range(4):
            poses=[]
            for frame in range(4):
                rect,shape=measured_grid(walk_image,frame*384,row*256,384,256,4)
                check(rect==walk_rects[row*4+frame],'walking crop matches native-grid alpha '+str(row*4+frame))
                poses.append(shape)
            check(len(set(poses))==4,'escort has four distinct walk phases '+str(row))
        image = Image.open(ROOT / 'Assets/Resources/Art/World.png')
        for i, (x, y, w, h) in enumerate(rects):
            region = image.getchannel('A').crop((x, y, x + w, y + h))
            check(region.point(lambda a: 255 if a >= 110 else 0).getbbox() is not None,
                  'world slice contains visible art ' + str(i))
        image = Image.open(ROOT / 'Assets/Resources/Art/Actions.png')
        from collections import deque
        action_cells = [(0,0,384,512),(384,0,384,512),(768,0,384,512),(1152,0,384,512),
                        (0,512,440,512),(350,512,450,512),(740,512,440,512),(1152,512,384,512)]
        seeds = [(190,250),(600,250),(980,245),(1340,240),(175,780),(530,780),(920,790),(1290,790)]
        source_pixels = image.load()
        silhouettes = []
        for i, ((x0,y0,w,h),(sx,sy)) in enumerate(zip(action_cells,seeds)):
            def color(x,y):
                # Same global 8px grid as the runtime texture, expressed in top-left coordinates.
                return source_pixels[(x+x0)//8*8+4,(y+y0)//8*8+3]
            candidates = [(x,y) for y in range(max(0,sy-y0-40),min(h,sy-y0+41))
                          for x in range(max(0,sx-x0-40),min(w,sx-x0+41)) if color(x,y)[3]>=110]
            check(bool(candidates), 'slash frame has an opaque body seed ' + str(i))
            seed = min(candidates,key=lambda p:(p[0]-(sx-x0))**2+(p[1]-(sy-y0))**2)
            selected={seed}; queue=deque([seed])
            while queue:
                x,y=queue.popleft()
                for dy in (-1,0,1):
                    for dx in (-1,0,1):
                        a,b=x+dx,y+dy
                        if 0<=a<w and 0<=b<h and (a,b) not in selected and color(a,b)[3]>=110:
                            selected.add((a,b));queue.append((a,b))
            bbox=(min(x for x,y in selected),min(y for x,y in selected),max(x for x,y in selected)+1,max(y for x,y in selected)+1)
            check(bbox[0]>0 and bbox[1]>0 and bbox[2]<w and bbox[3]<h,
                  'isolated slash silhouette retains full blade with clear gutters ' + str(i))
            gold=sum(1 for x,y in selected if color(x,y)[0]>180 and color(x,y)[1]>130 and color(x,y)[2]<100)
            check(gold>1800,'slash pose retains a long broad yellow blade ' + str(i))
            silhouettes.append(frozenset((x+x0-i%4*384,y) for x,y in selected))
        check(len(set(silhouettes))==8, 'all eight new slash poses are distinct')
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
    runtime = '\n'.join(p.read_text() for p in (ROOT / 'Assets/Scripts').glob('*.cs'))
    for spoiler in ('옛 친구', '옛 동료', '명아', '너였어?', '이제야 알아보네'):
        check(spoiler not in runtime, 'first-run script withholds friendship reveal: ' + spoiler)
    campaign = (ROOT / 'Assets/Scripts/AncientCampaignData.cs').read_text()
    check(campaign.count('조커') == 5, 'five sparse Joker mentions across city resident dialogue')
    check('new Line("치안경찰"' not in runtime, 'protagonist dialogue consistently uses Baek Myeong')
    results.append('NOT RUN: Unity C# type/API compilation, shader compilation, import, Play Mode, gameplay, player build')
    print('\n'.join(results))


if __name__ == '__main__':
    main()
