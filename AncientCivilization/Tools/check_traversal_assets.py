"""Check the actual prefab/data authoring contract; not a Unity import or playthrough."""
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[1]
FOLDER=ROOT/'Assets/Resources/Traversal'
def number(text,key):return float(re.search(r'^  '+re.escape(key)+r': ([\d.]+)$',text,re.M)[1])
def guid(path):return re.search(r'guid: (\w+)',Path(str(path)+'.meta').read_text())[1]
def main():
    by_guid={re.search(r'^guid: (\w+)',p.read_text(),re.M)[1]:p.with_suffix('') for p in (ROOT/'Assets').rglob('*.meta')}
    for prefab in FOLDER.glob('*.prefab'):
        text=prefab.read_text();assert 'm_LocalScale: {x: 1, y: 1, z: 1}' in text
        for g in re.findall(r'guid: (\w+)',text):assert g in by_guid,(prefab,g)
    scene=(ROOT/'Assets/Scenes/TraversalRhythmPractice.unity').read_text();assert guid(ROOT/'Assets/Scripts/TraversalRhythmPractice.cs') in scene
    data=(FOLDER/'DefaultGrapple.asset').read_text();assert number(data,'range')==14 and number(data,'speed')==72
    for name,count,gap,amp in [('Easy',3,6,.5),('Normal',4,7,1),('Hard',5,8,1.5)]:
        text=(FOLDER/(name+'.asset')).read_text();assert number(text,'linkedAnchors')==count and number(text,'gapTiles')==gap and number(text,'movingAmplitude')==amp
        assert (gap+2*amp)**2+2**2<=(number(data,'range')-2)**2,(name,'reach margin')
        assert number(text,'optional')==(1 if name=='Hard' else 0)
    assert 'hazards: {serializedVersion: 2, m_Bits: 1024}' in data
    assert 'TraversalHazard' in (ROOT/'ProjectSettings/TagManager.asset').read_text()
    assert number((FOLDER/'SafeFlowRoom.prefab').read_text(),'precision')==0
    assert number((FOLDER/'GauntletRoom.prefab').read_text(),'precision')==1
    assert len(list(FOLDER.glob('*.prefab')))==13
    for index,name in enumerate(['Production','Living','Administration','Residence','Top']):
        profile=FOLDER/(name+'.asset');text=profile.read_text();assert number(text,'floor')==index
        assert 0<number(text,'gateWarning')<number(text,'gateOpen')<number(text,'gatePeriod')
        assert 0<number(text,'socketOpen')<number(text,'socketPeriod')
        assert number(text,'scanHold')>0 and number(text,'scanWidth')>0 and number(text,'ventLift')>0
        for kind in ['FlowRoom','ChallengeRoom']:
            prefab=FOLDER/(('SafeFlowRoom' if kind=='FlowRoom' else 'GauntletRoom')+'.prefab') if index==0 else FOLDER/(name+kind+'.prefab')
            assert guid(profile) in prefab.read_text(),prefab
    print('PASS: thirteen traversal prefabs, scene GUIDs, three authored difficulty profiles, real range/motion margins and hazard layer contract')
if __name__=='__main__':main()
