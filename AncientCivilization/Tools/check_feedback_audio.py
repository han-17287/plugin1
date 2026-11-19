"""Verify the field mixer references and the shipped score/cue PCM without Unity."""
from array import array
from pathlib import Path
import json
import struct
import re
import sys
import wave


def meta_guid(asset):
    data = Path(str(asset) + '.meta').read_text(encoding='utf-8')
    return re.search(r'^guid: ([0-9a-f]{32})$', data, re.MULTILINE)[1]


def music_stream_info(path):
    # Read the Vorbis identification packet and the final Ogg granule without a decoder.
    data = path.read_bytes()
    offset = 0
    frames = None
    while offset < len(data):
        assert data[offset:offset + 5] == b'OggS\x00', path
        flags = data[offset + 5]
        segments = data[offset + 26]
        start = offset + 27 + segments
        end = start + sum(data[offset + 27:start])
        assert end <= len(data) and end > offset, path
        if offset == 0:
            assert flags & 2 and data[start:start + 7] == b'\x01vorbis', path
            channels, frequency = struct.unpack_from('<BI', data, start + 11)
            assert (channels, frequency) == (2, 44100), path
        if flags & 4:
            assert end == len(data), path
            frames = struct.unpack_from('<Q', data, offset + 6)[0]
        offset = end
    assert frames is not None and frames > 0, path
    return frames


def check_city_score(assets):
    music = assets / 'Resources/Music/CityScore'
    sources = assets.parent.parent / 'Soundtrack'
    catalog = json.loads((sources / 'catalog.json').read_text(encoding='utf-8')) if sources.is_dir() else None
    clips = sorted(music.glob('*.ogg'))
    assert len(clips) == 50, f'{music}: expected 50 campaign cues, found {len(clips)}'
    if catalog is not None:
        assert {clip.stem for clip in clips} == set(catalog), 'campaign source and packaged clip IDs differ'
    guids = set()
    for clip in clips:
        frames = music_stream_info(clip)
        guid = meta_guid(clip)
        assert guid not in guids, f'duplicate music asset ID: {clip.name}'
        guids.add(guid)
        meta = Path(str(clip) + '.meta').read_text(encoding='utf-8')
        assert 'AudioImporter:' in meta and 'forceToMono: 0' in meta and 'sampleRateSetting: 0' in meta, clip
        # The finale seeks by elapsed wall-clock seconds when the notebook is
        # reopened or the game resumes. A shortened master silently loses its ending.
        if clip.stem == 'sixty-to-recovery':
            assert frames == 60 * 44100, 'finale cue must cover the full in-game countdown'
        if catalog is not None:
            cue = catalog[clip.stem]
            assert frames == cue['frames'], f'wrong score duration: {clip.name}'
            score_frames = round(44100 * cue['bars'] * 240 / cue['bpm'])
            assert abs(frames - score_frames) <= 1, f'bar and master lengths disagree: {clip.name}'
            assert clip.read_bytes() == (sources / 'Mixes' / clip.name).read_bytes(), f'mixed master differs: {clip.name}'
    print(f'PASS: {len(clips)} stereo Vorbis cues, loop lengths and unique Unity music imports'
          + ('; exact match to released masters' if catalog is not None else '; project-only source archive'))


def main():
    assets = Path(__file__).resolve().parents[1] / 'Assets'
    check_city_score(assets)
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
