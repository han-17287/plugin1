"""Check tile authoring bounds and geometry only; P3-P6 puzzle logic remains unimplemented."""
from pathlib import Path
import json
import math

def main():
    root=Path(__file__).resolve().parents[1]
    rooms=json.loads((root/'Design/TraversalDesigns.json').read_text());assert len(rooms)==12
    assert {r['id'] for r in rooms}=={f'C{i:02}' for i in range(1,13)}
    extra=json.loads((root/'Design/FloorTraversalDesigns.json').read_text());assert len(extra['rooms'])==48 and len(extra['floors'])==4
    rooms+=extra['rooms'];assert len({r['id'] for r in rooms})==60
    for f in extra['floors']:
        assert len(f['stages'])==5 and len(f['required'])==5
        for stage in f['stages']:assert abs(stage['flow']/(stage['flow']+stage['challenge'])-.75)<.0001
    for room in rooms:
        assert len(room['solutions'])>=2 and room['checkpoint']==[3,1]
        anchors=[]
        for x,y,char in room['points']:
            assert 0<=x<room['width'] and 0<=y<room['height'],(room['id'],x,y)
            if char in 'AaMB':
                anchors.append((x,y))
                assert y+.9+.03<14,(room['id'],'roof clearance')
                for a,b,low,top in room['blocks']:
                    overlap=x+.36>a and x-.36<b+1
                    assert not overlap or y-.9>=top+.03,(room['id'],'anchor capsule inside structural terrain',x,y)
        for a,b in zip(anchors,anchors[1:]):assert math.dist(a,b)<=12,(room['id'],'consecutive blueprint links',a,b)
    # Two diagram layouts correspond to the actual C# room exports and the shipping data profiles.
    # The remaining fifty are designs, not executable room variants.
    data=(root/'Assets/Resources/Traversal/Normal.asset').read_text();assert 'linkedAnchors: 4' in data and 'gapTiles: 7' in data
    assert rooms[0]['width']==47 and rooms[1]['width']==60
    print('PASS:60 tile diagrams, two solutions each, checkpoint definitions, registered-link distance and structural capsule bounds; puzzle runtime and native traversal not validated')
if __name__=='__main__':main()
