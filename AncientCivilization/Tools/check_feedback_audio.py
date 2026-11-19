"""Verify the field mixer references and the shipped score/cue PCM without Unity."""
from array import array
from pathlib import Path
import re
import sys
import wave


def meta_guid(asset):
    data = Path(str(asset) + '.meta').read_text(encoding='utf-8')
    return re.search(r'^guid: ([0-9a-f]{32})$', data, re.MULTILINE)[1]


def main():
    assets = Path(__file__).resolve().parents[1] / 'Assets'
    directory = assets / 'Resources/Audio'
    mixer_file = directory / 'FieldMix.mixer'
    mixer = mixer_file.read_text(encoding='utf-8')
    profile = (directory / 'FieldFeedback.asset').read_text(encoding='utf-8')
    script = assets / 'Scripts/AncientFeedbackProfile.cs'
    assert 'guid: ' + meta_guid(script) in profile
    fields = {'effects': 'SFX', 'music': 'BGM', 'ui': 'UI'}
    blocks = {key: body for key, body in re.findall(r'^--- !u!\d+ &(\d+)\n(.*?)(?=^---|\Z)', mixer, re.M | re.S)}
    for field, name in fields.items():
        ref = re.search(rf'^  {field}: {{fileID: (\d+), guid: ([0-9a-f]+), type: 2}}$', profile, re.M)
        assert ref and ref[2] == meta_guid(mixer_file)
        assert re.search(rf'^  m_Name: {name}$', blocks[ref[1]], re.M)
    for group in (s for s in blocks.values() if s.startswith('AudioMixerGroupController:')):
        ref = re.search(r'm_Effects:\n  - {fileID: (\d+)}', group)[1]
        assert 'm_EffectName: Attenuation' in blocks[ref]

    cue_enum = re.search(r'enum AncientEquipmentCue\s*{([^}]+)}', script.read_text(encoding='utf-8'))[1]
    cues = [token.strip() for token in cue_enum.split(',')]
    stems = ['ArchiveBass', 'ArchiveGlass']
    expected = set(cues + stems)
    assert {file.stem for file in directory.glob('*.wav')} == expected
    for name in expected:
        audio = directory / (name + '.wav')
        with wave.open(str(audio), 'rb') as wav:
            assert (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()) == (1, 2, 22050), name
            count = wav.getnframes()
            pcm = array('h', wav.readframes(count))
            if sys.byteorder != 'little':
                pcm.byteswap()
            assert len(pcm) == count and 2500 <= max(map(abs, pcm)) < 32760, name
            assert abs(pcm[0]) < 64 and abs(pcm[-1]) < 256, name
            if name in stems:
                assert count == 8 * 22050, name
            else:
                assert .07 <= count / 22050 <= .75, name
    print(f'PASS: {len(cues)} equipment cues, two four-bar stems, PCM headroom/edges and mixer group references')


if __name__ == '__main__':
    main()
