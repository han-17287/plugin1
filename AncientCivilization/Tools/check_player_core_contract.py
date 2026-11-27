"""Validate authoring contracts; does not run Unity or simulate physical movement."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def main():
    data = json.loads((ROOT/'Assets/Resources/PlayerCore/P1Player.inputactions').read_text())
    maps = data['maps']; assert len(maps)==1 and maps[0]['name']=='Gameplay'
    action_map=maps[0]; actions={a['name']:a for a in action_map['actions']}
    expected={'Move','Jump','Dash','Drop','Summon','Primary','Collect','Detach','Laser','Overload','Pause','Point'}
    assert set(actions)==expected
    identifiers=[a['id'] for a in action_map['actions']]+[b['id'] for b in action_map['bindings']]
    assert len(identifiers)==len(set(identifiers))
    paths={name:[b['path'] for b in action_map['bindings'] if b['action']==name] for name in actions}
    for action,path in {'Jump':'<Keyboard>/space','Dash':'<Keyboard>/leftShift','Summon':'<Keyboard>/q','Primary':'<Mouse>/leftButton','Collect':'<Mouse>/rightButton','Detach':'<Keyboard>/e','Laser':'<Keyboard>/f','Overload':'<Keyboard>/r','Pause':'<Keyboard>/escape','Point':'<Mouse>/position'}.items():
        assert paths[action]==[path],(action,paths[action])
    assert paths['Drop']==['OneModifier','<Keyboard>/s','<Keyboard>/space']
    assert len(paths['Move'])==10
    for binding in action_map['bindings']:assert binding['action'] in actions
    packages=ROOT/'Packages'
    manifest=json.loads((packages/'manifest.json').read_text())
    input_package=packages/'com.unity.inputsystem'
    input_manifest=json.loads((input_package/'package.json').read_text())
    assert input_manifest['name']=='com.unity.inputsystem' and input_manifest['version']=='1.20.1'
    assert 'com.unity.inputsystem' not in manifest['dependencies']
    assert (input_package/'InputSystem/Unity.InputSystem.asmdef').is_file()
    assert 'com.unity.modules.tilemap' in manifest['dependencies']
    assert 'activeInputHandler: 2' in (ROOT/'ProjectSettings/ProjectSettings.asset').read_text()
    importer=(ROOT/'Assets/Resources/PlayerCore/P1Player.inputactions.meta').read_text()
    input_importer=(input_package/'InputSystem/Editor/AssetImporter/InputActionImporter.cs.meta').read_text()
    importer_guid=re.search(r'^guid: (\w+)',input_importer,re.M)[1]
    assert importer_guid in importer and 'ScriptedImporter:' in importer
    # Test-scene script and movement-data references must resolve before any editor generation.
    scene=(ROOT/'Assets/Scenes/PlayerMovementPractice.unity').read_text()
    for path in ['Assets/Scripts/PlayerMovementPractice.cs.meta','Assets/Resources/PlayerCore/DefaultMovement.asset.meta']:
        guid=re.search(r'guid: (\w+)',(ROOT/path).read_text())[1];assert guid in scene
    for path in (ROOT/'Assets/Scripts').glob('Player*.cs'):
        text=path.read_text()
        assert not re.search(r'\bInput\.(GetKey|GetMouse|GetAxis)|\bRigidbody\b|\bPhysics\.',text),path
        assert not re.search(r'\b(body|rigidbody|rb)\.velocity\b',text),path
    print('PASS: player action/binding contract, importer, scene/data GUID references, package/settings and 2D/Input System API checks')

if __name__=='__main__':main()
