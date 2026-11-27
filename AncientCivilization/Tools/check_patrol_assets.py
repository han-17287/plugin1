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
# 타이틀의 시험 버튼도 본편과 같은 벽 고정대를 가진 빨래골목으로 진입해야 한다.
socket_catalog = (scripts / 'PatrolSocketCatalog.cs').read_text(encoding='utf-8')
socket_rooms = re.findall(r'"([^"]+)"', socket_catalog.split('names = {', 1)[1].split('};', 1)[0])
expected_room = socket_rooms.index('빨래골목 두 갈래') + 1
menu = (scripts / 'AncientMainMenu.cs').read_text(encoding='utf-8')
preview = re.search(
    r'"파란 플러그 시험 · 빨래골목"\s*\)\s*\)\s*'
    r'Plugin\.PlayerCore\.PatrolEntry\.Request\(\s*(\d+)\s*,\s*developer\s*:\s*true\s*,'
    r'\s*wallSockets\s*:\s*true\s*\)', menu)
assert preview and int(preview[1]) == expected_room, '타이틀 시험 버튼의 빨래골목/임시 저장 경로'
print('PASS: 50 rooms and devices, guide, loadable patrol scene and title laundry wall-socket preview')
