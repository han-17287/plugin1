"""Verify shipped exploration data, scene registration, and the room/device guide."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
assets = root / 'Assets'
scripts = assets / 'Scripts'
catalog = (scripts / 'PatrolCatalog.cs').read_text(encoding='utf-8')


def array(key):
    return catalog.split(key + ' = {', 1)[1].split('};', 1)[0]


names = re.findall(r'"([^"]+)"', array('Rooms'))
devices = re.findall(r'"([^"]+)"', array('Devices'))
pairs = [int(value) for value in re.findall(r'\d+', array('pairedDevices'))]
assert len(names) == len(set(names)) == len(devices) == len(set(devices)) == 50
assert sorted(pairs) == list(range(51, 101))
assert [pairs[i - 1] for i in (1, 12, 41)] == [51, 61, 98]
guide = (root / 'PATROL_KO.md').read_text(encoding='utf-8')
for index, (name, device) in enumerate(zip(names, pairs), 1):
    assert f'| {index}. {name} | {device}. {devices[device - 51]} |' in guide, name
for floor, part in enumerate(('Production', 'Living', 'Administration', 'Residence', 'Summit')):
    data = (scripts / f'Patrol{part}.cs').read_text(encoding='utf-8')
    cases = [int(num) for num in re.findall(r'case\s+(\d+):\s*\w+\(\);\s*break;', data)]
    assert sorted(cases) == list(range(10 * floor + 1, 10 * floor + 11)), part


def guid(file):
    return re.search(r'^guid: (\w+)$', file.read_text(), re.M).group(1)


scene = assets / 'Scenes/PatrolExpedition.unity'
script = scripts / 'PatrolExpedition.cs'
assert guid(Path(str(script) + '.meta')) in scene.read_text()
builds = (root / 'ProjectSettings/EditorBuildSettings.asset').read_text()
assert re.search(r'enabled: 1\s+path: Assets/Scenes/PatrolExpedition.unity\s+guid: ' + guid(Path(str(scene) + '.meta')), builds)
print('PASS: 50 authored rooms, 50 distinct paired traversal devices, published room guide and loadable patrol scene')
